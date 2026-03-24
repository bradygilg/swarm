using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Swarm
{
    /// <summary>
    /// Orthographic 2D camera: WASD / arrows to pan, mouse wheel and +/- to zoom, middle-mouse drag to pan.
    /// Uses the **Input System** when devices are available (Unity 6 default); otherwise legacy <see cref="Input"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GameCameraController : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";

        [SerializeField] GameConfig gameConfig;
        Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultConfigResource);
        }

        void Update()
        {
            if (gameConfig == null || !_camera.orthographic)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Mouse.current != null)
            {
                UpdateFromInputSystem();
                return;
            }
#endif
            UpdateFromLegacyInput();
        }

#if ENABLE_INPUT_SYSTEM
        void UpdateFromInputSystem()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            float dt = Time.deltaTime;
            float ortho = _camera.orthographicSize;
            float worldPerPixel = (2f * ortho) / Mathf.Max(Screen.height, 1);

            Vector2 panKeys = Vector2.zero;
            if (kb[Key.W].isPressed || kb[Key.UpArrow].isPressed)
                panKeys.y += 1f;
            if (kb[Key.S].isPressed || kb[Key.DownArrow].isPressed)
                panKeys.y -= 1f;
            if (kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed)
                panKeys.x -= 1f;
            if (kb[Key.D].isPressed || kb[Key.RightArrow].isPressed)
                panKeys.x += 1f;

            if (panKeys.sqrMagnitude > 1e-6f)
            {
                panKeys.Normalize();
                float speed = gameConfig.cameraKeyboardPanSpeed * ortho / 6f;
                transform.position += (Vector3)(panKeys * (speed * dt));
            }

            Vector2 mouseDelta = mouse.delta.ReadValue();
            if (IsAnyPanButtonHeld(mouse, gameConfig.cameraMousePanButtons))
            {
                float sens = gameConfig.cameraMousePanSensitivity;
                transform.position -= new Vector3(
                    mouseDelta.x * worldPerPixel * sens,
                    mouseDelta.y * worldPerPixel * sens,
                    0f);
            }

            // Same scaling as legacy Input.mouseScrollDelta: wheel delta is already in small
            // “notch” units here — do not divide by 120 (that assumed raw WM_MOUSEWHEEL).
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 1e-4f)
                ApplyOrthographicDelta(-scroll * gameConfig.cameraScrollZoomSpeed);

            float zoomAxis = 0f;
            if (kb[Key.Equals].isPressed || kb[Key.NumpadPlus].isPressed)
                zoomAxis += 1f;
            if (kb[Key.Minus].isPressed || kb[Key.NumpadMinus].isPressed)
                zoomAxis -= 1f;
            if (Mathf.Abs(zoomAxis) > 1e-4f)
                ApplyOrthographicDelta(-zoomAxis * gameConfig.cameraKeyboardZoomSpeed * dt);
        }
#endif

        void UpdateFromLegacyInput()
        {
            float dt = Time.deltaTime;
            float ortho = _camera.orthographicSize;
            float worldPerPixel = (2f * ortho) / Mathf.Max(Screen.height, 1);

            Vector2 panKeys = Vector2.zero;
            if (Input.GetKey(KeyboardConfig.PanNorth) || Input.GetKey(KeyboardConfig.PanNorthAlt))
                panKeys.y += 1f;
            if (Input.GetKey(KeyboardConfig.PanSouth) || Input.GetKey(KeyboardConfig.PanSouthAlt))
                panKeys.y -= 1f;
            if (Input.GetKey(KeyboardConfig.PanWest) || Input.GetKey(KeyboardConfig.PanWestAlt))
                panKeys.x -= 1f;
            if (Input.GetKey(KeyboardConfig.PanEast) || Input.GetKey(KeyboardConfig.PanEastAlt))
                panKeys.x += 1f;

            if (panKeys.sqrMagnitude > 1e-6f)
            {
                panKeys.Normalize();
                float speed = gameConfig.cameraKeyboardPanSpeed * ortho / 6f;
                transform.position += (Vector3)(panKeys * (speed * dt));
            }

            if (IsAnyPanButtonHeldLegacy(gameConfig.cameraMousePanButtons))
            {
                float sens = gameConfig.cameraMousePanSensitivity;
                transform.position -= new Vector3(
                    Input.GetAxis("Mouse X") * worldPerPixel * sens,
                    Input.GetAxis("Mouse Y") * worldPerPixel * sens,
                    0f);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 1e-4f)
                ApplyOrthographicDelta(-scroll * gameConfig.cameraScrollZoomSpeed);

            float zoomAxis = 0f;
            if (Input.GetKey(KeyboardConfig.CameraZoomIn) || Input.GetKey(KeyboardConfig.CameraZoomInAlt))
                zoomAxis += 1f;
            if (Input.GetKey(KeyboardConfig.CameraZoomOut) || Input.GetKey(KeyboardConfig.CameraZoomOutAlt))
                zoomAxis -= 1f;
            if (Mathf.Abs(zoomAxis) > 1e-4f)
                ApplyOrthographicDelta(-zoomAxis * gameConfig.cameraKeyboardZoomSpeed * dt);
        }

        void ApplyOrthographicDelta(float delta)
        {
            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize + delta,
                gameConfig.cameraMinOrthographicSize,
                gameConfig.cameraMaxOrthographicSize);
        }

#if ENABLE_INPUT_SYSTEM
        static bool IsAnyPanButtonHeld(Mouse mouse, IList<int> buttonIndices)
        {
            if (mouse == null || buttonIndices == null)
                return false;

            foreach (int b in buttonIndices)
            {
                if (b == 0 && mouse.leftButton.isPressed)
                    return true;
                if (b == 1 && mouse.rightButton.isPressed)
                    return true;
                if (b == 2 && mouse.middleButton.isPressed)
                    return true;
            }

            return false;
        }
#endif

        static bool IsAnyPanButtonHeldLegacy(IList<int> buttonIndices)
        {
            if (buttonIndices == null)
                return false;

            foreach (int b in buttonIndices)
            {
                if (b >= 0 && b <= 2 && Input.GetMouseButton(b))
                    return true;
            }

            return false;
        }
    }
}
