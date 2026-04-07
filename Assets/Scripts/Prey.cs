using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    public sealed class Prey : OrganismType
    {
        const string DefaultConfigResource = "GameConfig";

        Organism _organism;
        readonly List<Flower> _flowersInRange = new List<Flower>();

        float _baseVisualScale = 1.0f;
        int _nectarConsumed;
        float _nectarAccumulator;

        void Awake()
        {
            _organism = GetComponent<Organism>();
            _baseVisualScale = Mathf.Max(0.05f, transform.localScale.x);
        }

        /// <summary>Vicsek weight this prey applies to <see cref="Flower"/> neighbors (called from <see cref="Flower.GetVicsekWeightForReceiver"/>).</summary>
        public float GetVicsekWeightForFlowerNeighbor(SwarmSimulation sim) =>
            sim != null && sim.Config != null ? sim.Config.flowerVicsekWeight : 1f;

        void FixedUpdate()
        {
            if (_organism == null || _organism.IsEaten || !_organism.IsActiveInSimulation)
                return;

            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : null;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>(DefaultConfigResource);
            if (cfg == null)
                return;

            SwarmSimulation sim = SwarmSimulation.Instance;
            if (sim == null)
                return;

            float gatherR = cfg.preyFlowerGatherRadius;
            Vector2 p = _organism.SimulationPosition;

            if (FindNearestFlowerInRange(sim, p, gatherR) == null)
                return;

            _nectarAccumulator += cfg.preyNectarConsumptionPerSecond * Time.fixedDeltaTime;
            int steps = Mathf.FloorToInt(_nectarAccumulator);
            _nectarAccumulator -= steps;

            float addPerNectar = _baseVisualScale * (cfg.preyNectarScaleAdditivePercent / 100f);

            for (int s = 0; s < steps; s++)
            {
                Flower f = FindNearestFlowerInRange(sim, p, gatherR);
                if (f == null)
                    break;

                if (f.ConsumeNectar(1) < 1)
                    continue;

                _nectarConsumed++;
                transform.localScale += Vector3.one * addPerNectar;

                if (_nectarConsumed >= cfg.preyReplicationNectarThreshold)
                {
                    Replicate(cfg);
                    _nectarConsumed = 0;
                }
            }
        }

        Flower FindNearestFlowerInRange(SwarmSimulation sim, Vector2 p, float gatherR)
        {
            sim.CollectFlowersWithinRadius(p, gatherR, _flowersInRange);

            Flower best = null;
            float bestDsq = float.MaxValue;
            for (int i = 0; i < _flowersInRange.Count; i++)
            {
                Flower f = _flowersInRange[i];
                float dsq = (f.SimulationPosition - p).sqrMagnitude;
                if (dsq < bestDsq)
                {
                    bestDsq = dsq;
                    best = f;
                }
            }

            return best;
        }

        void Replicate(GameConfig cfg)
        {
            float r = Mathf.Max(0.05f, transform.localScale.x * 0.2f);
            Vector2 offset = Random.insideUnitCircle * r;
            OrganismSpawn.SpawnSameOrganismType(_organism, cfg, offset, false, true);
            transform.localScale = Vector3.one * _baseVisualScale;
        }
    }
}
