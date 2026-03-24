using UnityEngine;

namespace Swarm
{
    /// <summary>
    /// Central mapping of logical actions to keys. Menu uses UI buttons; these are for in-game / global shortcuts.
    /// </summary>
    public static class KeyboardConfig
    {
        public static readonly KeyCode Pause = KeyCode.Escape;
        public static readonly KeyCode ReturnToMainMenu = KeyCode.Escape;

        public static readonly KeyCode PanNorth = KeyCode.W;
        public static readonly KeyCode PanSouth = KeyCode.S;
        public static readonly KeyCode PanWest = KeyCode.A;
        public static readonly KeyCode PanEast = KeyCode.D;
        public static readonly KeyCode PanNorthAlt = KeyCode.UpArrow;
        public static readonly KeyCode PanSouthAlt = KeyCode.DownArrow;
        public static readonly KeyCode PanWestAlt = KeyCode.LeftArrow;
        public static readonly KeyCode PanEastAlt = KeyCode.RightArrow;

        public static readonly KeyCode CameraZoomIn = KeyCode.Equals;
        public static readonly KeyCode CameraZoomOut = KeyCode.Minus;
        public static readonly KeyCode CameraZoomInAlt = KeyCode.KeypadPlus;
        public static readonly KeyCode CameraZoomOutAlt = KeyCode.KeypadMinus;
    }
}
