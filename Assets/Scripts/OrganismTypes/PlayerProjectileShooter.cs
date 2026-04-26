using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Swarm
{
    /// <summary>
    /// Player ability that spawns temporary attractive projectiles toward the mouse cursor.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Organism))]
    public sealed class PlayerProjectileShooter : OrganismType
    {
        const string DefaultConfigResource = "GameConfig";

        Organism _organism;
        Camera _camera;
        float _nextFireAt;

        void Awake()
        {
            _organism = GetComponent<Organism>();
            _camera = Camera.main;
            if (_camera == null)
                _camera = FindFirstObjectByType<Camera>();
        }

        void Update()
        {
            if (_organism == null || !_organism.IsActiveInSimulation || _camera == null || !_camera.orthographic)
                return;

            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : null;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>(DefaultConfigResource);
            if (cfg == null)
                return;
            OrganismTypeConfig typeCfg = cfg.OrganismTypeConfig;

            if (Time.time < _nextFireAt)
                return;
            if (!IsFireHeld(cfg.playerProjectileFireInput))
                return;

            Vector2 origin = _organism.SimulationPosition;
            Vector2 target = ScreenToWorldOnPlayPlane(GetMouseScreenPosition());
            Vector2 dir = target - origin;
            float fireDistance = dir.magnitude;
            if (dir.sqrMagnitude < 1e-8f)
                return;

            SpawnProjectile(cfg, typeCfg, origin, dir.normalized, fireDistance);
            _nextFireAt = Time.time + Mathf.Max(0f, typeCfg.playerProjectileFireCooldownSeconds);
        }

        void SpawnProjectile(GameConfig cfg, OrganismTypeConfig typeCfg, Vector2 origin, Vector2 dir, float fireDistance)
        {
            float distanceT = Mathf.Clamp01(fireDistance / Mathf.Max(1e-4f, cfg.boundingDomainRadius));
            float minScale = Mathf.Min(typeCfg.playerProjectileMinSpeedScale, typeCfg.playerProjectileMaxSpeedScale);
            float maxScale = Mathf.Max(typeCfg.playerProjectileMinSpeedScale, typeCfg.playerProjectileMaxSpeedScale);
            float launchSpeed = typeCfg.playerProjectileSpeed * Mathf.Lerp(minScale, maxScale, distanceT);

            var go = new GameObject("AttractorProjectile");
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            go.AddComponent<SpriteRenderer>();
            AttractorProjectile projectile = go.AddComponent<AttractorProjectile>();
            projectile.Configure(
                dir,
                launchSpeed,
                typeCfg.playerProjectileLifetimeSeconds,
                typeCfg.playerProjectileMaxDistance,
                typeCfg.playerProjectileVicsekWeight,
                typeCfg.playerProjectileAttractionStrength,
                typeCfg.playerProjectileVisualScale,
                typeCfg.playerProjectileColor);
            projectile.Initialize(cfg, origin, dir * launchSpeed, launchSpeed);
        }

        static bool IsFireHeld(PlayerProjectileFireInput input)
        {
            int buttonIndex = (int)input;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return input switch
                {
                    PlayerProjectileFireInput.LeftMouse => Mouse.current.leftButton.isPressed,
                    PlayerProjectileFireInput.RightMouse => Mouse.current.rightButton.isPressed,
                    PlayerProjectileFireInput.MiddleMouse => Mouse.current.middleButton.isPressed,
                    _ => Mouse.current.leftButton.isPressed
                };
            }
#endif
            return Input.GetMouseButton(buttonIndex);
        }

        static Vector2 GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
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
