using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public float Score { get; private set; }
    public float ScoreMultiplier { get; private set; } = 1f;
    public bool IsGameOver { get; private set; }
    public event System.Action OnGameOver;
    public event System.Action OnBossThreshold;
    public event System.Action OnBossDefeated;
    private bool bossThresholdFired = false;
    private const float BossScoreThreshold = 100f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ResetGame();
    }

    private void Update()
    {
        if (IsGameOver) return;
        Score += Time.deltaTime * ScoreMultiplier;

        if (!bossThresholdFired && Score >= BossScoreThreshold)
        {
            bossThresholdFired = true;
            Debug.Log("[GameManager] Boss threshold reached! Firing OnBossThreshold event.");
            Debug.Log($"[GameManager] OnBossThreshold has {(OnBossThreshold == null ? 0 : OnBossThreshold.GetInvocationList().Length)} listeners.");
            OnBossThreshold?.Invoke();
        }
    }

    public void ActivateScoreMultiplier(float duration, float multiplier)
    {
        StartCoroutine(ApplyScoreMultiplier(duration, multiplier));
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetGame()
    {
        Score = 0f;
        ScoreMultiplier = 1f;
        IsGameOver = false;
        bossThresholdFired = false;
    }

    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        OnGameOver?.Invoke();
    }

    public void NotifyBossDefeated()
    {
        OnBossDefeated?.Invoke();
    }

    private IEnumerator ApplyScoreMultiplier(float duration, float multiplier)
    {
        float original = ScoreMultiplier;
        ScoreMultiplier = multiplier;
        Debug.Log("ScoreMultiplier STARTED");
        yield return new WaitForSeconds(duration);
        ScoreMultiplier = original;
        Debug.Log("ScoreMultiplier ENDED");
    }
}