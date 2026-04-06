using System.Collections;
using UnityEngine;

namespace Swarm
{
    [DisallowMultipleComponent]
    public sealed class Prey : OrganismType
    {
        Organism _organism;
        SpriteRenderer _spriteRenderer;
        bool _eaten;
        float _replicateTimer;
        float _replicateIntervalSeconds = 10f;
        float _replicateChance = 0.2f;

        void Awake()
        {
            _organism = GetComponent<Organism>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyReplicationDefaults();
        }

        void ApplyReplicationDefaults()
        {
            GameConfig cfg = null;
            if (SwarmSimulation.Instance != null)
                cfg = SwarmSimulation.Instance.Config;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>("GameConfig");
            if (cfg == null)
                return;

            _replicateIntervalSeconds = Mathf.Max(0.1f, cfg.preyReplicateIntervalSeconds);
            _replicateChance = Mathf.Clamp01(cfg.preyReplicateChance);
        }

        void FixedUpdate()
        {
            if (_eaten || _organism == null || !_organism.IsActiveInSimulation)
                return;

            _replicateTimer += Time.fixedDeltaTime;
            if (_replicateTimer < _replicateIntervalSeconds)
                return;

            _replicateTimer = 0f;
            if (Random.value >= _replicateChance)
                return;

            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : null;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>("GameConfig");
            if (cfg == null)
                return;

            float r = Mathf.Max(0.05f, transform.localScale.x * 0.2f);
            Vector2 offset = Random.insideUnitCircle * r;
            OrganismSpawn.SpawnSameOrganismType(_organism, cfg, offset, false, true);
        }

        /// <summary>Called by <see cref="Predator"/> when this prey is consumed.</summary>
        public void NotifyEaten()
        {
            if (_eaten)
                return;

            _eaten = true;
            if (_organism != null)
            {
                _organism.SetVelocity(Vector2.zero);
                _organism.SetSimulationActive(false);
            }

            float duration = 1f;
            if (SwarmSimulation.Instance != null && SwarmSimulation.Instance.Config != null)
                duration = Mathf.Max(0.01f, SwarmSimulation.Instance.Config.preyFadeDurationSeconds);

            StartCoroutine(FadeOutAndDestroy(duration));
        }

        IEnumerator FadeOutAndDestroy(float duration)
        {
            if (_spriteRenderer == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Color c = _spriteRenderer.color;
            float startAlpha = c.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = duration > 1e-6f ? Mathf.Clamp01(t / duration) : 1f;
                c.a = Mathf.Lerp(startAlpha, 0f, u);
                _spriteRenderer.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
