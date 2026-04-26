using UnityEngine;

namespace Swarm
{
    /// <summary>
    /// Full-screen checkerboard using one quad and repeating UVs (avoids SpriteRenderer tiled mesh explosion).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CheckerboardBackground : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";
        const int CellsPerTextureSide = 4;
        const int PixelsPerCheckerCell = 16;

        [SerializeField] GameConfig gameConfig;
        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultConfigResource);
            if (gameConfig == null)
                return;

            Rebuild();
        }

        void Rebuild()
        {
            int texSize = CellsPerTextureSide * PixelsPerCheckerCell;
            float cellWorld = Mathf.Max(0.05f, gameConfig.checkerCellWorldSize);

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave,
                name = "CheckerboardRuntime"
            };

            Color32 a = gameConfig.checkerColorA;
            Color32 b = gameConfig.checkerColorB;
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    int cx = x / PixelsPerCheckerCell;
                    int cy = y / PixelsPerCheckerCell;
                    bool useA = ((cx + cy) & 1) == 0;
                    tex.SetPixel(x, y, useA ? a : b);
                }
            }

            tex.Apply(false, true);

            float extent = Mathf.Max(32f, gameConfig.checkerBackgroundHalfExtent);
            float w = extent * 2f;
            float h = extent * 2f;

            float cellsU = w / cellWorld;
            float cellsV = h / cellWorld;
            float uMax = cellsU / CellsPerTextureSide;
            float vMax = cellsV / CellsPerTextureSide;

            var mesh = new Mesh
            {
                name = "CheckerboardQuad",
                hideFlags = HideFlags.DontSave
            };

            mesh.vertices = new[]
            {
                new Vector3(-w * 0.5f, -h * 0.5f, 0f),
                new Vector3(w * 0.5f, -h * 0.5f, 0f),
                new Vector3(w * 0.5f, h * 0.5f, 0f),
                new Vector3(-w * 0.5f, h * 0.5f, 0f)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(uMax, 0f),
                new Vector2(uMax, vMax),
                new Vector2(0f, vMax)
            };

            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            _meshFilter.sharedMesh = mesh;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.DontSave,
                mainTexture = tex
            };

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);

            _meshRenderer.sharedMaterial = mat;
            _meshRenderer.sortingOrder = gameConfig.checkerboardSortingOrder;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            transform.position = new Vector3(
                gameConfig.spawnCircleCenter.x,
                gameConfig.spawnCircleCenter.y,
                gameConfig.checkerboardZOffset);
        }
    }
}
