using UnityEngine;

namespace Swarm
{
    [CreateAssetMenu(fileName = "OrganismTypeConfig", menuName = "Swarm/Organism Type Config", order = 1)]
    public sealed class OrganismTypeConfig : ScriptableObject
    {
        [Header("Predator / Prey")]
        [Tooltip("Seconds between eat attempts (X).")]
        [Min(1e-4f)] public float predatorEatIntervalSeconds = 5f;

        [Tooltip("World radius to find prey (Y).")]
        [Min(1e-4f)] public float predatorHuntRadius = 3f;

        [Tooltip("Scale multiplier increase per eat as percent (Z), e.g. 10 = +10% size.")]
        public float predatorGrowthPercentOnEat = 10f;

        [Tooltip("Seconds to fade prey sprite alpha to zero before destroy.")]
        [Min(0.01f)] public float preyFadeDurationSeconds = 1f;

        [Header("Circle organism")]
        [Tooltip("Cruise speed when few neighbors are in the Vicsek K-set (fill -> 0).")]
        [Min(0.01f)] public float circleCruiseSpeedLowDensity = 1f;

        [Tooltip("Cruise speed when the K-set is full (fill -> vicsekMaxNeighbors).")]
        [Min(0.01f)] public float circleCruiseSpeedHighDensity = 3.5f;

        [Header("Vicsek influence")]
        [Tooltip("Vicsek weight when receiving agent has Prey and neighbor is Flower.")]
        [Min(0.01f)] public float flowerVicsekWeight = 5f;

        [Header("Trailing wall defaults")]
        [Min(0.05f)] public float trailingWallFadeDurationSeconds = 2f;
        [Min(0.001f)] public float trailingWallLineWidth = 0.08f;
        [Min(0.005f)] public float trailingWallSampleDistance = 0.08f;
        [Min(0f)] public float trailingWallRepulsionRadius = 0.5f;
        [Min(0f)] public float trailingWallRepulsionStrength = 0.85f;
        public bool trailingWallUseOrganismColor = true;
        public Color trailingWallColor = Color.white;
        [Min(0)] public int trailingWallSortingOrder = -50;
        [Min(0)] public int trailingWallCapVertices = 4;
        [Min(0)] public int trailingWallCornerVertices = 2;

        [Header("Trailing wall quality presets")]
        [Min(0.01f)] public float trailingWallLowVisualUpdateIntervalSeconds = 0.12f;
        [Min(0.01f)] public float trailingWallLowRepulsionUpdateIntervalSeconds = 0.2f;
        [Min(8)] public int trailingWallLowMaxTrailPoints = 48;
        [Min(1)] public int trailingWallLowMaxRepulsionSegments = 12;

        [Min(0.01f)] public float trailingWallMediumVisualUpdateIntervalSeconds = 0.05f;
        [Min(0.01f)] public float trailingWallMediumRepulsionUpdateIntervalSeconds = 0.1f;
        [Min(8)] public int trailingWallMediumMaxTrailPoints = 96;
        [Min(1)] public int trailingWallMediumMaxRepulsionSegments = 24;

        [Min(0.01f)] public float trailingWallHighVisualUpdateIntervalSeconds = 0.03f;
        [Min(0.01f)] public float trailingWallHighRepulsionUpdateIntervalSeconds = 0.06f;
        [Min(8)] public int trailingWallHighMaxTrailPoints = 160;
        [Min(1)] public int trailingWallHighMaxRepulsionSegments = 48;

        [Min(0.01f)] public float trailingWallCustomVisualUpdateIntervalSeconds = 0.05f;
        [Min(0.01f)] public float trailingWallCustomRepulsionUpdateIntervalSeconds = 0.1f;
        [Min(8)] public int trailingWallCustomMaxTrailPoints = 96;
        [Min(1)] public int trailingWallCustomMaxRepulsionSegments = 24;

        [Header("Player projectile")]
        [Tooltip("Minimum seconds between projectile shots.")]
        [Min(0f)] public float playerProjectileFireCooldownSeconds = 0.15f;

        [Tooltip("Projectile straight-line travel speed in world units/second.")]
        [Min(0.01f)] public float playerProjectileSpeed = 10f;

        [Tooltip("Projectile speed scale when firing at very close cursor distance.")]
        [Min(0f)] public float playerProjectileMinSpeedScale = 0.5f;

        [Tooltip("Projectile speed scale when firing at far cursor distance.")]
        [Min(0f)] public float playerProjectileMaxSpeedScale = 1.75f;

        [Tooltip("Projectile lifetime before auto-destroy.")]
        [Min(0.01f)] public float playerProjectileLifetimeSeconds = 3f;

        [Tooltip("Maximum distance from spawn position before auto-destroy.")]
        [Min(0.01f)] public float playerProjectileMaxDistance = 30f;

        [Tooltip("Vicsek influence weight applied to predators/prey for projectile attraction.")]
        [Min(0f)] public float playerProjectileVicsekWeight = 12f;

        [Tooltip("Additional scalar strength for attraction direction toward the projectile.")]
        [Min(0f)] public float playerProjectileAttractionStrength = 3f;

        [Tooltip("Visual size for projectile sprite in world units.")]
        [Min(0.01f)] public float playerProjectileVisualScale = 0.22f;

        [Tooltip("Projectile tint.")]
        public Color playerProjectileColor = new Color(0.9f, 0.95f, 1f, 1f);
    }
}
