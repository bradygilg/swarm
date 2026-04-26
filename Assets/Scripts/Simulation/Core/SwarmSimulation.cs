using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;

namespace Swarm
{
    /// <summary>
    /// Shared spatial grid; each <see cref="Organism"/> owns simulation position, velocity, and cruise speed.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SwarmSimulation : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";

        static readonly ProfilerMarker s_ProfileRebuildGrid = new ProfilerMarker("Swarm.RebuildGrid");
        static readonly ProfilerMarker s_ProfileRebuildClear = new ProfilerMarker("Swarm.RebuildGrid.ClearBuckets");
        static readonly ProfilerMarker s_ProfileRebuildInsert = new ProfilerMarker("Swarm.RebuildGrid.InsertAgents");

        static readonly ProfilerMarker s_ProfileVicsek = new ProfilerMarker("Swarm.NeighborSteering");

        static readonly ProfilerMarker s_ProfileGatherKSetup = new ProfilerMarker("Swarm.GatherK.Setup");
        static readonly ProfilerMarker s_ProfileGatherKScan = new ProfilerMarker("Swarm.GatherK.ScanNeighborCells");

        static readonly ProfilerMarker s_ProfileBoundary = new ProfilerMarker("Swarm.BoundarySteering");
        static readonly ProfilerMarker s_ProfileIntegrate = new ProfilerMarker("Swarm.IntegratePosition");
        static readonly ProfilerMarker s_ProfileClamp = new ProfilerMarker("Swarm.ClampPosition");

        static readonly ProfilerMarker s_ProfileTransforms = new ProfilerMarker("Swarm.ApplyTransforms");

        public static SwarmSimulation Instance { get; private set; }

        [SerializeField] GameConfig gameConfig;

        public GameConfig Config => gameConfig;

        readonly List<Organism> _organisms = new List<Organism>();
        public IReadOnlyList<Organism> RegisteredOrganisms => _organisms;

        /// <summary>Active <see cref="Flower"/> instances; maintained for O(flowers) radius queries (prey nectar).</summary>
        readonly List<Flower> _flowers = new List<Flower>();

        readonly Dictionary<(int cx, int cy), List<Organism>> _spatialGrid =
            new Dictionary<(int cx, int cy), List<Organism>>();

        Candidate[] _kBestBuffer;

        int _physicsStepCounter;

        struct Candidate
        {
            public float DistSq;
            public Organism Neighbor;
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

        void Start()
        {
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultConfigResource);
            if (gameConfig == null || !gameConfig.showSpatialGridInGame)
                return;

            Type overlayType = typeof(SwarmSimulation).Assembly.GetType("Swarm.SpatialGridOverlay");
            if (overlayType == null)
                return;

            var go = new GameObject("SpatialGridOverlay");
            go.transform.SetParent(transform, false);
            Component comp = go.AddComponent(overlayType);
            MethodInfo setCfg = overlayType.GetMethod("SetGameConfig", new[] { typeof(GameConfig) });
            setCfg?.Invoke(comp, new object[] { gameConfig });
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RegisterAgent(Organism organism)
        {
            if (organism == null)
                return;
            _organisms.Add(organism);
            if (organism is Flower flower)
                _flowers.Add(flower);
        }

        public void UnregisterAgent(Organism organism)
        {
            if (organism == null)
                return;
            _organisms.Remove(organism);
            if (organism is Flower flower)
                _flowers.Remove(flower);
        }

        /// <summary>Registered <see cref="Organism"/> instances (non-destroyed); matches how many exist in the simulation.</summary>
        public int LiveOrganismCount
        {
            get
            {
                GetLiveOrganismCounts(out int total, out _, out _);
                return total;
            }
        }

        /// <summary>Single pass over registered agents: total organisms, and how many have <see cref="Predator"/> / <see cref="Prey"/> (an agent may contribute to both).</summary>
        public void GetLiveOrganismCounts(out int total, out int predatorCount, out int preyCount)
        {
            total = 0;
            predatorCount = 0;
            preyCount = 0;
            int n = _organisms.Count;
            for (int i = 0; i < n; i++)
            {
                Organism o = _organisms[i];
                if (o == null)
                    continue;

                total++;
                if (o.GetComponent<Predator>() != null)
                    predatorCount++;
                if (o.GetComponent<Prey>() != null)
                    preyCount++;
            }
        }

        /// <summary>All organisms within <paramref name="radius"/> of <paramref name="position"/> (active only), using current simulation positions.</summary>
        public void CollectOrganismsWithinRadius(Vector2 position, float radius, List<Organism> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            float r2 = radius * radius;
            int n = _organisms.Count;
            for (int i = 0; i < n; i++)
            {
                Organism org = _organisms[i];
                if (org == null || !org.IsActiveInSimulation)
                    continue;

                Vector2 d = org.SimulationPosition - position;
                if (d.sqrMagnitude <= r2)
                    buffer.Add(org);
            }
        }

        /// <summary>
        /// Clears <paramref name="buffer"/> and appends active <see cref="Flower"/> instances with nectar within <paramref name="radius"/> of <paramref name="position"/>.
        /// Uses the maintained flower list (O(flowers)), not a full organism scan.
        /// </summary>
        public void CollectFlowersWithinRadius(Vector2 position, float radius, List<Flower> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            float r2 = radius * radius;
            int n = _flowers.Count;
            for (int i = 0; i < n; i++)
            {
                Flower f = _flowers[i];
                if (f == null || !f.IsActiveInSimulation || f.Nectar <= 0)
                    continue;

                Vector2 d = f.SimulationPosition - position;
                if (d.sqrMagnitude <= r2)
                    buffer.Add(f);
            }
        }

        /// <summary>Valid until the next <see cref="GatherKNearestNeighbors"/> on this instance.</summary>
        public Organism GetGatheredNeighbor(int slot) => _kBestBuffer[slot].Neighbor;

        /// <summary>Fills the internal K-best buffer; neighbors are <see cref="GetGatheredNeighbor"/> for slots <c>[0, return)</c>.</summary>
        public int GatherKNearestNeighbors(Organism self, float cellSize, float neighborRadius, float r2, int maxK)
        {
            if (self == null || gameConfig == null)
                return 0;

            int fill;
            Candidate[] buf;
            Vector2 p;
            int cx;
            int cy;
            int range;
            int maxBucketSamples;

            using (s_ProfileGatherKSetup.Auto())
            {
                EnsureKBestCapacity(maxK);

                buf = _kBestBuffer;
                fill = 0;

                p = self.SimulationPosition;
                cx = Mathf.FloorToInt(p.x / cellSize);
                cy = Mathf.FloorToInt(p.y / cellSize);

                range = Mathf.Max(1, Mathf.CeilToInt(neighborRadius / cellSize));
                maxBucketSamples = gameConfig.vicsekMaxBucketSamples;
            }

            using (s_ProfileGatherKScan.Auto())
            {
                for (int ox = -range; ox <= range; ox++)
                {
                    for (int oy = -range; oy <= range; oy++)
                    {
                        if (!_spatialGrid.TryGetValue((cx + ox, cy + oy), out List<Organism> bucket))
                            continue;

                        int cnt = bucket.Count;
                        int stride = 1;
                        if (maxBucketSamples > 0 && cnt > maxBucketSamples)
                            stride = (cnt + maxBucketSamples - 1) / maxBucketSamples;

                        for (int b = 0; b < cnt; b += stride)
                        {
                            Organism neighbor = bucket[b];
                            if (!neighbor.IsActiveInSimulation)
                                continue;
                            if (!self.IncludeNeighborInGather(neighbor))
                                continue;

                            Vector2 d = neighbor.SimulationPosition - p;
                            float dsq = d.sqrMagnitude;
                            if (dsq > r2)
                                continue;

                            if (fill < maxK)
                            {
                                buf[fill++] = new Candidate { DistSq = dsq, Neighbor = neighbor };
                            }
                            else
                            {
                                int w = IndexOfLargestDistSq(buf, maxK);
                                if (dsq < buf[w].DistSq)
                                    buf[w] = new Candidate { DistSq = dsq, Neighbor = neighbor };
                            }
                        }
                    }
                }
            }

            return fill;
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

            using (s_ProfileRebuildGrid.Auto())
                RebuildSpatialGrid(cellSize, n);

            using (s_ProfileVicsek.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    Organism org = _organisms[i];
                    if (org == null || !org.IsActiveInSimulation)
                        continue;

                    bool runSteering = (i % stagger) == (step % stagger);
                    if (runSteering)
                        org.RunNeighborSteering(this, cellSize, r, r2, maxK);
                }
            }

            for (int i = 0; i < n; i++)
            {
                Organism org = _organisms[i];
                if (org == null || !org.IsActiveInSimulation)
                    continue;
                if (org.SkipsSimulationDynamics)
                    continue;

                using (s_ProfileBoundary.Auto())
                    org.ApplyBoundarySteering(gameConfig);

                using (s_ProfileIntegrate.Auto())
                    org.IntegrateSimulationPosition(dt);

                using (s_ProfileClamp.Auto())
                    org.ClampSimulationPosition(gameConfig);
            }

            using (s_ProfileTransforms.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    Organism org = _organisms[i];
                    if (org == null || !org.IsActiveInSimulation)
                        continue;

                    org.SyncTransformFromSimulation();
                }
            }
        }

        void RebuildSpatialGrid(float cellSize, int n)
        {
            int bucketCap = Mathf.Max(1, gameConfig.spatialGridBucketCapacity);

            using (s_ProfileRebuildClear.Auto())
            {
                foreach (List<Organism> bucket in _spatialGrid.Values)
                    bucket.Clear();
            }

            using (s_ProfileRebuildInsert.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    Organism org = _organisms[i];
                    if (org == null || !org.IsActiveInSimulation)
                        continue;

                    Vector2 pos = org.SimulationPosition;
                    int cx = Mathf.FloorToInt(pos.x / cellSize);
                    int cy = Mathf.FloorToInt(pos.y / cellSize);
                    var key = (cx, cy);
                    if (!_spatialGrid.TryGetValue(key, out List<Organism> list))
                    {
                        list = new List<Organism>(bucketCap);
                        _spatialGrid[key] = list;
                    }

                    list.Add(org);
                }
            }
        }

        void EnsureKBestCapacity(int maxK)
        {
            if (_kBestBuffer == null || _kBestBuffer.Length < maxK)
                _kBestBuffer = new Candidate[maxK];
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
    }
}
