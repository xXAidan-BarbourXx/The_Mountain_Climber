using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
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

    [Header("Movement")]
    [SerializeField] private float playerSpeed = 10f;

    [Header("Defeat")]
    [SerializeField] private float fallSpeed = 9.81f;

    private bool activated = false;
    private float timer;
    private bool defeated = false;
    private bool defeatStarted = false;

    private Queue<(float time, float x)> xHistory = new Queue<(float, float)>();
    private HashSet<string> triggeredZThresholds = new HashSet<string>();
    private float lastPlatformOriginZ = float.MaxValue;

    private void Awake()
    {
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.freezeRotation = true;
        }

        timer = bossDuration;
    }

    private void Start()
    {
        if (PlayerController.Instance == null)
            Debug.LogError("[Boss] PlayerController.Instance is null in Start.");

        timer = bossDuration;
    }

    private void Update()
    {
        if (defeated)
        {
            transform.position += Vector3.back * (fallSpeed * Time.deltaTime);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (PlayerController.Instance == null) return;

        Vector3 playerPos = PlayerController.Instance.RigidbodyPosition;

        if (!activated)
        {
            float distZ = Mathf.Abs(transform.position.z - playerPos.z);
            if (distZ > activationRange)
            {
                Debug.Log($"[Boss] Waiting to activate — player Z: {playerPos.z}, boss Z: {transform.position.z}, distZ: {distZ}");
                return;
            }

            activated = true;
            Debug.Log("[Boss] Activated — beginning flee sequence.");
        }

        transform.position += Vector3.forward * (playerSpeed * Time.deltaTime);

        xHistory.Enqueue((Time.time, playerPos.x));
        while (xHistory.Count > 1 && xHistory.Peek().time < Time.time - xFollowDelay)
            xHistory.Dequeue();

        float delayedX = xHistory.Count > 0 ? xHistory.Peek().x : transform.position.x;
        transform.position = new Vector3(delayedX, transform.position.y, transform.position.z);

        timer -= Time.deltaTime;
        if (timer <= 0f && !defeatStarted)
        {
            defeatStarted = true;
            StartCoroutine(DefeatSequence());
            return;
        }

        CheckAndSpawnFallObs();
    }

    public void NotifyPlatformOriginZ(float worldZ)
    {
        if (Mathf.Abs(worldZ - lastPlatformOriginZ) > 5f)
        {
            lastPlatformOriginZ = worldZ;
            triggeredZThresholds.Clear();
        }
    }

    private void UpdatePlatformOriginFallback()
    {
        if (PlayerController.Instance == null) return;

        Vector3 playerPos = PlayerController.Instance.RigidbodyPosition;

        if (lastPlatformOriginZ == float.MaxValue)
        {
            lastPlatformOriginZ = playerPos.z;
            return;
        }

        if (playerPos.z > lastPlatformOriginZ + 60f)
        {
            lastPlatformOriginZ = playerPos.z;
            triggeredZThresholds.Clear();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PowerUp"))
        {
            OnPowerUpAbsorbed();
            Destroy(other.gameObject);
        }
    }

    public void OnPowerUpAbsorbed()
    {
        timer += powerUpTimeBonus;
        Debug.Log($"[Boss] Absorbed powerup — timer now {timer:F1}s");
    }

    private void CheckAndSpawnFallObs()
    {
        UpdatePlatformOriginFallback();
        if (fallObsPrefab == null || lastPlatformOriginZ == float.MaxValue) return;

        foreach (float zOff in obstacleZOffsets)
        {
            float thresholdZ = lastPlatformOriginZ + zOff;
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
        int count = Random.Range(1, 3);
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