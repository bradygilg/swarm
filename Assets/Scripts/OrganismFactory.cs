using UnityEngine;

namespace Swarm
{
    public sealed class OrganismFactory : MonoBehaviour
    {
        const string DefaultGameConfigResource = "GameConfig";
        const string DefaultOrganismPrefabResource = "Organism";

        [SerializeField] GameConfig gameConfig;
        [SerializeField] Organism organismPrefab;

        void Start()
        {
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultGameConfigResource);
            if (organismPrefab == null)
                organismPrefab = Resources.Load<Organism>(DefaultOrganismPrefabResource);

            if (gameConfig == null || organismPrefab == null)
            {
                Debug.LogError(
                    "OrganismFactory needs GameConfig and Organism prefab. Assign them on the component, or place Assets/Resources/GameConfig.asset and Organism.prefab (names without extension).",
                    this);
                return;
            }

            if (SwarmSimulation.Instance == null)
            {
                Debug.LogError(
                    "OrganismFactory requires an active SwarmSimulation in the scene (see Game scene).",
                    this);
                return;
            }

            for (int i = 0; i < gameConfig.organismCount; i++)
            {
                float theta = Random.Range(0f, Mathf.PI * 2f);
                float radius = gameConfig.spawnCircleRadius * Mathf.Sqrt(Random.value);
                Vector2 pos = gameConfig.spawnCircleCenter + radius * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                float heading = Random.Range(0f, Mathf.PI * 2f);
                float vmin = Mathf.Min(gameConfig.organismSpeedMin, gameConfig.organismSpeedMax);
                float vmax = Mathf.Max(gameConfig.organismSpeedMin, gameConfig.organismSpeedMax);
                float cruiseSpeed = Random.Range(vmin, vmax);

                Organism instance = Instantiate(organismPrefab);
                instance.Initialize(gameConfig, pos, heading, cruiseSpeed);
            }
        }
    }
}
