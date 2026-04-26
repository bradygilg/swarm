using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Swarm
{
    /// <summary>Moves the attached <see cref="Organism"/> toward the mouse in world space (2D orthographic).</summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class PlayerControlled : OrganismType
    {
        const string DefaultConfigResource = "GameConfig";

        Organism _organism;
        Camera _camera;

        void Awake()
        {
            _organism = GetComponent<Organism>();
            if (_organism != null)
            {
                _organism.SetPlayerDriven(true);
                _organism.SetEatable(false);
            }

            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }

        void OnDestroy()
        {
            if (_organism != null)
                _organism.SetPlayerDriven(false);
        }

        void FixedUpdate()
        {
            if (_organism == null || !_organism.IsActiveInSimulation || _camera == null || !_camera.orthographic)
                return;

            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : null;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>(DefaultConfigResource);
            if (cfg == null)
                return;

            Vector2 screen = GetMouseScreenPosition();
            Vector2 target = ScreenToWorldOnPlayPlane(screen);
            Vector2 toMouse = target - _organism.SimulationPosition;
            float dist = toMouse.magnitude;

            float vmin = Mathf.Min(cfg.playerSpeedMin, cfg.playerSpeedMax);
            float vmax = Mathf.Max(cfg.playerSpeedMin, cfg.playerSpeedMax);
            float maxDist = Mathf.Max(1e-4f, cfg.boundingDomainRadius);

            if (toMouse.sqrMagnitude > 1e-8f)
            {
                float t = Mathf.Clamp01(dist / maxDist);
                float speed = Mathf.Lerp(vmin, vmax, t);
                _organism.SetCruiseSpeed(speed);
                _organism.SetVelocity(toMouse.normalized * speed);
                _organism.ApplyVisualColorForSpeedRange(cfg, speed, vmin, vmax);
            }
            else
            {
                _organism.SetVelocity(Vector2.zero);
                _organism.ApplyVisualColorForSpeedRange(cfg, 0f, vmin, vmax);
            }
        }

        Vector2 GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 p = Mouse.current.position.ReadValue();
                return p;
            }
#endif
            return Input.mousePosition;
        }

        Vector2 ScreenToWorldOnPlayPlane(Vector2 screenPixels)
        {
            float z = Mathf.Abs(_camera.transform.position.z);
            if (z < 1e-4f)
                z = 10f;
            Vector3 w = _camera.ScreenToWorldPoint(new Vector3(screenPixels.x, screenPixels.y, z));
            return new Vector2(w.x, w.y);
        }
    }
}
