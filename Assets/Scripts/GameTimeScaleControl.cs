using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Swarm
{
    /// <summary>Game scene UI: adjust <see cref="Time.timeScale"/> with slower / faster buttons.</summary>
    [DisallowMultipleComponent]
    public sealed class GameTimeScaleControl : MonoBehaviour
    {
        const float MinScale = 0.25f;
        const float MaxScale = 8f;

        Text _label;
        float _scale = 1f;

        void Awake()
        {
            EnsureEventSystem();
            _scale = Mathf.Clamp(Time.timeScale, MinScale, MaxScale);
            BuildUi();
            Apply();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("TimeScaleCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var row = new GameObject("TimeScaleRow");
            row.transform.SetParent(canvasGo.transform, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(1, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(1, 1);
            rowRect.anchoredPosition = new Vector2(-16f, -16f);
            rowRect.sizeDelta = new Vector2(420f, 44f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            CreateButton(row.transform, "Slower", font, Slower);
            _label = CreateLabel(row.transform, font);
            CreateButton(row.transform, "Faster", font, Faster);
        }

        static void CreateButton(Transform parent, string title, Font font, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(title + "Button");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.16f, 0.2f, 0.92f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.3f, 0.38f, 1f);
            colors.pressedColor = new Color(0.12f, 0.12f, 0.15f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 40f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 120f;
            le.preferredHeight = 40f;
            le.minWidth = 120f;
            le.minHeight = 40f;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = title;

            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        static Text CreateLabel(Transform parent, Font font)
        {
            var go = new GameObject("TimeLabel");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 140f;
            le.flexibleWidth = 0f;

            var rect = text.rectTransform;
            rect.sizeDelta = new Vector2(140f, 40f);
            return text;
        }

        void Slower()
        {
            _scale = Mathf.Max(MinScale, _scale * 0.5f);
            Apply();
        }

        void Faster()
        {
            _scale = Mathf.Min(MaxScale, _scale * 2f);
            Apply();
        }

        void Apply()
        {
            Time.timeScale = _scale;
            if (_label != null)
                _label.text = "Time: " + _scale.ToString("0.###") + "x";
        }
    }
}
