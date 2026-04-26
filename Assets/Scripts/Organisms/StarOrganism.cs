using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StarOrganism : Organism
    {
        public override Vector2 GetAlignmentInfluenceToward(Organism receiver)
        {
            if (receiver == null)
                return Vector2.zero;

            Vector2 away = receiver.SimulationPosition - SimulationPosition;
            float sq = away.sqrMagnitude;
            if (sq < 1e-12f)
                return Vector2.zero;
            return 10 * away * (1f / Mathf.Sqrt(sq));       // TODO: make this a parameter
        }

        protected override void SetupAppearance(GameConfig config, float cruiseSpeed)
        {
            SpriteRenderer.sprite = StarSpriteCache.GetOrCreate(config.triangleSpriteResolution);
            SpriteRenderer.color = ColorForCruiseSpeed(config, cruiseSpeed);
            transform.localScale = Vector3.one * config.triangleScale;
        }

        static class StarSpriteCache
        {
            static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

            public static Sprite GetOrCreate(int resolution)
            {
                if (Cache.TryGetValue(resolution, out Sprite existing))
                    return existing;

                Sprite sprite = BuildStarSprite(resolution);
                Cache[resolution] = sprite;
                return sprite;
            }

            static Sprite BuildStarSprite(int resolution)
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
                float outer = Mathf.Min(w, h) * 0.45f;
                float inner = outer * 0.38f;

                var verts = new Vector2[10];
                for (int k = 0; k < 10; k++)
                {
                    float ang = (90f - k * 36f) * Mathf.Deg2Rad;
                    float r = (k & 1) == 0 ? outer : inner;
                    verts[k] = new Vector2(cx + r * Mathf.Cos(ang), cy - r * Mathf.Sin(ang));
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
