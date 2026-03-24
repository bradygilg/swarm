using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Organism : MonoBehaviour
    {
        SpriteRenderer _spriteRenderer;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>Visual setup + registration with <see cref="SwarmSimulation"/>.</summary>
        public void Initialize(GameConfig config, Vector2 position, float headingRadians, float cruiseSpeed)
        {
            if (config == null)
                return;

            Vector2 velocity = cruiseSpeed * new Vector2(Mathf.Cos(headingRadians), Mathf.Sin(headingRadians));
            transform.position = new Vector3(position.x, position.y, 0f);
            _spriteRenderer.sprite = TriangleSpriteCache.GetOrCreate(config.triangleSpriteResolution);
            _spriteRenderer.color = ColorForCruiseSpeed(config, cruiseSpeed);
            transform.localScale = Vector3.one * config.triangleScale;

            if (SwarmSimulation.Instance == null)
            {
                Debug.LogError(
                    "SwarmSimulation is missing from the scene. Add a SwarmSimulation component (Game scene).",
                    this);
                return;
            }

            SwarmSimulation.Instance.RegisterAgent(this, position, velocity, cruiseSpeed);
            ApplyRotationVisual(velocity);
        }

        static Color ColorForCruiseSpeed(GameConfig config, float cruiseSpeed)
        {
            float vmin = Mathf.Min(config.organismSpeedMin, config.organismSpeedMax);
            float vmax = Mathf.Max(config.organismSpeedMin, config.organismSpeedMax);
            float t = vmax > vmin ? Mathf.Clamp01((cruiseSpeed - vmin) / (vmax - vmin)) : 0.5f;
            return Color.Lerp(config.organismColorAtMinSpeed, config.organismColorAtMaxSpeed, t);
        }

        void ApplyRotationVisual(Vector2 velocity)
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
