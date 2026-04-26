using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    public enum TrailingWallQualityPreset
    {
        Low,
        Medium,
        High,
        Custom
    }

    public enum PlayerProjectileFireInput
    {
        LeftMouse = 0,
        RightMouse = 1,
        MiddleMouse = 2
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Swarm/Game Config", order = 0)]
    public sealed class GameConfig : ScriptableObject
    {
        const string DefaultOrganismTypeConfigResource = "OrganismTypeConfig";
        [SerializeField] OrganismTypeConfig organismTypeConfig;

        public OrganismTypeConfig OrganismTypeConfig
        {
            get
            {
                if (organismTypeConfig == null)
                    organismTypeConfig = Resources.Load<OrganismTypeConfig>(DefaultOrganismTypeConfigResource);
                if (organismTypeConfig == null)
                    organismTypeConfig = ScriptableObject.CreateInstance<OrganismTypeConfig>();
                return organismTypeConfig;
            }
        }

        [Header("Spawn")]
        [Min(1)] public int organismCount = 20;
        public Vector2 spawnCircleCenter = Vector2.zero;
        [Min(0.01f)] public float spawnCircleRadius = 5f;

        [Header("Organism motion")]
        [Tooltip("Each organism picks a cruise speed in [min, max] at spawn (inclusive).")]
        [Min(0.01f)] public float organismSpeedMin = 0.5f;

        [Min(0.01f)] public float organismSpeedMax = 2f;

        [Tooltip("Player-controlled speed floor used by PlayerControlled (separate from NPC organism speeds).")]
        [Min(0.01f)] public float playerSpeedMin = 0.5f;

        [Tooltip("Player-controlled speed cap used by PlayerControlled (separate from NPC organism speeds).")]
        [Min(0.01f)] public float playerSpeedMax = 2f;

        [Tooltip("Fraction of spawns that use SquareOrganism (anti-alignment).")]
        [Range(0f, 1f)] public float spawnFractionSquare = 0f;

        [Tooltip("Fraction of spawns that use CircleOrganism (density-based speed).")]
        [Range(0f, 1f)] public float spawnFractionCircle = 0.05f;

        [Tooltip("Fraction of spawns that use StarOrganism (others flee its position in Vicsek mean).")]
        [Range(0f, 1f)] public float spawnFractionStar = 0f;

        [Tooltip("Fraction of spawns that receive a Predator component; remainder receive Prey.")]
        [Range(0f, 1f)] public float spawnFractionPredator = 0.01f;

        [Tooltip("Fraction of NPC spawns that receive the TrailingWall component.")]
        [Range(0f, 1f)] public float spawnFractionNpcTrailingWall = 0.05f;

        [Header("Vicsek")]
        [Min(0.01f)] public float neighborRadius = 1.5f;
        [Tooltip("Standard deviation of Vicsek heading noise in degrees (Gaussian, mean 0).")]
        [Min(0f)] public float angularNoiseDegrees = 8f;

        [Tooltip("Max neighbors used for Vicsek alignment (keeps K nearest within radius).")]
        [Min(1)] public int vicsekMaxNeighbors = 10;

        [Tooltip("1 = every physics step; 2 = half the agents update Vicsek per step (others keep prior heading).")]
        [Min(1)] public int vicsekStagger = 3;

        [Tooltip("Grid cell size = neighborRadius / subdivisions. Higher splits dense clusters across more buckets (wider cell query range).")]
        [Min(1)] public int vicsekSpatialSubdivisions = 2;

        [Tooltip("Max indices visited per spatial bucket when gathering Vicsek candidates; 0 = scan entire bucket. Uses striding when bucket is larger (approximate).")]
        [Min(0)] public int vicsekMaxBucketSamples = 16;

        [Header("Flower / nectar")]
        [Tooltip("Starting nectar per spawned flower.")]
        [Min(0)] public int flowerInitialNectar = 100;

        [Tooltip("Seconds between random flower spawns inside the bounding disk.")]
        [Min(0.1f)] public float flowerSpawnIntervalSeconds = 10f;

        [Min(8)] public int flowerSpriteResolution = 32;

        [Tooltip("World scale when nectar is depleted.")]
        [Min(0.01f)] public float flowerMinVisualScale = 0.1f;

        [Tooltip("World scale when nectar is full (initial).")]
        [Min(0.01f)] public float flowerMaxVisualScale = 0.35f;

        [Tooltip("Gather radius for prey feeding on flowers.")]
        [Min(0.01f)] public float preyFlowerGatherRadius = 2f;

        [Tooltip("Nectar units removed per second while a prey is in gather range of a flower with nectar.")]
        [Min(0f)] public float preyNectarConsumptionPerSecond = 1f;

        [Tooltip("Nectar units consumed before prey replicates.")]
        [Min(1)] public int preyReplicationNectarThreshold = 10;

        [Tooltip("Additive visual scale per nectar unit consumed, as percent of base prey scale.")]
        [Min(0f)] public float preyNectarScaleAdditivePercent = 10f;

        [Header("Bounding domain")]
        [Tooltip("World radius from spawnCircleCenter to the circular wall.")]
        [Min(0.01f)] public float boundingDomainRadius = 20f;

        [Tooltip("Distance inside the wall where inward steering ramps from Vicsek to full inward.")]
        [Min(0.01f)] public float boundingRepulsionBandWidth = 2f;

        [Header("Trailing wall")]
        [Tooltip("Global performance/quality preset for TrailingWall behavior. Custom uses the explicit values below.")]
        public TrailingWallQualityPreset trailingWallQualityPreset = TrailingWallQualityPreset.Medium;

        [Header("Player projectile")]
        [Tooltip("Mouse button used to fire player projectiles.")]
        public PlayerProjectileFireInput playerProjectileFireInput = PlayerProjectileFireInput.LeftMouse;

        [Header("Initializations")]
        [Tooltip("Initial capacity for each spatial-grid bucket (per cell) when finding Vicsek neighbors; avoids extra List growth.")]
        [Min(1)] public int spatialGridBucketCapacity = 16;

        [Header("Visual")]
        [Min(0.05f)] public float triangleScale = 0.35f;
        [Min(8)] public int triangleSpriteResolution = 32;

        [Tooltip("Triangle tint at organismSpeedMin (lerps toward max-speed color).")]
        public Color organismColorAtMinSpeed = new Color(0.25f, 0.55f, 1f, 1f);

        [Tooltip("Triangle tint at organismSpeedMax.")]
        public Color organismColorAtMaxSpeed = new Color(1f, 0.4f, 0.15f, 1f);

        [Tooltip("When true, draws world lines for the CPU Vicsek spatial hash: cell size = neighborRadius / vicsekSpatialSubdivisions, aligned to world axes (same as SwarmSimulation._spatialGrid).")]
        public bool showSpatialGridInGame;

        [Tooltip("Color for spatial grid lines (alpha controls visibility).")]
        public Color spatialGridLineColor = new Color(0.45f, 0.9f, 1f, 0.4f);

        [Tooltip("Draw order relative to other 2D renderers (above checkerboard, below organisms).")]
        public int spatialGridSortingOrder = 400;

        [Header("Physics (optional tuning)")]
        [Min(0.01f)] public float organismMass = 1f;

        [Header("Camera (game scene)")]
        [Tooltip("WASD / arrow pan speed; scaled slightly with zoom.")]
        [Min(0.01f)] public float cameraKeyboardPanSpeed = 10f;

        [Tooltip("Mouse buttons that drag-pan the camera (0 = left, 1 = right, 2 = middle). Empty = no mouse pan.")]
        public List<int> cameraMousePanButtons = new List<int> { 1, 2 };

        [Min(0.01f)] public float cameraMousePanSensitivity = 1f;

        [Tooltip("Orthographic size change per wheel delta (matches legacy Input.mouseScrollDelta scale).")]
        [Min(0.01f)] public float cameraScrollZoomSpeed = 0.65f;

        [Tooltip("Orthographic size change per second while holding zoom keys.")]
        [Min(0.01f)] public float cameraKeyboardZoomSpeed = 4f;

        [Min(0.1f)] public float cameraMinOrthographicSize = 1.5f;
        [Min(0.1f)] public float cameraMaxOrthographicSize = 48f;

        [Header("Background (game scene)")]
        [Min(0.05f)] public float checkerCellWorldSize = 1f;

        public Color checkerColorA = new Color(0.14f, 0.15f, 0.18f, 1f);
        public Color checkerColorB = new Color(0.10f, 0.11f, 0.14f, 1f);

        [Tooltip("Half-width and half-height of the tiled area in world units.")]
        [Min(16f)] public float checkerBackgroundHalfExtent = 320f;

        [Tooltip("Draw behind organisms.")]
        public int checkerboardSortingOrder = -500;

        [Tooltip("Slight Z offset for 2D ordering.")]
        public float checkerboardZOffset = 0.02f;

        void OnValidate()
        {
            if (spawnFractionSquare + spawnFractionCircle > 1f)
                spawnFractionCircle = Mathf.Max(0f, 1f - spawnFractionSquare);
            float maxStar = Mathf.Max(0f, 1f - spawnFractionSquare - spawnFractionCircle);
            if (spawnFractionStar > maxStar)
                spawnFractionStar = maxStar;
        }
    }
}
