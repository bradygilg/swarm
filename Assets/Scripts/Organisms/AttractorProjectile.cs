using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    /// <summary>
    /// Temporary moving attractor spawned by the player. Pulls NPC Vicsek heading toward its position.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AttractorProjectile : Resource
    {
        Vector2 _origin;
        Vector2 _direction = Vector2.right;
        float _speed;
        float _lifetimeSeconds;
        float _maxDistance;
        float _vicsekWeight;
        float _attractionStrength;
        float _spawnTime;
        Color _color = Color.white;
        float _visualScale = 0.22f;

        public void Configure(
            Vector2 direction,
            float speed,
            float lifetimeSeconds,
            float maxDistance,
            float vicsekWeight,
            float attractionStrength,
            float visualScale,
            Color color)
        {
            _direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.right;
            _speed = Mathf.Max(0.01f, speed);
            _lifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
            _maxDistance = Mathf.Max(0.01f, maxDistance);
            _vicsekWeight = Mathf.Max(0f, vicsekWeight);
            _attractionStrength = Mathf.Max(0f, attractionStrength);
            _visualScale = Mathf.Max(0.01f, visualScale);
            _color = color;
        }

        public override float GetVicsekWeightForReceiver(Organism receiver, SwarmSimulation sim)
        {
            if (receiver == null)
                return 0f;
            if (receiver.GetComponent<Predator>() != null || receiver.GetComponent<Prey>() != null)
                return _vicsekWeight;
            return 0f;
        }

        public override Vector2 GetAlignmentInfluenceToward(Organism receiver)
        {
            if (receiver == null)
                return Vector2.zero;

            Vector2 toProjectile = SimulationPosition - receiver.SimulationPosition;
            float sq = toProjectile.sqrMagnitude;
            if (sq < 1e-10f)
                return Vector2.zero;
            return _attractionStrength * (toProjectile / Mathf.Sqrt(sq));
        }

        public override void Initialize(GameConfig config, Vector2 position, Vector2 velocity, float cruiseSpeed)
        {
            base.Initialize(config, position, velocity, Mathf.Max(cruiseSpeed, 0.01f));
            _origin = position;
            _spawnTime = Time.time;
            SetEatable(false);
            SetVelocity(_direction * _speed);
        }

        void FixedUpdate()
        {
            if (!IsActiveInSimulation)
                return;

            float age = Time.time - _spawnTime;
            if (age >= _lifetimeSeconds)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 next = SimulationPosition + (_direction * _speed * Time.fixedDeltaTime);
            SetSimulationPosition(next);
            SetVelocity(_direction * _speed);

            if ((next - _origin).sqrMagnitude >= _maxDistance * _maxDistance)
                Destroy(gameObject);
        }

        protected override void SetupAppearance(GameConfig config, float cruiseSpeed)
        {
            SpriteRenderer.sprite = CircleSpriteCache.GetOrCreate(24);
            SpriteRenderer.color = _color;
            SpriteRenderer.sortingOrder = -40;
            transform.localScale = Vector3.one * _visualScale;
        }

        static class CircleSpriteCache
        {
            static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

            public static Sprite GetOrCreate(int resolution)
            {
                if (Cache.TryGetValue(resolution, out Sprite existing))
                    return existing;

                Sprite sprite = BuildCircleSprite(resolution);
                Cache[resolution] = sprite;
                return sprite;
            }

            static Sprite BuildCircleSprite(int resolution)
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
                float cx = (w - 1) * 0.5f;
                float cy = (h - 1) * 0.5f;
                float r = Mathf.Min(w, h) * 0.5f - 1f;
                float r2 = r * r;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        pixels[y * w + x] = (dx * dx + dy * dy <= r2) ? fill : clear;
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }
}
