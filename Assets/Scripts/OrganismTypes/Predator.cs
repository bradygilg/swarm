using System.Collections.Generic;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(0)]
    public sealed class Predator : OrganismType
    {
        Organism _organism;
        float _eatTimer;
        float _eatIntervalSeconds;
        float _huntRadius;
        float _growthPercentOnEat;
        float _originalUniformScale = 1f;
        /// <summary>Added to <see cref="Transform.localScale"/> each eat: Z% of original scale (uniform).</summary>
        Vector3 _additiveScalePerEat;

        static readonly List<Organism> s_RadiusBuffer = new List<Organism>();

        void Awake()
        {
            _organism = GetComponent<Organism>();
            _originalUniformScale = transform.localScale.x;
            if (_originalUniformScale < 1e-6f)
                _originalUniformScale = 1f;
            ApplyConfigDefaults();
            RecomputeAdditiveScalePerEat();
        }

        void RecomputeAdditiveScalePerEat()
        {
            float delta = _originalUniformScale * (_growthPercentOnEat / 100f);
            _additiveScalePerEat = new Vector3(delta, delta, delta);
        }

        void ApplyConfigDefaults()
        {
            GameConfig cfg = null;
            if (SwarmSimulation.Instance != null)
                cfg = SwarmSimulation.Instance.Config;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>("GameConfig");

            if (cfg == null)
            {
                _eatIntervalSeconds = 5f;
                _huntRadius = 1f;
                _growthPercentOnEat = 10f;
                return;
            }

            OrganismTypeConfig typeCfg = cfg.OrganismTypeConfig;
            _eatIntervalSeconds = Mathf.Max(1e-4f, typeCfg.predatorEatIntervalSeconds);
            _huntRadius = Mathf.Max(1e-4f, typeCfg.predatorHuntRadius);
            _growthPercentOnEat = typeCfg.predatorGrowthPercentOnEat;
        }

        void FixedUpdate()
        {
            if (_organism == null || !_organism.IsActiveInSimulation)
                return;

            SwarmSimulation sim = SwarmSimulation.Instance;
            if (sim == null)
                return;

            _eatTimer += Time.fixedDeltaTime;
            if (_eatTimer < _eatIntervalSeconds)
                return;

            _eatTimer = 0f;

            s_RadiusBuffer.Clear();
            sim.CollectOrganismsWithinRadius(_organism.SimulationPosition, _huntRadius, s_RadiusBuffer);

            int n = 0;
            for (int i = 0; i < s_RadiusBuffer.Count; i++)
            {
                Organism o = s_RadiusBuffer[i];
                if (o == null || ReferenceEquals(o, _organism))
                    continue;
                if (!o.IsActiveInSimulation)
                    continue;
                if (!o.Eatable)
                    continue;
                // if (o.GetComponent<Prey>() == null)
                //     continue;

                s_RadiusBuffer[n++] = o;
            }

            if (n == 0)
                return;

            float totalWeight = 0f;
            for (int i = 0; i < n; i++)
                totalWeight += VictimSelectionWeight(s_RadiusBuffer[i]);

            float pick = Random.Range(0f, totalWeight);
            Organism victim = s_RadiusBuffer[n - 1];
            for (int i = 0; i < n; i++)
            {
                pick -= VictimSelectionWeight(s_RadiusBuffer[i]);
                if (pick < 0f)
                {
                    victim = s_RadiusBuffer[i];
                    break;
                }
            }
            // Prey preyComp = victim.GetComponent<Prey>();
            // if (preyComp == null)
            //     return;

            transform.localScale += _additiveScalePerEat;

            victim.NotifyEaten();

            if (transform.localScale.x >= _originalUniformScale * 2f - 1e-4f)
                SplitIntoTwoPredators();
        }

        /// <summary>Prey and other non-predators are 3× as likely to be chosen as another <see cref="Predator"/>.</summary>
        static float VictimSelectionWeight(Organism o)
        {
            if (o == null)
                return 0f;
            return o.GetComponent<Predator>() != null ? 1f : 3f;
        }

        void SplitIntoTwoPredators()
        {
            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : null;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>("GameConfig");
            if (cfg == null || _organism == null)
                return;

            Vector2 v = _organism.SimulationVelocity;
            Vector2 sep = new Vector2(-v.y, v.x);
            if (sep.sqrMagnitude < 1e-6f)
                sep = Vector2.right;
            sep = sep.normalized * Mathf.Max(0.05f, _originalUniformScale * 0.25f);

            _organism.SplitIntoTwoClones(cfg, sep, out _, out _);
            Destroy(gameObject);
        }
    }
}
