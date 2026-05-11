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

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += ShowGameOverScreen;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOverScreen;
        }
    }

    private void Update()
    {
        // Update live score only while playing
        if (GameManager.Instance != null &&
            !GameManager.Instance.IsGameOver &&
            scoreText != null)
        {
            scoreText.text = $"Score: {Mathf.FloorToInt(GameManager.Instance.Score)}";
        }
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null && GameManager.Instance != null)
        {
            finalScoreText.text = $"GAME OVER\nFinal Score: {Mathf.FloorToInt(GameManager.Instance.Score)}";
        }
    }

    public void RestartButtonPressed()
    {
        Time.timeScale = 1f;                    // Just in case
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}