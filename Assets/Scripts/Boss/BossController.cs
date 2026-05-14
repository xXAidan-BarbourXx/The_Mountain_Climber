using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("FallObs")]
    [SerializeField] private GameObject fallObsPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float[] obstacleZOffsets = { 20f, 0f, -20f };
    [SerializeField] private float[] laneXPositions = { -3f, 0f, 3f };
    [SerializeField] private float spawnHeightOffset = 1f;

    [Header("Timing")]
    [SerializeField] private float xFollowDelay = 0.5f;
    [SerializeField] private float bossDuration = 30f;

    [Header("Powerup Bonus")]
    [SerializeField] private float powerUpTimeBonus = 5f;

    [Header("Activation")]
    [SerializeField] private float activationRange = 40f;

    [Header("Defeat Fall")]
    [SerializeField] private float fallSpeed = 9.81f;

    private bool activated = false;
    private float timer;
    private bool defeated = false;
    private bool defeatStarted = false;

    private Queue<(float time, float x)> xHistory = new Queue<(float, float)>();

    // FIX: was HashSet<float> but keys were strings — now correctly HashSet<string>
    private HashSet<string> triggeredZThresholds = new HashSet<string>();
    private float lastPlatformOriginZ = float.MaxValue;

    private void Start()
    {
        timer = bossDuration;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (defeated)
        {
            transform.position += Vector3.back * fallSpeed * Time.deltaTime;
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Wait until the player is within activation range before doing anything
        if (!activated)
        {
            if (player == null) return;
            if (Vector3.Distance(transform.position, player.position) > activationRange) return;
            activated = true;
        }

        // Mirror player X with delay
        if (player != null)
        {
            xHistory.Enqueue((Time.time, player.position.x));

            while (xHistory.Count > 1 && xHistory.Peek().time < Time.time - xFollowDelay)
                xHistory.Dequeue();

            float delayedX = xHistory.Count > 0 ? xHistory.Peek().x : transform.position.x;
            transform.position = new Vector3(delayedX, transform.position.y, transform.position.z);
        }

        // Count down boss timer
        timer -= Time.deltaTime;
        if (timer <= 0f && !defeatStarted)
        {
            defeatStarted = true;
            StartCoroutine(DefeatSequence());
            return;
        }

        CheckAndSpawnFallObs();
    }

    /// <summary>
    /// Called by PlatformManager each time a new boss-fight platform spawns,
    /// so the boss knows which Z thresholds to watch for.
    /// </summary>
    public void NotifyPlatformOriginZ(float worldZ)
    {
        // Only reset thresholds when we've moved to a genuinely new platform
        if (Mathf.Abs(worldZ - lastPlatformOriginZ) > 5f)
        {
            lastPlatformOriginZ = worldZ;
            triggeredZThresholds.Clear();
        }
    }

    /// <summary>
    /// Called by a powerup when the boss collides with it.
    /// Despawns the powerup and adds bonus time to the boss timer.
    /// </summary>
    public void OnPowerUpAbsorbed()
    {
        timer += powerUpTimeBonus;
        Debug.Log($"[Boss] Absorbed a powerup — timer extended to {timer:F1}s");
    }

    private void CheckAndSpawnFallObs()
    {
        if (fallObsPrefab == null || lastPlatformOriginZ == float.MaxValue) return;

        foreach (float zOff in obstacleZOffsets)
        {
            float thresholdZ = lastPlatformOriginZ + zOff;
            // FIX: key is now a string, matching HashSet<string>
            string key = thresholdZ.ToString("F1");

            if (!triggeredZThresholds.Contains(key) && transform.position.z >= thresholdZ)
            {
                triggeredZThresholds.Add(key);
                SpawnFallObsAtThreshold(thresholdZ);
            }
        }
    }

    private void SpawnFallObsAtThreshold(float worldZ)
    {
        int count = Random.Range(1, 3); // 1 or 2
        List<int> availableLanes = new List<int> { 0, 1, 2 };
        ShuffleList(availableLanes);

        for (int i = 0; i < Mathf.Min(count, availableLanes.Count); i++)
        {
            Vector3 spawnPos = new Vector3(
                laneXPositions[availableLanes[i]],
                transform.position.y + spawnHeightOffset,
                worldZ
            );
            Instantiate(fallObsPrefab, spawnPos, Quaternion.identity);
        }
    }

    private IEnumerator DefeatSequence()
    {
        defeated = true;

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyBossDefeated();

        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}