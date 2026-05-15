using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Live Score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Boss Warning Banner")]
    [SerializeField] private TextMeshProUGUI bossWarningText;
    [SerializeField] private float warningDuration = 2f;

    private Coroutine bossWarningCoroutine;

    private void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (bossWarningText != null) bossWarningText.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += ShowGameOverScreen;
            GameManager.Instance.OnBossThreshold += ShowBossWarning;
            GameManager.Instance.OnBossDefeated += OnBossDefeated;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOverScreen;
            GameManager.Instance.OnBossThreshold -= ShowBossWarning;
            GameManager.Instance.OnBossDefeated -= OnBossDefeated;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver && scoreText != null)
            scoreText.text = $"Score: {Mathf.FloorToInt(GameManager.Instance.Score)}";
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (finalScoreText != null && GameManager.Instance != null)
            finalScoreText.text = $"GAME OVER\nFinal Score: {Mathf.FloorToInt(GameManager.Instance.Score)}";
    }

    private void ShowBossWarning()
    {
        if (bossWarningText == null) return;

        if (bossWarningCoroutine != null) StopCoroutine(bossWarningCoroutine);
        bossWarningCoroutine = StartCoroutine(BossWarningRoutine());
    }

    private IEnumerator BossWarningRoutine()
    {
        bossWarningText.text = "Boss Incoming!";
        bossWarningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(warningDuration);
        bossWarningText.gameObject.SetActive(false);
        bossWarningCoroutine = null;
    }

    private void OnBossDefeated()
    {
        Debug.Log("[UIManager] Boss defeated!");
    }

    public void RestartButtonPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}