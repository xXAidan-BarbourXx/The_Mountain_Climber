using UnityEngine;
using System.Collections.Generic;

public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject platformPrefab;

    [Header("Initial Platforms (in order: Starter, Runner1, Runner2)")]
    public GameObject[] initialPlatforms;

    [Header("Boss")]
    [SerializeField] private GameObject bossPrefab;

    private LinkedList<GameObject> activePlatforms = new LinkedList<GameObject>();
    private bool bossPlatformPending = false;
    private bool bossAlive = false;
    private BossController activeBoss = null;

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

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossThreshold += HandleBossThreshold;
            GameManager.Instance.OnBossDefeated += HandleBossDefeated;

            Debug.Log("[PlatformManager] Subscribed to boss events.");
        }
        else
        {
            Debug.LogError("[PlatformManager] GameManager.Instance is STILL null in Start!");
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossThreshold -= HandleBossThreshold;
            GameManager.Instance.OnBossDefeated -= HandleBossDefeated;
        }
    }

    public void SpawnNext(Transform platformGenerationPoint)
    {
        GameObject newPlatform = Instantiate(platformPrefab,
                                             platformGenerationPoint.position,
                                             Quaternion.identity);

        ObstacleSpawner spawner = newPlatform.GetComponent<ObstacleSpawner>();

        if (bossPlatformPending)
        {
            Debug.Log("[PlatformManager] Spawning BOSS platform.");
            bossPlatformPending = false;
            bossAlive = true;
            newPlatform.name = "PlatformBoss";

            if (spawner != null)
                spawner.SpawnBossObstacles();

            if (bossPrefab != null)
            {
                float farZ = spawner != null ? spawner.farZOffset : -20f;
                float height = spawner != null ? spawner.spawnHeightOffset : 1f;
                Vector3 bossPos = new Vector3(
                    platformGenerationPoint.position.x,
                    platformGenerationPoint.position.y + height,
                    platformGenerationPoint.position.z + farZ
                );
                GameObject bossGO = Instantiate(bossPrefab, bossPos, Quaternion.identity);
                activeBoss = bossGO.GetComponent<BossController>();
                activeBoss?.NotifyPlatformOriginZ(newPlatform.transform.position.z);
                Debug.Log($"[PlatformManager] Boss spawned at {bossPos}.");
            }
            else
            {
                Debug.LogWarning("[PlatformManager] bossPrefab is not assigned in Inspector!");
            }
        }
        else if (bossAlive)
        {
            newPlatform.name = "PlatformBossFight";
            if (spawner != null)
                spawner.SpawnBossObstacles();
            activeBoss?.NotifyPlatformOriginZ(newPlatform.transform.position.z);
        }
        else
        {
            newPlatform.name = "PlatformRunner";
            if (spawner != null)
                spawner.SpawnObstacles();
        }

        activePlatforms.AddLast(newPlatform);
    }

    public void DestroyTail()
    {
        if (activePlatforms.Count == 0) return;
        GameObject tail = activePlatforms.First.Value;
        activePlatforms.RemoveFirst();
        Destroy(tail);
    }

    private void HandleBossThreshold()
    {
        Debug.Log("[PlatformManager] HandleBossThreshold called — boss platform will spawn next.");
        bossPlatformPending = true;
    }

    private void HandleBossDefeated()
    {
        Debug.Log("[PlatformManager] HandleBossDefeated called — returning to normal platforms.");
        bossAlive = false;
        activeBoss = null;
    }
}