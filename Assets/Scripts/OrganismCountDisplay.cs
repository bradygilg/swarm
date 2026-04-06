using UnityEngine;
using UnityEngine.UI;

namespace Swarm
{
    /// <summary>Screen overlay for predator / prey / total counts from <see cref="SwarmSimulation.GetLiveOrganismCounts"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class OrganismCountDisplay : MonoBehaviour
    {
        Text _text;
        SwarmSimulation _sim;

        void Awake()
        {
            BuildUi();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("OrganismCountCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("OrganismCountText");
            textGo.transform.SetParent(canvasGo.transform, false);
            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 22;
            _text.color = Color.white;
            _text.alignment = TextAnchor.UpperLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = _text.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(420f, 88f);

            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        void LateUpdate()
        {
            if (_sim == null)
                _sim = SwarmSimulation.Instance;
            if (_text == null)
                return;

            if (_sim == null)
            {
                _text.text = "Predators: 0\nPrey: 0\nTotal: 0";
                return;
            }

            _sim.GetLiveOrganismCounts(out int total, out int predators, out int prey);
            _text.text = "Predators: " + predators + "\nPrey: " + prey + "\nTotal: " + total;
        }
    }
}
