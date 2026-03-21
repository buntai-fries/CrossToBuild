using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Config")]
    [SerializeField] private int startingOrbTarget = 5;
    [SerializeField] private int orbIncrement = 5;

    [Header("Health Reward Per Round")]
    [Tooltip("HP given on round complete at Round 1.")]
    [SerializeField] private float baseHealthReward = 25f;

    [Tooltip("Reward shrinks each round. 2 means Round 1=25, Round 5=17, Round 10=7.")]
    [SerializeField] private float rewardDecreasePerRound = 2f;

    [Tooltip("Minimum reward regardless of round. PlayerScript always gets something.")]
    [SerializeField] private float minHealthReward = 8f;

    [Header("UI — wire in Inspector")]
    [SerializeField] private GameObject buildChoicePanel;
    [SerializeField] private Button forestButton;
    [SerializeField] private Button industryButton;
    [SerializeField] private TextMeshProUGUI roundLabelText;
    [SerializeField] private TextMeshProUGUI buildPromptText;
    [SerializeField] private TextMeshProUGUI ratioDisplayText;

    // ─── Public state ─────────────────────────────────────────────────────────

    public int CurrentOrbTarget { get; private set; }

    /// <summary>Exposed so PlayerScript.HandleHealth() can scale drain by round.</summary>
    public int CurrentRound { get; private set; } = 1;

    // ─── Private ─────────────────────────────────────────────────────────────

    private bool panelOpen = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CurrentOrbTarget = startingOrbTarget;
        CurrentRound = 1;
    }

    void Start()
    {
        bool valid = true;
        if (buildChoicePanel == null)
        { Debug.LogError("[RoundManager] buildChoicePanel not assigned!"); valid = false; }
        if (forestButton == null)
        { Debug.LogError("[RoundManager] forestButton not assigned!"); valid = false; }
        if (industryButton == null)
        { Debug.LogError("[RoundManager] industryButton not assigned!"); valid = false; }

        if (!valid) { enabled = false; return; }

        forestButton.onClick.RemoveAllListeners();
        industryButton.onClick.RemoveAllListeners();
        forestButton.onClick.AddListener(() => OnBuildChosen(true));
        industryButton.onClick.AddListener(() => OnBuildChosen(false));

        buildChoicePanel.SetActive(false);
        panelOpen = false;

        RefreshRoundLabel();

        Debug.Log($"[RoundManager] Ready — Round {CurrentRound}  target={CurrentOrbTarget}");
    }

    // ─── Called by PlayerScript ────────────────────────────────────────────────────

    public void OnOrbCollected(int totalOrbsThisRound)
    {
        if (panelOpen) return;

        Debug.Log($"[RoundManager] Orbs: {totalOrbsThisRound}/{CurrentOrbTarget}");

        if (totalOrbsThisRound >= CurrentOrbTarget)
            ShowBuildChoice();
    }

    // ─── Build Panel ─────────────────────────────────────────────────────────

    void ShowBuildChoice()
    {
        if (panelOpen) return;
        panelOpen = true;

        Time.timeScale = 0f;

        int f = PlayerScript.natureCount, i = PlayerScript.industryCount;

        if (ratioDisplayText != null)
            ratioDisplayText.text = $"Ratio  {f} : {i}   {GetVerdict(f, i)}";

        if (buildPromptText != null)
            buildPromptText.text = GetFlavour(f, i);

        buildChoicePanel.SetActive(true);

        Debug.Log($"[RoundManager] Panel opened — Round {CurrentRound}  " +
                  $"ratio={f}:{i}");
    }

    void OnBuildChosen(bool isForest)
    {
        if (isForest) PlayerScript.natureCount++;
        else PlayerScript.industryCount++;

        // ── Health reward — decreases each round ──────────────────────────────
        float reward = Mathf.Max(
            minHealthReward,
            baseHealthReward - (CurrentRound - 1) * rewardDecreasePerRound
        );
        PlayerScript.Instance?.AddHealth(reward);

        Debug.Log($"[RoundManager] Choice: {(isForest ? "Forest" : "Industry")}  " +
                  $"+{reward:F0}HP  ratio={PlayerScript.natureCount}:{PlayerScript.industryCount}");

        // ── Advance round ─────────────────────────────────────────────────────
        CurrentRound++;
        CurrentOrbTarget += orbIncrement;

        PlayerScript.Instance?.ResetOrbCount();
        AudioManager.Instance?.PlayRoundComplete();
        buildChoicePanel.SetActive(false);
        panelOpen = false;
        Time.timeScale = 1f;

        RefreshRoundLabel();

        Debug.Log($"[RoundManager] Round {CurrentRound} starts — " +
                  $"target={CurrentOrbTarget}");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    void RefreshRoundLabel()
    {
        if (roundLabelText != null)
            roundLabelText.text = $"Round {CurrentRound}";
    }

    static string GetVerdict(int f, int i)
    {
        if (i == 0) return "(all forest)";
        float r = (float)f / i;
        if (r >= 5f) return "balanced";
        if (r >= 3f) return "slightly industry";
        return "industry heavy!";
    }

    static string GetFlavour(int f, int i)
    {
        if (i == 0 && f > 2) return "All forest — industry will make things harder.";
        if (f == 0) return "Pure industry! Build forest to slow health drain.";
        float r = (float)f / Mathf.Max(1, i);
        if (r > 5f) return "Forest heavy — good health, slow drain. Add industry?";
        if (r < 2f) return "Industry heavy — health draining fast. Build forest!";
        return "Near the 5:1 balance. Either choice is valid.";
    }
}