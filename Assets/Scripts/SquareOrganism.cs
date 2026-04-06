using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SquareOrganism : Organism
    {
        static readonly Type[] s_GetAlignmentParamTypes = { typeof(Organism) };
        static readonly ConcurrentDictionary<Type, bool> s_NeighborUsesOrganismDeclaredGetAlignment =
            new ConcurrentDictionary<Type, bool>();

        /// <summary>True if <paramref name="neighbor"/>'s runtime type still uses <see cref="Organism.GetAlignmentInfluenceToward"/> as declared on <see cref="Organism"/> (not overridden on a subclass).</summary>
        static bool NeighborUsesOrganismDeclaredGetAlignmentInfluence(Organism neighbor)
        {
            if (neighbor == null)
                return true;

            Type t = neighbor.GetType();
            return s_NeighborUsesOrganismDeclaredGetAlignment.GetOrAdd(t, ComputeUsesOrganismDeclaredGetAlignment);
        }

        static bool ComputeUsesOrganismDeclaredGetAlignment(Type type)
        {
            MethodInfo mi = type.GetMethod(
                nameof(Organism.GetAlignmentInfluenceToward),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: s_GetAlignmentParamTypes,
                modifiers: null);

            return mi != null && mi.DeclaringType == typeof(Organism);
        }

        /// <summary>Align with other squares; oppose neighbors whose <see cref="Organism.GetAlignmentInfluenceToward"/> is not overridden.</summary>
        protected override Vector2 NeighborAlignmentContribution(Organism neighbor)
        {
            Vector2 dir = neighbor.GetAlignmentInfluenceToward(this);
            if (neighbor is SquareOrganism)
                return dir;
            if (NeighborUsesOrganismDeclaredGetAlignmentInfluence(neighbor))
                return -dir;
            return dir;
        }

        protected override void SetupAppearance(GameConfig config, float cruiseSpeed)
        {
            SpriteRenderer.sprite = SquareSpriteCache.GetOrCreate(config.triangleSpriteResolution);
            SpriteRenderer.color = ColorForCruiseSpeed(config, cruiseSpeed);
            transform.localScale = Vector3.one * config.triangleScale;
        }

        static class SquareSpriteCache
        {
            static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

            public static Sprite GetOrCreate(int resolution)
            {
                if (Cache.TryGetValue(resolution, out Sprite existing))
                    return existing;

                Sprite sprite = BuildSquareSprite(resolution);
                Cache[resolution] = sprite;
                return sprite;
            }

            static Sprite BuildSquareSprite(int resolution)
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
                int margin = Mathf.Max(1, w / 16);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        bool inside = x >= margin && x < w - margin && y >= margin && y < h - margin;
                        pixels[y * w + x] = inside ? fill : clear;
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
