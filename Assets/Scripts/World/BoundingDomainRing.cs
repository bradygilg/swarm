using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swarm
{
    /// <summary>
    /// Draws an orange world-space ring at <see cref="GameConfig.boundingDomainRadius"/> around <see cref="GameConfig.spawnCircleCenter"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoundingDomainRing : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";
        const int CircleSegments = 96;
        const float LineWidthWorld = 0.15f;
        const float ZOffset = 0.03f;

        static readonly Color RingColor = new Color(1f, 0.45f, 0.05f, 1f);

        [SerializeField] GameConfig gameConfig;
        LineRenderer _line;
        Vector2 _cachedCenter = new Vector2(float.NaN, float.NaN);
        float _cachedRadius = float.NaN;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            if (_line == null)
                _line = gameObject.AddComponent<LineRenderer>();

            ConfigureLineRendererDefaults();
        }

        void OnEnable()
        {
            RebuildFromConfig();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // AddComponent is not allowed during OnValidate; defer to next editor tick.
            EditorApplication.delayCall += OnValidateDeferred;
        }

        void OnValidateDeferred()
        {
            if (this == null)
                return;

            _line = GetComponent<LineRenderer>();
            if (_line == null)
                _line = gameObject.AddComponent<LineRenderer>();

            ConfigureLineRendererDefaults();
            if (gameConfig != null)
                RebuildFromConfig();
        }
#endif

        void Update()
        {
            if (gameConfig == null)
                return;

            Vector2 c = gameConfig.spawnCircleCenter;
            float r = gameConfig.boundingDomainRadius;
            if (c == _cachedCenter && Mathf.Approximately(r, _cachedRadius))
                return;

            RebuildFromConfig();
        }

        void ConfigureLineRendererDefaults()
        {
            _line.loop = true;
            _line.useWorldSpace = false;
            _line.positionCount = CircleSegments;
            _line.numCapVertices = 4;
            _line.numCornerVertices = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.sortingOrder = -400;

            _line.startWidth = LineWidthWorld;
            _line.endWidth = LineWidthWorld;
            _line.startColor = RingColor;
            _line.endColor = RingColor;

            if (_line.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                    _line.material = new Material(shader);
            }
        }

        void RebuildFromConfig()
        {
            if (_line == null)
                return;

            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultConfigResource);
            if (gameConfig == null)
                return;

            Vector2 c = gameConfig.spawnCircleCenter;
            float r = Mathf.Max(1e-4f, gameConfig.boundingDomainRadius);
            transform.position = new Vector3(c.x, c.y, ZOffset);

            for (int i = 0; i < CircleSegments; i++)
            {
                float t = (i / (float)CircleSegments) * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t) * r, 0f));
            }

            _cachedCenter = c;
            _cachedRadius = r;
        }
    }
}
