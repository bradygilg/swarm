using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    /// <summary>
    /// Leaves a fading line behind the attached organism and repels nearby organisms from the trail.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Organism))]
    public sealed class TrailingWall : OrganismType
    {
        const float MinFadeDuration = 0.05f;
        const float MinSampleDistance = 0.005f;
        const float MinWidth = 0.001f;
        const float LineZOffset = 0.025f;
        const string DefaultConfigResource = "GameConfig";
        const int MaxGradientKeys = 8;

        struct TrailPoint
        {
            public Vector2 Position;
            public float TimeCreated;
            public Color SampledColor;
        }

        [Header("Trail Visual")]
        [SerializeField] bool followOrganismColor = true;
        [SerializeField] Color lineColor = Color.white;
        [SerializeField, Min(MinFadeDuration)] float fadeDurationSeconds = 2f;
        [SerializeField, Min(MinWidth)] float lineWidth = 0.08f;
        [SerializeField, Min(MinSampleDistance)] float sampleDistance = 0.08f;

        [Header("Trail Repulsion")]
        [SerializeField, Min(0f)] float repulsionRadius = 0.5f;
        [SerializeField, Min(0f)] float repulsionStrength = 0.85f;

        [Header("Performance")]
        [SerializeField, Min(0.01f)] float visualUpdateIntervalSeconds = 0.05f;
        [SerializeField, Min(0.01f)] float repulsionUpdateIntervalSeconds = 0.1f;
        [SerializeField, Min(8)] int maxTrailPoints = 96;
        [SerializeField, Min(1)] int maxRepulsionSegments = 24;

        [Header("Defaults")]
        [Tooltip("When enabled, initialize this component's values from GameConfig during Awake.")]
        [SerializeField] bool applyConfigDefaultsOnAwake = true;

        readonly List<TrailPoint> _points = new List<TrailPoint>(256);
        readonly List<GradientColorKey> _colorKeys = new List<GradientColorKey>(256);
        readonly List<GradientAlphaKey> _alphaKeys = new List<GradientAlphaKey>(256);

        Organism _organism;
        SpriteRenderer _spriteRenderer;
        LineRenderer _lineRenderer;
        SwarmSimulation _simulation;
        GameConfig _config;
        Gradient _gradient;
        float _nextVisualUpdateAt;
        float _nextRepulsionUpdateAt;

        public bool FollowOrganismColor
        {
            get => followOrganismColor;
            set => followOrganismColor = value;
        }

        public Color LineColor
        {
            get => lineColor;
            set => lineColor = value;
        }

        public float FadeDurationSeconds
        {
            get => fadeDurationSeconds;
            set => fadeDurationSeconds = Mathf.Max(MinFadeDuration, value);
        }

        void Awake()
        {
            _organism = GetComponent<Organism>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _simulation = SwarmSimulation.Instance;
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _gradient = new Gradient();

            ConfigureLineRenderer();
            TryApplyConfigDefaults();
            AddSamplePoint(force: true);
        }

        void Update()
        {
            if (_organism == null || !_organism.IsActiveInSimulation)
                return;

            TrimExpiredPoints();
            bool sampled = TrySampleByDistance();
            EnforceTrailPointLimit();

            float now = Time.time;
            if (sampled || now >= _nextVisualUpdateAt)
            {
                RebuildLine();
                _nextVisualUpdateAt = now + visualUpdateIntervalSeconds;
            }
        }

        void FixedUpdate()
        {
            if (_organism == null || !_organism.IsActiveInSimulation)
                return;
            if (repulsionRadius <= 1e-6f || repulsionStrength <= 1e-6f || _points.Count < 2)
                return;
            if (Time.time < _nextRepulsionUpdateAt)
                return;

            if (_simulation == null)
                _simulation = SwarmSimulation.Instance;
            if (_simulation == null)
                return;

            IReadOnlyList<Organism> organisms = _simulation.RegisteredOrganisms;
            if (organisms == null)
                return;

            int count = organisms.Count;
            float minX;
            float minY;
            float maxX;
            float maxY;
            if (!TryGetExpandedTrailBounds(repulsionRadius, out minX, out minY, out maxX, out maxY))
                return;

            for (int i = 0; i < count; i++)
            {
                Organism other = organisms[i];
                if (other == null || other == _organism || !other.IsActiveInSimulation || other.SkipsSimulationDynamics)
                    continue;

                Vector2 position = other.SimulationPosition;
                if (position.x < minX || position.x > maxX || position.y < minY || position.y > maxY)
                    continue;
                if (!TryGetClosestPointOnTrail(position, out Vector2 closest))
                    continue;

                Vector2 away = position - closest;
                float dist = away.magnitude;
                if (dist <= 1e-6f || dist > repulsionRadius)
                    continue;

                float w = Mathf.Clamp01(1f - (dist / repulsionRadius));
                w = w * w * (3f - 2f * w);
                w = Mathf.Clamp01(w * repulsionStrength);

                Vector2 awayDir = away / dist;
                Vector2 velocity = other.SimulationVelocity;
                Vector2 steerDir = velocity.sqrMagnitude > 1e-8f ? velocity.normalized : awayDir;
                Vector2 blended = Vector2.Lerp(steerDir, awayDir, w);
                if (blended.sqrMagnitude < 1e-8f)
                    continue;

                other.SetVelocity(blended.normalized * other.CruiseSpeed);
            }

            _nextRepulsionUpdateAt = Time.time + repulsionUpdateIntervalSeconds;
        }

        void OnValidate()
        {
            fadeDurationSeconds = Mathf.Max(MinFadeDuration, fadeDurationSeconds);
            sampleDistance = Mathf.Max(MinSampleDistance, sampleDistance);
            lineWidth = Mathf.Max(MinWidth, lineWidth);
            repulsionRadius = Mathf.Max(0f, repulsionRadius);
            repulsionStrength = Mathf.Max(0f, repulsionStrength);
            visualUpdateIntervalSeconds = Mathf.Max(0.01f, visualUpdateIntervalSeconds);
            repulsionUpdateIntervalSeconds = Mathf.Max(0.01f, repulsionUpdateIntervalSeconds);
            maxTrailPoints = Mathf.Max(8, maxTrailPoints);
            maxRepulsionSegments = Mathf.Max(1, maxRepulsionSegments);

            if (_lineRenderer != null)
            {
                _lineRenderer.startWidth = lineWidth;
                _lineRenderer.endWidth = lineWidth;
            }
        }

        void ConfigureLineRenderer()
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.loop = false;
            _lineRenderer.positionCount = 0;
            _lineRenderer.numCapVertices = 0;
            _lineRenderer.numCornerVertices = 0;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.sortingOrder = 0;

            if (_lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                    _lineRenderer.material = new Material(shader);
            }
        }

        void TryApplyConfigDefaults()
        {
            if (!applyConfigDefaultsOnAwake)
                return;

            if (_config == null)
                _config = _simulation != null ? _simulation.Config : Resources.Load<GameConfig>(DefaultConfigResource);
            if (_config == null)
                return;
            OrganismTypeConfig typeCfg = _config.OrganismTypeConfig;

            fadeDurationSeconds = Mathf.Max(MinFadeDuration, typeCfg.trailingWallFadeDurationSeconds);
            lineWidth = Mathf.Max(MinWidth, typeCfg.trailingWallLineWidth);
            sampleDistance = Mathf.Max(MinSampleDistance, typeCfg.trailingWallSampleDistance);
            repulsionRadius = Mathf.Max(0f, typeCfg.trailingWallRepulsionRadius);
            repulsionStrength = Mathf.Max(0f, typeCfg.trailingWallRepulsionStrength);
            lineColor = typeCfg.trailingWallColor;
            followOrganismColor = typeCfg.trailingWallUseOrganismColor;
            ApplyQualityPreset(_config, typeCfg);

            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.sortingOrder = typeCfg.trailingWallSortingOrder;
            _lineRenderer.numCapVertices = Mathf.Max(0, typeCfg.trailingWallCapVertices);
            _lineRenderer.numCornerVertices = Mathf.Max(0, typeCfg.trailingWallCornerVertices);
        }

        void ApplyQualityPreset(GameConfig cfg, OrganismTypeConfig typeCfg)
        {
            switch (cfg.trailingWallQualityPreset)
            {
                case TrailingWallQualityPreset.Low:
                    visualUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallLowVisualUpdateIntervalSeconds);
                    repulsionUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallLowRepulsionUpdateIntervalSeconds);
                    maxTrailPoints = Mathf.Max(8, typeCfg.trailingWallLowMaxTrailPoints);
                    maxRepulsionSegments = Mathf.Max(1, typeCfg.trailingWallLowMaxRepulsionSegments);
                    break;
                case TrailingWallQualityPreset.High:
                    visualUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallHighVisualUpdateIntervalSeconds);
                    repulsionUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallHighRepulsionUpdateIntervalSeconds);
                    maxTrailPoints = Mathf.Max(8, typeCfg.trailingWallHighMaxTrailPoints);
                    maxRepulsionSegments = Mathf.Max(1, typeCfg.trailingWallHighMaxRepulsionSegments);
                    break;
                case TrailingWallQualityPreset.Custom:
                    visualUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallCustomVisualUpdateIntervalSeconds);
                    repulsionUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallCustomRepulsionUpdateIntervalSeconds);
                    maxTrailPoints = Mathf.Max(8, typeCfg.trailingWallCustomMaxTrailPoints);
                    maxRepulsionSegments = Mathf.Max(1, typeCfg.trailingWallCustomMaxRepulsionSegments);
                    break;
                default:
                    visualUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallMediumVisualUpdateIntervalSeconds);
                    repulsionUpdateIntervalSeconds = Mathf.Max(0.01f, typeCfg.trailingWallMediumRepulsionUpdateIntervalSeconds);
                    maxTrailPoints = Mathf.Max(8, typeCfg.trailingWallMediumMaxTrailPoints);
                    maxRepulsionSegments = Mathf.Max(1, typeCfg.trailingWallMediumMaxRepulsionSegments);
                    break;
            }
        }

        bool TrySampleByDistance()
        {
            if (_points.Count == 0)
            {
                AddSamplePoint(force: true);
                return true;
            }

            Vector2 current = _organism.SimulationPosition;
            Vector2 last = _points[_points.Count - 1].Position;
            if ((current - last).sqrMagnitude >= sampleDistance * sampleDistance)
            {
                AddSamplePoint(force: false);
                return true;
            }

            return false;
        }

        void AddSamplePoint(bool force)
        {
            Vector2 p = _organism != null ? _organism.SimulationPosition : (Vector2)transform.position;
            if (!force && _points.Count > 0)
            {
                Vector2 last = _points[_points.Count - 1].Position;
                if ((p - last).sqrMagnitude < sampleDistance * sampleDistance)
                    return;
            }

            _points.Add(new TrailPoint
            {
                Position = p,
                TimeCreated = Time.time,
                SampledColor = GetCurrentOrganismColor()
            });
        }

        Color GetCurrentOrganismColor()
        {
            if (followOrganismColor && _spriteRenderer != null)
                return _spriteRenderer.color;
            return Color.white;
        }

        void TrimExpiredPoints()
        {
            if (_points.Count == 0)
                return;

            float expiryThreshold = Time.time - fadeDurationSeconds;
            int removeCount = 0;
            int count = _points.Count;
            for (int i = 0; i < count; i++)
            {
                if (_points[i].TimeCreated >= expiryThreshold)
                    break;
                removeCount++;
            }

            if (removeCount > 0)
                _points.RemoveRange(0, removeCount);
        }

        void EnforceTrailPointLimit()
        {
            int overflow = _points.Count - maxTrailPoints;
            if (overflow > 0)
                _points.RemoveRange(0, overflow);
        }

        void RebuildLine()
        {
            int count = _points.Count;
            if (count == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            _colorKeys.Clear();
            _alphaKeys.Clear();

            float fadeDuration = Mathf.Max(MinFadeDuration, fadeDurationSeconds);
            _lineRenderer.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                TrailPoint point = _points[i];
                _lineRenderer.SetPosition(i, new Vector3(point.Position.x, point.Position.y, LineZOffset));
            }

            int gradientKeyCount = Mathf.Min(count, MaxGradientKeys);
            for (int k = 0; k < gradientKeyCount; k++)
            {
                int i = gradientKeyCount == 1 ? count - 1 : Mathf.RoundToInt(k * (count - 1f) / (gradientKeyCount - 1f));
                TrailPoint point = _points[i];
                float t = count > 1 ? i / (float)(count - 1) : 0f;
                float age = Mathf.Clamp01((Time.time - point.TimeCreated) / fadeDuration);
                float alpha = 1f - age;

                Color sourceColor = followOrganismColor ? point.SampledColor : Color.white;
                Color finalColor = new Color(
                    sourceColor.r * lineColor.r,
                    sourceColor.g * lineColor.g,
                    sourceColor.b * lineColor.b,
                    alpha * lineColor.a);

                _colorKeys.Add(new GradientColorKey(finalColor, t));
                _alphaKeys.Add(new GradientAlphaKey(finalColor.a, t));
            }

            _gradient.SetKeys(_colorKeys.ToArray(), _alphaKeys.ToArray());
            _lineRenderer.colorGradient = _gradient;
        }

        bool TryGetClosestPointOnTrail(Vector2 query, out Vector2 closest)
        {
            closest = default;
            if (_points.Count < 2)
                return false;

            float bestDistSq = float.PositiveInfinity;
            bool found = false;
            int segmentCount = _points.Count - 1;
            int stride = Math.Max(1, Mathf.CeilToInt(segmentCount / (float)Math.Max(1, maxRepulsionSegments)));
            for (int i = stride; i < _points.Count; i += stride)
            {
                int aIndex = i - stride;
                Vector2 a = _points[aIndex].Position;
                Vector2 b = _points[i].Position;
                Vector2 c = ClosestPointOnSegment(query, a, b);
                float d2 = (query - c).sqrMagnitude;
                if (d2 >= bestDistSq)
                    continue;
                bestDistSq = d2;
                closest = c;
                found = true;
            }

            return found;
        }

        bool TryGetExpandedTrailBounds(float padding, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = maxX = maxY = 0f;
            if (_points.Count == 0)
                return false;

            Vector2 p = _points[0].Position;
            minX = maxX = p.x;
            minY = maxY = p.y;
            for (int i = 1; i < _points.Count; i++)
            {
                p = _points[i].Position;
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;
            return true;
        }

        static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom <= 1e-8f)
                return a;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom);
            return a + t * ab;
        }
    }
}
