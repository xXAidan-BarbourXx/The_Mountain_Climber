using UnityEngine;
using System.Collections.Generic;

public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject[] platformPrefabs;

    [Header("Initial Platforms (in order: Starter, Runner1, Runner2)")]
    public GameObject[] initialPlatforms;

    private LinkedList<GameObject> activePlatforms = new LinkedList<GameObject>();

    void Awake()
    {
        Instance = this;
        foreach (var p in initialPlatforms)
            activePlatforms.AddLast(p);

        for (int i = 1; i < initialPlatforms.Length; i++)
        {
            ObstacleSpawner spawner = initialPlatforms[i].GetComponent<ObstacleSpawner>();
            if (spawner != null)
                spawner.SpawnObstacles();
        }
    }


    public void SpawnNext()
    {
        GameObject furthest = activePlatforms.Last.Value;

        Transform spawnPoint = furthest.transform.Find("platformGenerationPoint");
        if (spawnPoint == null)
        {
            foreach (Transform t in furthest.GetComponentsInChildren<Transform>())
            {
                if (t.name == "platformGenerationPoint")
                {
                    spawnPoint = t;
                    break;
                }
            }
        }

        if (spawnPoint == null)
        {
            Debug.LogError("No platformGenerationPoint found on " + furthest.name);
            return;
        }

        GameObject prefab = platformPrefabs[Random.Range(0, platformPrefabs.Length)];
        GameObject newPlatform = Instantiate(prefab, spawnPoint.position, Quaternion.identity) as GameObject;

        newPlatform.name = "PlatformRunner";

        ObstacleSpawner spawner = newPlatform.GetComponent<ObstacleSpawner>();
        if (spawner != null)
            spawner.SpawnObstacles();

        activePlatforms.AddLast(newPlatform);
    }

    public void DestroyTail()
    {
        if (activePlatforms.Count == 0) return;

        GameObject tail = activePlatforms.First.Value;
        activePlatforms.RemoveFirst();
        Destroy(tail);
    }
}