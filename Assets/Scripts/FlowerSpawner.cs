using UnityEngine;

namespace Swarm
{
    /// <summary>Spawns <see cref="Flower"/> instances at random positions in the bounding disk on a timer.</summary>
    [DisallowMultipleComponent]
    public sealed class FlowerSpawner : MonoBehaviour
    {
        const string DefaultConfigResource = "GameConfig";

        float _timer;

        void FixedUpdate()
        {
            GameConfig cfg = SwarmSimulation.Instance != null ? SwarmSimulation.Instance.Config : null;
            if (cfg == null)
                cfg = Resources.Load<GameConfig>(DefaultConfigResource);
            if (cfg == null)
                return;

            _timer += Time.fixedDeltaTime;
            if (_timer < cfg.flowerSpawnIntervalSeconds)
                return;

            _timer = 0f;

            var prefab = Resources.Load<Flower>("Flower");
            if (prefab == null)
                return;

            Vector2 center = cfg.spawnCircleCenter;
            float R = cfg.boundingDomainRadius;
            float u = Mathf.Sqrt(Random.value);
            float t = Random.Range(0f, 2f * Mathf.PI);
            float r = u * R;
            Vector2 pos = center + new Vector2(r * Mathf.Cos(t), r * Mathf.Sin(t));

            GameObject go = Instantiate(prefab.gameObject);
            var flower = go.GetComponent<Flower>();
            if (flower != null)
                flower.Setup(cfg, pos);
        }
    }
}
