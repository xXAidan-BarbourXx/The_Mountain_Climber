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
        if (!IsGameOver)
        {
            Score += Time.deltaTime * ScoreMultiplier;
        }
    }

    public void ActivateScoreMultiplier(float duration, float multiplier)
    {
        StartCoroutine(ApplyScoreMultiplier(duration, multiplier));
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

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetGame()
    {
        Score = 0f;
        ScoreMultiplier = 1f;
        IsGameOver = false;
    }

    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        OnGameOver?.Invoke();
    }

}