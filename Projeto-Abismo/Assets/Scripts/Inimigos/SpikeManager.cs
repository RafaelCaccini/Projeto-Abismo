using UnityEngine;
using System.Collections.Generic;

public class SpikeManager : MonoBehaviour
{
    public static SpikeManager Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorSpikePrefab;
    [SerializeField] private GameObject ceilingSpikePrefab;

    [Header("Settings")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spacing = 1.5f;

    private Queue<GameObject> floorPool = new Queue<GameObject>();
    private Queue<GameObject> ceilingPool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;

        CreatePool(floorSpikePrefab, floorPool);
        CreatePool(ceilingSpikePrefab, ceilingPool);
    }

    private void CreatePool(GameObject prefab, Queue<GameObject> pool)
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    // ================= SPAWN =================

    public void SpawnFloorSpikes(float centerX)
    {
        SpawnLine(centerX, floorPool, yOffset: -2f);
    }

    public void SpawnCeilingSpikes(float centerX)
    {
        SpawnLine(centerX, ceilingPool, yOffset: 5f);
    }

    private void SpawnLine(float centerX, Queue<GameObject> pool, float yOffset)
    {
        int count = 7;

        for (int i = -count / 2; i <= count / 2; i++)
        {
            if (pool.Count == 0) return;

            GameObject spike = pool.Dequeue();

            Vector3 pos = new Vector3(centerX + i * spacing, yOffset, 0f);
            spike.transform.position = pos;

            spike.SetActive(true);
        }
    }

    // ================= RETURN =================

    public void ReturnToPool(GameObject obj, bool isFloor)
    {
        obj.SetActive(false);

        if (isFloor)
            floorPool.Enqueue(obj);
        else
            ceilingPool.Enqueue(obj);
    }
}