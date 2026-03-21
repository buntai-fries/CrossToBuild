using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI — wire in Inspector")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI ratioText;
    [SerializeField] private Button restartButton;

    [Header("Scene")]
    [Tooltip("Name of your game scene — must match exactly what's in Build Settings.")]
    [SerializeField] private string gameSceneName = "GameScene";

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("[GameOverManager] gameOverPanel not assigned!");
            return;
        }

        gameOverPanel.SetActive(false);

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from PlayerScript.Die() to show the game over screen.
    /// </summary>
    public void ShowGameOver()
    {
        // Pause everything
        Time.timeScale = 0f;

        // ── Fill in stats ──────────────────────────────────────────────────────
        int round = RoundManager.Instance != null
            ? RoundManager.Instance.CurrentRound : 1;

        int forest = PlayerScript.natureCount;
        int industry = PlayerScript.industryCount;

        if (titleText != null)
            titleText.text = "GAME OVER";

        if (roundText != null)
            roundText.text = round > 1
                ? $"You reached Round {round}"
                : "You didn't make it past Round 1";

        if (ratioText != null)
        {
            string verdict = GetVerdict(forest, industry);
            ratioText.text = $"Final ratio  {forest} : {industry}\n{verdict}";
        }

        gameOverPanel.SetActive(true);
        AudioManager.Instance?.PlayGameOver();

        Debug.Log($"[GameOverManager] Game Over — Round={round}  " +
                  $"ratio={forest}:{industry}");
    }

    // ─────────────────────────────────────────────────────────────────────────

    void RestartGame()
    {
        // Reset static state before reload
        // Static fields survive scene reload so must be manually cleared
        PlayerScript.natureCount = 0;
        PlayerScript.industryCount = 0;

        Time.timeScale = 1f;

        SceneManager.LoadScene(gameSceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────

    static string GetVerdict(int forest, int industry)
    {
        if (forest == 0 && industry == 0)
            return "You never built anything.";
        if (industry == 0)
            return "Pure forest. Try mixing in industry next time.";
        float r = (float)forest / industry;
        if (r >= 5f) return "Well balanced. Industry killed you.";
        if (r >= 3f) return "Slightly industry heavy.";
        if (r >= 1f) return "Industry dominated your run.";
        return "Pure industry. No wonder the health drained fast.";
    }
}
