using UnityEngine;

namespace Swarm
{
    /// <summary>Spawns organisms from Resources prefabs (same strategy as <see cref="OrganismFactory"/>), without cloning runtime Predator/Prey state.</summary>
    public static class OrganismSpawn
    {
        const string DefaultOrganismPrefabResource = "Organism";
        const string SquareOrganismPrefabResource = "SquareOrganism";
        const string CircleOrganismPrefabResource = "CircleOrganism";
        const string StarOrganismPrefabResource = "StarOrganism";

        /// <summary>Instantiates the same concrete organism type as <paramref name="source"/>, initializes it, then optionally adds Predator and/or Prey.</summary>
        public static Organism SpawnSameOrganismType(Organism source, GameConfig cfg, Vector2 positionOffset, bool addPredator, bool addPrey)
        {
            if (source == null || cfg == null)
                return null;

            Organism prefab = ResolvePrefab(source);
            if (prefab == null)
            {
                Debug.LogError("OrganismSpawn: missing Resources prefab for " + source.GetType().Name, source);
                return null;
            }

            GameObject go = Object.Instantiate(prefab.gameObject);
            var org = go.GetComponent<Organism>();
            Vector2 p = source.SimulationPosition + positionOffset;
            if (org is Flower flower)
                flower.Setup(cfg, p);
            else
                org.Initialize(cfg, p, source.SimulationVelocity, source.CruiseSpeed);
            if (addPredator)
                go.AddComponent<Predator>();
            if (addPrey)
                go.AddComponent<Prey>();
            return org;
        }

        static Organism ResolvePrefab(Organism source)
        {
            switch (source)
            {
                case SquareOrganism _:
                    return Resources.Load<SquareOrganism>(SquareOrganismPrefabResource);
                case CircleOrganism _:
                    return Resources.Load<CircleOrganism>(CircleOrganismPrefabResource);
                case StarOrganism _:
                    return Resources.Load<StarOrganism>(StarOrganismPrefabResource);
                case Flower _:
                    return Resources.Load<Flower>("Flower");
                default:
                    return Resources.Load<Organism>(DefaultOrganismPrefabResource);
            }
        }
    }
}
