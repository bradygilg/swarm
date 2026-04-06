using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Organism : MonoBehaviour
    {
        SpriteRenderer _spriteRenderer;

        Vector2 _simulationPosition;
        Vector2 _simulationVelocity;
        float _cruiseSpeed;
        bool _activeInSimulation = true;

        public Vector2 SimulationPosition => _simulationPosition;
        public Vector2 SimulationVelocity => _simulationVelocity;
        public float CruiseSpeed => _cruiseSpeed;

        /// <summary>When false, this agent is ignored for Vicsek, spatial queries, and integration.</summary>
        public bool IsActiveInSimulation => _activeInSimulation;

        public void SetSimulationActive(bool active) => _activeInSimulation = active;

        protected SpriteRenderer SpriteRenderer => _spriteRenderer;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(GameConfig config, Vector2 position, float headingRadians, float cruiseSpeed)
        {
            Vector2 velocity = cruiseSpeed * new Vector2(Mathf.Cos(headingRadians), Mathf.Sin(headingRadians));
            Initialize(config, position, velocity, cruiseSpeed);
        }

        /// <summary>Initializes simulation state using an explicit velocity (e.g. for duplicates).</summary>
        public void Initialize(GameConfig config, Vector2 position, Vector2 velocity, float cruiseSpeed)
        {
            if (config == null)
                return;

            _simulationPosition = position;
            SetCruiseSpeed(cruiseSpeed);
            SetVelocity(velocity.sqrMagnitude > 1e-8f ? velocity : cruiseSpeed * Vector2.right);

            transform.position = new Vector3(position.x, position.y, 0f);
            SetupAppearance(config, cruiseSpeed);

            if (SwarmSimulation.Instance == null)
            {
                Debug.LogError(
                    "SwarmSimulation is missing from the scene. Add a SwarmSimulation component (Game scene).",
                    this);
                return;
            }

            SwarmSimulation.Instance.RegisterAgent(this);
            ApplyRotationVisual(SimulationVelocity);
        }

        void OnDestroy()
        {
            if (SwarmSimulation.Instance != null)
                SwarmSimulation.Instance.UnregisterAgent(this);
        }

        /// <summary>Replaces simulation velocity (magnitude and direction).</summary>
        public void SetVelocity(Vector2 velocity) => _simulationVelocity = velocity;

        /// <summary>Preferred speed magnitude used by boundary blending and base Vicsek.</summary>
        public void SetCruiseSpeed(float cruiseSpeed) => _cruiseSpeed = Mathf.Max(1e-4f, cruiseSpeed);

        /// <summary>Whether <paramref name="other"/> may appear in K-nearest gather for this agent.</summary>
        protected internal virtual bool IncludeNeighborInGather(Organism other)
        {
            if (other == null || ReferenceEquals(other, this))
                return false;
            if (!other.IsActiveInSimulation)
                return false;
            return true;
        }

        public void ApplyBoundarySteering(GameConfig cfg)
        {
            if (cfg == null)
                return;

            Vector2 center = cfg.spawnCircleCenter;
            float R = cfg.boundingDomainRadius;
            float band = Mathf.Min(cfg.boundingRepulsionBandWidth, R - 1e-4f);
            Vector2 p = _simulationPosition;
            Vector2 offset = p - center;
            float dist = offset.magnitude;
            float inner = R - band;
            if (dist <= inner || dist < 1e-6f)
                return;

            Vector2 toCenter = -offset / dist;
            float w = Mathf.Clamp01((dist - inner) / band);
            w = w * w * (3f - 2f * w);

            Vector2 vel = _simulationVelocity;
            Vector2 steerDir = vel.sqrMagnitude > 1e-8f ? vel.normalized : toCenter;
            Vector2 dir = Vector2.Lerp(steerDir, toCenter, w).normalized;
            SetVelocity(dir * _cruiseSpeed);
        }

        public void IntegrateSimulationPosition(float dt) => _simulationPosition += _simulationVelocity * dt;

        public void ClampSimulationPosition(GameConfig cfg)
        {
            if (cfg == null)
                return;

            Vector2 center = cfg.spawnCircleCenter;
            float R = cfg.boundingDomainRadius;
            const float eps = 1e-3f;
            Vector2 p = _simulationPosition;
            Vector2 offset = p - center;
            float dist = offset.magnitude;
            if (dist <= R - eps || dist < 1e-6f)
                return;

            float scale = (R - eps) / dist;
            _simulationPosition = new Vector2(
                center.x + offset.x * scale,
                center.y + offset.y * scale);
        }

        public void SyncTransformFromSimulation()
        {
            Vector2 p = _simulationPosition;
            Vector2 v = _simulationVelocity;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected virtual void SetupAppearance(GameConfig config, float cruiseSpeed)
        {
            _spriteRenderer.sprite = TriangleSpriteCache.GetOrCreate(config.triangleSpriteResolution);
            _spriteRenderer.color = ColorForCruiseSpeed(config, cruiseSpeed);
            transform.localScale = Vector3.one * config.triangleScale;
        }

        public void RunNeighborSteering(SwarmSimulation sim, float cellSize, float neighborRadius, float neighborRadiusSq, int maxK)
        {
            if (sim == null)
                return;

            int fill = sim.GatherKNearestNeighbors(this, cellSize, neighborRadius, neighborRadiusSq, maxK);
            UpdateNeighborSteering(sim, fill, maxK);
        }

        protected virtual void UpdateNeighborSteering(SwarmSimulation sim, int fill, int maxK)
        {
            if (!TryComputeMeanAlignmentHeading(sim, fill, out float theta))
                return;

            GameConfig cfg = sim.Config;
            theta += ApplyAngularNoiseRadians(cfg);
            SetVelocity(CruiseSpeed * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)));
        }

        /// <summary>Direction this agent contributes toward <paramref name="receiver"/>'s Vicsek mean (velocity by default).</summary>
        public virtual Vector2 GetAlignmentInfluenceToward(Organism receiver)
        {
            Vector2 v = _simulationVelocity;
            return v.sqrMagnitude < 1e-8f ? Vector2.zero : v.normalized;
        }

        /// <summary>How <paramref name="neighbor"/> is folded into this agent's mean (subclasses may reweight).</summary>
        protected virtual Vector2 NeighborAlignmentContribution(Organism neighbor) =>
            neighbor.GetAlignmentInfluenceToward(this);

        protected bool TryComputeMeanAlignmentHeading(SwarmSimulation sim, int fill, out float headingRadians)
        {
            headingRadians = 0f;
            if (fill == 0)
                return false;

            Vector2 sum = Vector2.zero;
            int counted = 0;
            for (int c = 0; c < fill; c++)
            {
                Organism nbr = sim.GetGatheredNeighbor(c);
                Vector2 contrib = NeighborAlignmentContribution(nbr);
                if (contrib.sqrMagnitude < 1e-12f)
                    continue;

                sum += contrib;
                counted++;
            }

            if (counted == 0)
                return false;

            Vector2 avg = sum / counted;
            if (avg.sqrMagnitude < 1e-8f)
                return false;

            headingRadians = Mathf.Atan2(avg.y, avg.x);
            return true;
        }

        protected static float ApplyAngularNoiseRadians(GameConfig cfg)
        {
            if (cfg == null || cfg.angularNoiseDegrees <= 0f)
                return 0f;
            return SampleGaussianStandard() * cfg.angularNoiseDegrees * Mathf.Deg2Rad;
        }

        protected static float SampleGaussianStandard()
        {
            float u1 = 1f - Random.value;
            if (u1 < 1e-7f)
                u1 = 1e-7f;
            float u2 = Random.value;
            return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        }

        protected static Color ColorForCruiseSpeed(GameConfig config, float cruiseSpeed)
        {
            float vmin = Mathf.Min(config.organismSpeedMin, config.organismSpeedMax);
            float vmax = Mathf.Max(config.organismSpeedMin, config.organismSpeedMax);
            float t = vmax > vmin ? Mathf.Clamp01((cruiseSpeed - vmin) / (vmax - vmin)) : 0.5f;
            return Color.Lerp(config.organismColorAtMinSpeed, config.organismColorAtMaxSpeed, t);
        }

        protected void ApplyRotationVisual(Vector2 velocity)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        static class TriangleSpriteCache
        {
            static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

            public static Sprite GetOrCreate(int resolution)
            {
                if (Cache.TryGetValue(resolution, out Sprite existing))
                    return existing;

                Sprite sprite = BuildTriangleSprite(resolution);
                Cache[resolution] = sprite;
                return sprite;
            }

            static Sprite BuildTriangleSprite(int resolution)
            {
                int w = Mathf.Max(8, resolution);
                int h = Mathf.Max(8, resolution);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                Color32 clear = new Color32(0, 0, 0, 0);
                Color32 fill = Color.white;
                var pixels = new Color32[w * h];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = clear;

                Vector2 v0 = new Vector2(w - 2f, h * 0.5f);
                Vector2 v1 = new Vector2(2f, 2f);
                Vector2 v2 = new Vector2(2f, h - 2f);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        if (PointInTriangle(p, v0, v1, v2))
                            pixels[y * w + x] = fill;
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                const float pixelsPerUnit = 100f;
                return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }

            static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
                float c1 = Cross(b - a, p - a);
                float c2 = Cross(c - b, p - b);
                float c3 = Cross(a - c, p - c);
                bool neg = (c1 < 0f) || (c2 < 0f) || (c3 < 0f);
                bool pos = (c1 > 0f) || (c2 > 0f) || (c3 > 0f);
                return !(neg && pos);
            }
        }
    }
}
