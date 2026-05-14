using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject jumpObsPrefab;
    public GameObject duckObsPrefab;
    public GameObject dodgeObsPrefab;
    public GameObject fallObsPrefab;

    [Header("Power-Up Prefabs")]
    public GameObject higherJumpPrefab;
    public GameObject invulnerabilityPrefab;
    public GameObject scoreMultiplierPrefab;
    public GameObject launchPrefab;
    public GameObject oxygenRefillPrefab;


    [Header("Spawn Settings")]
    public float spawnHeightOffset = 1f;
    public float[] laneXPositions = { -3f, 0f, 3f };
    public float nearZOffset = 20f;
    public float centerZOffset = 0f;
    public float farZOffset = -20f;

    [Header("Spawn Chances")]
    [Range(0f, 1f)] public float powerUpSpawnChance = 0.3f;
    [Range(0f, 1f)] public float twoLaneBlockChance = 0.4f;
    [Range(0f, 1f)] public float threeLaneBlockChance = 0.15f;
    public bool enableMultiLaneBlocking = true;

    private int dodgeCountThisSpawn = 0;

    public void SpawnObstacles()
    {
        dodgeCountThisSpawn = 0;
        SpawnZone(nearZOffset);
        SpawnZone(centerZOffset);
        SpawnZone(farZOffset);
    }

    public void SpawnBossObstacles()
    {
        SpawnPowerUpOnlyZone(nearZOffset);
        SpawnPowerUpOnlyZone(centerZOffset);
        SpawnPowerUpOnlyZone(farZOffset);
    }

    float GetObstacleHeightOffset(GameObject prefab)
    {
        if (prefab == dodgeObsPrefab) return 1f;
        if (prefab == duckObsPrefab) return 2f;
        if (prefab == jumpObsPrefab) return 0.2f;
        return spawnHeightOffset;
    }

    void SpawnZone(float zOffset)
    {
        List<int> availableLanes = new List<int> { 0, 1, 2 };
        List<int> obstacleLanes = new List<int>();

        int lanesToBlock = 1;

        if (enableMultiLaneBlocking)
        {
            float roll = Random.value;
            if (roll < threeLaneBlockChance)
                lanesToBlock = 3;
            else if (roll < threeLaneBlockChance + twoLaneBlockChance)
                lanesToBlock = 2;
        }

        List<int> shuffled = new List<int>(availableLanes);
        ShuffleList(shuffled);

        for (int i = 0; i < lanesToBlock; i++)
            obstacleLanes.Add(shuffled[i]);

        foreach (int lane in obstacleLanes)
            availableLanes.Remove(lane);

        foreach (int lane in obstacleLanes)
        {
            GameObject prefab = GetObstaclePrefab();
            if (prefab != null)
            {
                Vector3 pos = new Vector3(
                    laneXPositions[lane],
                    transform.position.y + GetObstacleHeightOffset(prefab),
                    transform.position.z + zOffset
                );
                GameObject obs = Instantiate(prefab, pos, Quaternion.identity);
                obs.transform.SetParent(transform, worldPositionStays: true);
            }
        }

        if (Random.value <= powerUpSpawnChance && availableLanes.Count > 0)
            SpawnPowerUpInLanes(availableLanes, zOffset);
    }

    void SpawnPowerUpOnlyZone(float zOffset)
    {
        List<int> allLanes = new List<int> { 0, 1, 2 };

        if (Random.value <= powerUpSpawnChance)
            SpawnPowerUpInLanes(allLanes, zOffset);
    }

    void SpawnPowerUpInLanes(List<int> availableLanes, float zOffset)
    {
        int powerUpLane = availableLanes[Random.Range(0, availableLanes.Count)];
        Vector3 powerUpPos = new Vector3(
            laneXPositions[powerUpLane],
            transform.position.y + spawnHeightOffset,
            transform.position.z + zOffset
        );

        GameObject powerUpPrefab = GetRandomPowerUpPrefab();
        if (powerUpPrefab != null)
        {
            GameObject pu = Instantiate(powerUpPrefab, powerUpPos, Quaternion.identity);
            pu.transform.SetParent(transform, worldPositionStays: true);
        }
    }

    GameObject GetObstaclePrefab()
    {
        List<int> options = new List<int> { 0, 1, 2 };
        if (dodgeCountThisSpawn >= 2)
            options.Remove(2);

        int roll = options[Random.Range(0, options.Count)];
        if (roll == 2) dodgeCountThisSpawn++;

        return roll switch
        {
            0 => jumpObsPrefab,
            1 => duckObsPrefab,
            _ => dodgeObsPrefab,
        };
    }

    GameObject GetRandomPowerUpPrefab()
    {
        List<GameObject> pool = new List<GameObject>();

        if (higherJumpPrefab != null) pool.Add(higherJumpPrefab);
        if (invulnerabilityPrefab != null) pool.Add(invulnerabilityPrefab);
        if (scoreMultiplierPrefab != null) pool.Add(scoreMultiplierPrefab);
        if (launchPrefab != null) pool.Add(launchPrefab);
        if (oxygenRefillPrefab != null) pool.Add(oxygenRefillPrefab);

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}