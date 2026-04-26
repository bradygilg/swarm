using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    /// <summary>Yellow pentagon resource with nectar; Vicsek influence pulls neighbors toward it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Flower : Resource
    {
        int _nectar;
        int _initialNectar;

        public int Nectar => _nectar;

        public override float GetVicsekWeightForReceiver(Organism receiver, SwarmSimulation sim)
        {
            if (receiver != null && receiver.TryGetComponent<Prey>(out var prey))
                return prey.GetVicsekWeightForFlowerNeighbor(sim);
            return 1f;
        }

        public override Vector2 GetAlignmentInfluenceToward(Organism receiver)
        {
            if (receiver == null)
                return Vector2.zero;

            Vector2 toFlower = SimulationPosition - receiver.SimulationPosition;
            float sq = toFlower.sqrMagnitude;
            if (sq < 1e-12f)
                return Vector2.zero;
            return toFlower * (1f / Mathf.Sqrt(sq));
        }

        /// <summary>Spawn / pool entry: place in world with nectar from <paramref name="config"/>.</summary>
        public void Setup(GameConfig config, Vector2 position)
        {
            if (config == null)
                return;

            _initialNectar = Mathf.Max(0, config.flowerInitialNectar);
            _nectar = _initialNectar;
            if (_nectar <= 0)
            {
                Destroy(gameObject);
                return;
            }

            Initialize(config, position, Vector2.zero, 0.5f);
        }

        protected override void SetupAppearance(GameConfig config, float cruiseSpeed)
        {
            SpriteRenderer.sprite = PentagonSpriteCache.GetOrCreate(config.flowerSpriteResolution);
            SpriteRenderer.color = new Color(1f, 0.92f, 0.15f, 1f);
            SpriteRenderer.sortingOrder = -50;
            RefreshVisualScale(config);
        }

        public void RefreshVisualScale(GameConfig config)
        {
            if (config == null)
                return;

            float t = _initialNectar > 0 ? Mathf.Clamp01(_nectar / (float)_initialNectar) : 0f;
            float s = Mathf.Lerp(config.flowerMinVisualScale, config.flowerMaxVisualScale, t);
            transform.localScale = Vector3.one * s;
        }

        /// <summary>Removes up to <paramref name="amount"/> nectar; returns amount actually removed. Despawns when empty.</summary>
        public int ConsumeNectar(int amount)
        {
            if (amount <= 0 || _nectar <= 0)
                return 0;

            int removed = Mathf.Min(amount, _nectar);
            _nectar -= removed;

            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : Resources.Load<GameConfig>("GameConfig");
            if (cfg != null)
                RefreshVisualScale(cfg);

            if (_nectar <= 0)
                Destroy(gameObject);

            return removed;
        }

        static class PentagonSpriteCache
        {
            static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

            public static Sprite GetOrCreate(int resolution)
            {
                if (Cache.TryGetValue(resolution, out Sprite existing))
                    return existing;

                Sprite sprite = BuildPentagonSprite(resolution);
                Cache[resolution] = sprite;
                return sprite;
            }

            static Sprite BuildPentagonSprite(int resolution)
            {
                int w = Mathf.Max(8, resolution);
                int h = Mathf.Max(8, resolution);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                float cx = w * 0.5f;
                float cy = h * 0.5f;
                float outer = Mathf.Min(w, h) * 0.5f - 1f;

                var verts = new Vector2[5];
                for (int k = 0; k < 5; k++)
                {
                    float ang = (90f - k * 72f) * Mathf.Deg2Rad;
                    verts[k] = new Vector2(cx + outer * Mathf.Cos(ang), cy - outer * Mathf.Sin(ang));
                }

                Color32 clear = new Color32(0, 0, 0, 0);
                Color32 fill = Color.white;
                var pixels = new Color32[w * h];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = clear;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        if (PointInPolygon(p, verts))
                            pixels[y * w + x] = fill;
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                const float pixelsPerUnit = 100f;
                return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }

            static bool PointInPolygon(Vector2 p, Vector2[] poly)
            {
                bool inside = false;
                int n = poly.Length;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                        (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                        inside = !inside;
                }

                return inside;
            }
        }
    }
}
