using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swarm
{
    /// <summary>
    /// Draws world-space lines for the CPU Vicsek spatial hash when <see cref="GameConfig.showSpatialGridInGame"/>
    /// is enabled (boundaries at multiples of cell size, matching <see cref="SwarmSimulation"/> indexing).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpatialGridOverlay : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";

        [SerializeField] GameConfig gameConfig;

        readonly List<LineRenderer> _linePool = new List<LineRenderer>();
        Material _lineMaterial;

        /// <summary>Called from <see cref="SwarmSimulation"/> so the overlay reads the same <see cref="GameConfig"/> asset as the simulation.</summary>
        public void SetGameConfig(GameConfig config)
        {
            if (config != null)
                gameConfig = config;
        }

        GameConfig ResolveConfig()
        {
            if (SwarmSimulation.Instance != null && SwarmSimulation.Instance.Config != null)
                return SwarmSimulation.Instance.Config;
            if (gameConfig != null)
                return gameConfig;
            return Resources.Load<GameConfig>(DefaultConfigResource);
        }

        void Awake()
        {
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultConfigResource);

            _lineMaterial = CreateGridLineMaterial();
        }

        static Material CreateGridLineMaterial()
        {
            // Sprites/Default handles vertex colors + alpha well in 2D URP; URP Unlit as fallback.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader) { hideFlags = HideFlags.DontSave };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color"))
                mat.color = Color.white;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            return mat;
        }

        void LateUpdate()
        {
            GameConfig cfg = ResolveConfig();
            if (cfg == null)
            {
                DisableAllLines();
                return;
            }

            if (!cfg.showSpatialGridInGame)
            {
                DisableAllLines();
                return;
            }

            float r = cfg.neighborRadius;
            int subdivisions = Mathf.Max(1, cfg.vicsekSpatialSubdivisions);
            float cellSize = Mathf.Max(r / subdivisions, 1e-4f);
            Vector2 center = cfg.spawnCircleCenter;
            float extent = cfg.boundingDomainRadius + cellSize;

            float xMin = center.x - extent;
            float xMax = center.x + extent;
            float yMin = center.y - extent;
            float yMax = center.y + extent;

            int iMin = Mathf.FloorToInt(xMin / cellSize);
            int iMax = Mathf.CeilToInt(xMax / cellSize);
            int jMin = Mathf.FloorToInt(yMin / cellSize);
            int jMax = Mathf.CeilToInt(yMax / cellSize);

            int verticalLines = iMax - iMin + 1;
            int horizontalLines = jMax - jMin + 1;
            int need = verticalLines + horizontalLines;

            EnsurePool(need);

            Color c = cfg.spatialGridLineColor;
            float w = Mathf.Max(0.04f, cellSize * 0.06f);
            int sortOrder = cfg.spatialGridSortingOrder;

            for (int i = 0; i < need; i++)
            {
                LineRenderer lr = _linePool[i];
                lr.enabled = true;
                lr.sharedMaterial = _lineMaterial;
                lr.startWidth = w;
                lr.endWidth = w;
                lr.startColor = c;
                lr.endColor = c;
                lr.sortingOrder = sortOrder;
            }

            int lineIndex = 0;
            float z = 0.04f;

            for (int i = iMin; i <= iMax; i++)
            {
                float x = i * cellSize;
                LineRenderer lr = _linePool[lineIndex++];
                lr.SetPosition(0, new Vector3(x, yMin, z));
                lr.SetPosition(1, new Vector3(x, yMax, z));
            }

            for (int j = jMin; j <= jMax; j++)
            {
                float y = j * cellSize;
                LineRenderer lr = _linePool[lineIndex++];
                lr.SetPosition(0, new Vector3(xMin, y, z));
                lr.SetPosition(1, new Vector3(xMax, y, z));
            }

            for (int k = need; k < _linePool.Count; k++)
                _linePool[k].enabled = false;
        }

        void EnsurePool(int count)
        {
            while (_linePool.Count < count)
            {
                var go = new GameObject("SpatialGridLine");
                go.transform.SetParent(transform, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.numCornerVertices = 0;
                lr.numCapVertices = 0;
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.sortingLayerName = "Default";
                lr.sharedMaterial = _lineMaterial;
                _linePool.Add(lr);
            }
        }

        void DisableAllLines()
        {
            for (int i = 0; i < _linePool.Count; i++)
                _linePool[i].enabled = false;
        }

        void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_lineMaterial);
                else
                    DestroyImmediate(_lineMaterial);
            }
        }
    }
}
