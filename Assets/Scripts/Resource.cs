using UnityEngine;

namespace Swarm
{
    /// <summary>Stationary resource agents (see <see cref="Flower"/>): no self-Vicsek update, no integration, excluded from live counts.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class Resource : Organism
    {
        public override bool IncludeInLiveOrganismCounts => false;

        public override bool SkipsSimulationDynamics => true;

        public override void Initialize(GameConfig config, Vector2 position, Vector2 velocity, float cruiseSpeed)
        {
            base.Initialize(config, position, velocity.sqrMagnitude > 1e-8f ? velocity : new Vector2(1e-6f, 0f), cruiseSpeed);
            SetVelocity(Vector2.zero);
        }

        protected override void UpdateNeighborSteering(SwarmSimulation sim, int fill, int maxK)
        {
            // Stationary resources do not update heading from Vicsek.
        }

        public override void SyncTransformFromSimulation()
        {
            Vector2 p = SimulationPosition;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
