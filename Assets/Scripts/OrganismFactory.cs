using UnityEngine;

namespace Swarm
{
    public sealed class OrganismFactory : MonoBehaviour
    {
        const string DefaultGameConfigResource = "GameConfig";
        const string DefaultOrganismPrefabResource = "Organism";
        const string SquareOrganismPrefabResource = "SquareOrganism";
        const string CircleOrganismPrefabResource = "CircleOrganism";
        const string StarOrganismPrefabResource = "StarOrganism";

        [SerializeField] GameConfig gameConfig;
        [SerializeField] Organism organismPrefab;
        [SerializeField] SquareOrganism squareOrganismPrefab;
        [SerializeField] CircleOrganism circleOrganismPrefab;
        [SerializeField] StarOrganism starOrganismPrefab;

        void Start()
        {
            if (gameConfig == null)
                gameConfig = Resources.Load<GameConfig>(DefaultGameConfigResource);
            if (organismPrefab == null)
                organismPrefab = Resources.Load<Organism>(DefaultOrganismPrefabResource);
            if (squareOrganismPrefab == null)
                squareOrganismPrefab = Resources.Load<SquareOrganism>(SquareOrganismPrefabResource);
            if (circleOrganismPrefab == null)
                circleOrganismPrefab = Resources.Load<CircleOrganism>(CircleOrganismPrefabResource);
            if (starOrganismPrefab == null)
                starOrganismPrefab = Resources.Load<StarOrganism>(StarOrganismPrefabResource);

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

            float fSquare = Mathf.Max(0f, gameConfig.spawnFractionSquare);
            float fCircle = Mathf.Max(0f, gameConfig.spawnFractionCircle);
            if (fSquare + fCircle > 1f)
                fCircle = Mathf.Max(0f, 1f - fSquare);
            float fStar = Mathf.Max(0f, Mathf.Min(gameConfig.spawnFractionStar, 1f - fSquare - fCircle));

            for (int i = 0; i < gameConfig.organismCount; i++)
            {
                float theta = Random.Range(0f, Mathf.PI * 2f);
                float radius = gameConfig.spawnCircleRadius * Mathf.Sqrt(Random.value);
                Vector2 pos = gameConfig.spawnCircleCenter + radius * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                float heading = Random.Range(0f, Mathf.PI * 2f);
                float vmin = Mathf.Min(gameConfig.organismSpeedMin, gameConfig.organismSpeedMax);
                float vmax = Mathf.Max(gameConfig.organismSpeedMin, gameConfig.organismSpeedMax);
                float cruiseSpeed = Random.Range(vmin, vmax);

                Organism prefab = organismPrefab;
                float u = Random.value;
                if (u < fSquare)
                {
                    if (squareOrganismPrefab != null)
                        prefab = squareOrganismPrefab;
                }
                else if (u < fSquare + fCircle)
                {
                    if (circleOrganismPrefab != null)
                        prefab = circleOrganismPrefab;
                }
                else if (u < fSquare + fCircle + fStar)
                {
                    if (starOrganismPrefab != null)
                        prefab = starOrganismPrefab;
                }

                Organism instance = Instantiate(prefab);
                instance.Initialize(gameConfig, pos, heading, cruiseSpeed);

                if (Random.value < Mathf.Clamp01(gameConfig.spawnFractionPredator))
                    instance.gameObject.AddComponent<Predator>();
                else
                    instance.gameObject.AddComponent<Prey>();
            }

            SpawnPlayerOrganism(gameConfig, organismPrefab);
        }

        static void SpawnPlayerOrganism(GameConfig gameConfig, Organism organismPrefab)
        {
            if (gameConfig == null || organismPrefab == null)
                return;

            float theta = Random.Range(0f, Mathf.PI * 2f);
            float radius = gameConfig.spawnCircleRadius * Mathf.Sqrt(Random.value);
            Vector2 pos = gameConfig.spawnCircleCenter + radius * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            float heading = Random.Range(0f, Mathf.PI * 2f);
            float vmin = Mathf.Min(gameConfig.organismSpeedMin, gameConfig.organismSpeedMax);
            float vmax = Mathf.Max(gameConfig.organismSpeedMin, gameConfig.organismSpeedMax);
            float cruiseSpeed = Random.Range(vmin, vmax);

            Organism player = Object.Instantiate(organismPrefab);
            player.Initialize(gameConfig, pos, heading, cruiseSpeed);
            player.transform.localScale *= 5f;
            player.gameObject.AddComponent<PlayerControlled>();
        }
    }
}
