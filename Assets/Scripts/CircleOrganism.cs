using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CircleOrganism : Organism
    {
        protected override void SetupAppearance(GameConfig config, float cruiseSpeed)
        {
            SpriteRenderer.sprite = CircleSpriteCache.GetOrCreate(config.triangleSpriteResolution);
            SpriteRenderer.color = ColorForCruiseSpeed(config, cruiseSpeed);
            transform.localScale = Vector3.one * config.triangleScale;
        }

        protected override void UpdateNeighborSteering(SwarmSimulation sim, int fill, int maxK)
        {
            if (!TryComputeMeanAlignmentHeading(sim, fill, out float theta))
                return;

            GameConfig cfg = sim.Config;
            float densityT = maxK > 0 ? fill / (float)maxK : 0f;
            float speed = Mathf.Lerp(cfg.circleCruiseSpeedLowDensity, cfg.circleCruiseSpeedHighDensity, densityT);
            SetCruiseSpeed(speed);

            theta += ApplyAngularNoiseRadians(cfg);
            SetVelocity(speed * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)));
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
                Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
                float radius = Mathf.Min(w, h) * 0.5f - 1f;
                float r2 = radius * radius;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        pixels[y * w + x] = (p - c).sqrMagnitude <= r2 ? fill : clear;
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                const float pixelsPerUnit = 100f;
                return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }
        }
    }
}
