using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject jumpObsPrefab;
    public GameObject duckObsPrefab;
    public GameObject dodgeObsPrefab;

    [Header("Power-Up Prefabs")]
    public GameObject higherJumpPrefab;
    public GameObject invulnerabilityPrefab;
    public GameObject scoreMultiplierPrefab;

    [Header("Spawn Settings")]
    public float spawnHeightOffset = 1f;
    public float[] laneXPositions = { -3f, 0f, 3f };
    public float nearZOffset = 20f;
    public float farZOffset = -20f;

    [Header("Spawn Chance")]
    [Range(0f, 1f)] public float powerUpSpawnChance = 0.3f;

    public void SpawnObstacles()
    {
        SpawnZone(nearZOffset);
        SpawnZone(farZOffset);
    }

    void SpawnZone(float zOffset)
    {
        List<int> availableLanes = new List<int> { 0, 1, 2 };

        // Pick obstacle lane
        int obstacleLaneIndex = Random.Range(0, availableLanes.Count);
        int obstacleLane = availableLanes[obstacleLaneIndex];
        availableLanes.RemoveAt(obstacleLaneIndex);

        Vector3 obstaclePos = new Vector3(
            laneXPositions[obstacleLane],
            transform.position.y + spawnHeightOffset,
            transform.position.z + zOffset
        );

        GameObject obstaclePrefab = GetObstaclePrefab();
        if (obstaclePrefab != null)
            Instantiate(obstaclePrefab, obstaclePos, Quaternion.identity, null);

        // Pick power-up lane from remaining lanes
        if (Random.value <= powerUpSpawnChance && availableLanes.Count > 0)
        {
            int powerUpLane = availableLanes[Random.Range(0, availableLanes.Count)];
            Vector3 powerUpPos = new Vector3(
                laneXPositions[powerUpLane],
                transform.position.y + spawnHeightOffset,
                transform.position.z + zOffset
            );

            GameObject powerUpPrefab = GetRandomPowerUpPrefab();
            if (powerUpPrefab != null)
                Instantiate(powerUpPrefab, powerUpPos, Quaternion.identity, null);
        }
    }

    GameObject GetObstaclePrefab()
    {
        int roll = Random.Range(0, 3);
        return roll switch
        {
            0 => jumpObsPrefab,
            1 => duckObsPrefab,
            _ => dodgeObsPrefab,
        };
    }

    GameObject GetRandomPowerUpPrefab()
    {
        int roll = Random.Range(0, 3);
        return roll switch
        {
            0 => higherJumpPrefab,
            1 => invulnerabilityPrefab,
            _ => scoreMultiplierPrefab,
        };
    }
}