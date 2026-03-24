using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Swarm
{
    /// <summary>
    /// Single batched Vicsek + boundary step for all organisms. Replaces per-<see cref="Organism"/> <see cref="FixedUpdate"/>.
    /// </summary>
    /// <remarks>
    /// Optional next steps for very large counts: Unity Jobs + Burst over NativeArrays / NativeParallelMultiHashMap;
    /// rendering: Graphics.DrawMeshInstanced to replace many SpriteRenderers.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SwarmSimulation : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";

        static readonly ProfilerMarker s_ProfileRebuildGrid = new ProfilerMarker("Swarm.RebuildGrid");
        static readonly ProfilerMarker s_ProfileVicsek = new ProfilerMarker("Swarm.Vicsek");
        static readonly ProfilerMarker s_ProfileDynamics = new ProfilerMarker("Swarm.BoundaryIntegrateClamp");
        static readonly ProfilerMarker s_ProfileTransforms = new ProfilerMarker("Swarm.ApplyTransforms");

        public static SwarmSimulation Instance { get; private set; }

        [SerializeField] GameConfig gameConfig;

        readonly List<Organism> _organisms = new List<Organism>();
        readonly List<Vector2> _positions = new List<Vector2>();
        readonly List<Vector2> _velocities = new List<Vector2>();
        readonly List<float> _cruiseSpeeds = new List<float>();

        readonly Dictionary<(int cx, int cy), List<int>> _spatialGrid =
            new Dictionary<(int cx, int cy), List<int>>();

        /// <summary>Reused for Vicsek K-nearest selection (no List grow / Sort per agent).</summary>
        Candidate[] _kBestBuffer;

        int _physicsStepCounter;

        struct Candidate
        {
            public float DistSq;
            public int Index;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SwarmSimulation components; destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Register an organism after <see cref="Organism.Initialize"/> sets up visuals. Call from main thread only.</summary>
        public int RegisterAgent(Organism organism, Vector2 position, Vector2 velocity, float cruiseSpeed)
        {
            int id = _organisms.Count;
            _organisms.Add(organism);
            _positions.Add(position);
            _velocities.Add(velocity);
            _cruiseSpeeds.Add(Mathf.Max(1e-4f, cruiseSpeed));
            return id;
        }

        void FixedUpdate()
        {
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultConfigResource);
            if (gameConfig == null || _organisms.Count == 0)
                return;

            int n = _organisms.Count;
            float dt = Time.fixedDeltaTime;
            float r = gameConfig.neighborRadius;
            int subdivisions = Mathf.Max(1, gameConfig.vicsekSpatialSubdivisions);
            float cellSize = Mathf.Max(r / subdivisions, 1e-4f);
            float r2 = r * r;
            int maxK = Mathf.Max(1, gameConfig.vicsekMaxNeighbors);
            int stagger = Mathf.Max(1, gameConfig.vicsekStagger);
            int step = _physicsStepCounter++;

            // Pass order + ProfilerMarkers: Vicsek reads positions from this frame’s grid (symmetric);
            // then boundary / integration / clamp; then transform writes (clear CPU breakdown in Profiler).
            using (s_ProfileRebuildGrid.Auto())
                RebuildSpatialGrid(cellSize, n);

            using (s_ProfileVicsek.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    if (_organisms[i] == null)
                        continue;

                    bool runVicsek = (i % stagger) == (step % stagger);
                    if (runVicsek)
                        StepVicsekForAgent(i, cellSize, r, r2, maxK);
                }
            }

            using (s_ProfileDynamics.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    if (_organisms[i] == null)
                        continue;

                    ApplyBoundarySteering(i);
                    _positions[i] += _velocities[i] * dt;
                    ClampAgentPosition(i);
                }
            }

            using (s_ProfileTransforms.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    if (_organisms[i] == null)
                        continue;

                    Transform t = _organisms[i].transform;
                    Vector2 p = _positions[i];
                    Vector2 v = _velocities[i];
                    t.position = new Vector3(p.x, p.y, t.position.z);
                    float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                    t.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        void RebuildSpatialGrid(float cellSize, int n)
        {
            int bucketCap = Mathf.Max(1, gameConfig.spatialGridBucketCapacity);

            foreach (List<int> bucket in _spatialGrid.Values)
                bucket.Clear();

            for (int i = 0; i < n; i++)
            {
                if (_organisms[i] == null)
                    continue;

                Vector2 pos = _positions[i];
                int cx = Mathf.FloorToInt(pos.x / cellSize);
                int cy = Mathf.FloorToInt(pos.y / cellSize);
                var key = (cx, cy);
                if (!_spatialGrid.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>(bucketCap);
                    _spatialGrid[key] = list;
                }

                list.Add(i);
            }
        }

        void EnsureKBestCapacity(int maxK)
        {
            if (_kBestBuffer == null || _kBestBuffer.Length < maxK)
                _kBestBuffer = new Candidate[maxK];
        }

        /// <summary>Standard normal N(0,1) via Box–Muller (uses two uniform samples).</summary>
        static float SampleGaussianStandard()
        {
            float u1 = 1f - Random.value;
            if (u1 < 1e-7f)
                u1 = 1e-7f;
            float u2 = Random.value;
            return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        }

        static int IndexOfLargestDistSq(Candidate[] buf, int count)
        {
            int w = 0;
            for (int i = 1; i < count; i++)
            {
                if (buf[i].DistSq > buf[w].DistSq)
                    w = i;
            }

            return w;
        }

        void StepVicsekForAgent(int agentIndex, float cellSize, float neighborRadius, float r2, int maxK)
        {
            EnsureKBestCapacity(maxK);

            Vector2 p = _positions[agentIndex];
            int cx = Mathf.FloorToInt(p.x / cellSize);
            int cy = Mathf.FloorToInt(p.y / cellSize);

            int range = Mathf.Max(1, Mathf.CeilToInt(neighborRadius / cellSize));
            int maxBucketSamples = gameConfig.vicsekMaxBucketSamples;

            int fill = 0;
            Candidate[] buf = _kBestBuffer;

            for (int ox = -range; ox <= range; ox++)
            {
                for (int oy = -range; oy <= range; oy++)
                {
                    if (!_spatialGrid.TryGetValue((cx + ox, cy + oy), out List<int> bucket))
                        continue;

                    int cnt = bucket.Count;
                    int stride = 1;
                    if (maxBucketSamples > 0 && cnt > maxBucketSamples)
                        stride = (cnt + maxBucketSamples - 1) / maxBucketSamples;

                    for (int b = 0; b < cnt; b += stride)
                    {
                        int j = bucket[b];
                        Vector2 d = _positions[j] - p;
                        float dsq = d.sqrMagnitude;
                        if (dsq > r2)
                            continue;

                        Vector2 vj = _velocities[j];
                        if (vj.sqrMagnitude < 1e-8f)
                            continue;

                        if (fill < maxK)
                        {
                            buf[fill++] = new Candidate { DistSq = dsq, Index = j };
                        }
                        else
                        {
                            int w = IndexOfLargestDistSq(buf, maxK);
                            if (dsq < buf[w].DistSq)
                                buf[w] = new Candidate { DistSq = dsq, Index = j };
                        }
                    }
                }
            }

            if (fill == 0)
                return;

            Vector2 sum = Vector2.zero;
            for (int c = 0; c < fill; c++)
            {
                int j = buf[c].Index;
                sum += _velocities[j].normalized;
            }

            Vector2 avg = sum / fill;
            if (avg.sqrMagnitude < 1e-8f)
                return;

            float theta = Mathf.Atan2(avg.y, avg.x);
            float sigmaDeg = gameConfig.angularNoiseDegrees;
            float noiseDeg = sigmaDeg > 0f ? SampleGaussianStandard() * sigmaDeg : 0f;
            theta += noiseDeg * Mathf.Deg2Rad;
            _velocities[agentIndex] = _cruiseSpeeds[agentIndex] * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
        }

        void ApplyBoundarySteering(int i)
        {
            Vector2 center = gameConfig.spawnCircleCenter;
            float R = gameConfig.boundingDomainRadius;
            float band = Mathf.Min(gameConfig.boundingRepulsionBandWidth, R - 1e-4f);
            Vector2 p = _positions[i];
            Vector2 offset = p - center;
            float dist = offset.magnitude;
            float inner = R - band;
            if (dist <= inner || dist < 1e-6f)
                return;

            Vector2 toCenter = -offset / dist;
            float w = Mathf.Clamp01((dist - inner) / band);
            w = w * w * (3f - 2f * w);

            Vector2 vel = _velocities[i];
            Vector2 vicsekDir = vel.sqrMagnitude > 1e-8f ? vel.normalized : toCenter;
            Vector2 dir = Vector2.Lerp(vicsekDir, toCenter, w).normalized;
            _velocities[i] = dir * _cruiseSpeeds[i];
        }

        void ClampAgentPosition(int i)
        {
            Vector2 center = gameConfig.spawnCircleCenter;
            float R = gameConfig.boundingDomainRadius;
            const float eps = 1e-3f;
            Vector2 p = _positions[i];
            Vector2 offset = p - center;
            float dist = offset.magnitude;
            if (dist <= R - eps || dist < 1e-6f)
                return;

            float scale = (R - eps) / dist;
            _positions[i] = new Vector2(
                center.x + offset.x * scale,
                center.y + offset.y * scale);
        }
    }
}
