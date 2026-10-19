
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance { get; private set; }

    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>>
        pools = new();

    private readonly Dictionary<ParticleSystem, ParticleSystem>
        instancePrefabs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Play(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<ParticleSystem>();
            pools[prefab] = queue;
        }

        ParticleSystem effect = null;

        while (queue.Count > 0 && effect == null)
            effect = queue.Dequeue();

        if (effect == null)
        {
            effect = Instantiate(prefab, transform);
            instancePrefabs[effect] = prefab;
        }

        effect.transform.position = position;
        effect.gameObject.SetActive(true);

        effect.Stop(true,
            ParticleSystemStopBehavior.StopEmittingAndClear);

        effect.Play(true);

        StartCoroutine(ReturnWhenFinished(effect));
    }

    private IEnumerator ReturnWhenFinished(ParticleSystem effect)
    {
        yield return new WaitUntil(
            () => effect == null || !effect.IsAlive(true)
        );

        if (effect == null)
            yield break;

        effect.gameObject.SetActive(false);

        if (instancePrefabs.TryGetValue(effect, out var prefab))
            pools[prefab].Enqueue(effect);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
