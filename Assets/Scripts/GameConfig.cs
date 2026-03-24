using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Swarm/Game Config", order = 0)]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Spawn")]
        [Min(1)] public int organismCount = 1000;
        public Vector2 spawnCircleCenter = Vector2.zero;
        [Min(0.01f)] public float spawnCircleRadius = 5f;

        [Header("Organism motion")]
        [Tooltip("Each organism picks a cruise speed in [min, max] at spawn (inclusive).")]
        [Min(0.01f)] public float organismSpeedMin = 1f;

        [Min(0.01f)] public float organismSpeedMax = 4f;

        [Header("Vicsek")]
        [Min(0.01f)] public float neighborRadius = 1.5f;
        [Tooltip("Standard deviation of Vicsek heading noise in degrees (Gaussian, mean 0).")]
        [Min(0f)] public float angularNoiseDegrees = 8f;

        [Tooltip("Max neighbors used for Vicsek alignment (keeps K nearest within radius).")]
        [Min(1)] public int vicsekMaxNeighbors = 10;

        [Tooltip("1 = every physics step; 2 = half the agents update Vicsek per step (others keep prior heading).")]
        [Min(1)] public int vicsekStagger = 1;

        [Tooltip("Grid cell size = neighborRadius / subdivisions. Higher splits dense clusters across more buckets (wider cell query range).")]
        [Min(1)] public int vicsekSpatialSubdivisions = 2;

        [Tooltip("Max indices visited per spatial bucket when gathering Vicsek candidates; 0 = scan entire bucket. Uses striding when bucket is larger (approximate).")]
        [Min(0)] public int vicsekMaxBucketSamples = 16;

        [Header("Bounding domain")]
        [Tooltip("World radius from spawnCircleCenter to the circular wall.")]
        [Min(0.01f)] public float boundingDomainRadius = 20f;

        [Tooltip("Distance inside the wall where inward steering ramps from Vicsek to full inward.")]
        [Min(0.01f)] public float boundingRepulsionBandWidth = 2f;

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
    }
}
