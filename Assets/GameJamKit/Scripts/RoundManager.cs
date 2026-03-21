using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls round progression:
///   Round 1 → collect 5 orbs  → pick biome
///   Round 2 → collect 10 orbs → pick biome
///   Round N → collect N*5 orbs → pick biome  (endless)
///
/// On round complete:
///   1. Game pauses (Time.timeScale = 0)
///   2. BuildChoicePanel appears
///   3. Player picks Forest or Industry
///   4. Corresponding static counter on Player increments
///   5. Game resumes, orb count resets, next target set
/// </summary>
public class RoundManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    public static RoundManager Instance { get; private set; }

    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("Round Config")]
    [SerializeField] private int startingOrbTarget = 5;
    [SerializeField] private int orbIncrement = 5;

    [Header("UI – wire these up in the Inspector")]
    [SerializeField] private GameObject buildChoicePanel;  // The whole popup
    [SerializeField] private Button forestButton;
    [SerializeField] private Button industryButton;
    [SerializeField] private TextMeshProUGUI roundLabelText;     // "Round 1", "Round 2"…
    [SerializeField] private TextMeshProUGUI buildPromptText;    // Flavour text
    [SerializeField] private TextMeshProUGUI ratioDisplayText;   // Shows current ratio

    // ─── State ───────────────────────────────────────────────────────────────

    private int currentRound = 1;

    /// <summary>Current orb threshold – read by Player to update HUD.</summary>
    public int CurrentOrbTarget { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CurrentOrbTarget = startingOrbTarget;
    }

    void Start()
    {
        // Safety check – these must be assigned in the Inspector
        if (buildChoicePanel == null)
            Debug.LogError("[RoundManager] buildChoicePanel is not assigned!");
        if (forestButton == null || industryButton == null)
            Debug.LogError("[RoundManager] Forest/Industry buttons are not assigned!");

        buildChoicePanel.SetActive(false);

        forestButton.onClick.AddListener(() => OnBuildChosen(isForest: true));
        industryButton.onClick.AddListener(() => OnBuildChosen(isForest: false));

        RefreshRoundLabel();
    }

    // ─── Called by Player ────────────────────────────────────────────────────

    /// <summary>Player calls this every time it picks up an orb.</summary>
    public void OnOrbCollected(int totalOrbsThisRound)
    {
        if (totalOrbsThisRound >= CurrentOrbTarget)
            ShowBuildChoice();
    }

    // ─── Build Panel ─────────────────────────────────────────────────────────

    void ShowBuildChoice()
    {
        // Pause everything except UI
        Time.timeScale = 0f;

        // Refresh the ratio display
        int f = Player.natureCount, i = Player.industryCount;
        if (ratioDisplayText)
            ratioDisplayText.text = $"Current ratio  {f}:{i}  " + GetRatioVerdict(f, i);

        if (buildPromptText)
            buildPromptText.text = GetBuildFlavourText(f, i);

        buildChoicePanel.SetActive(true);
    }

    void OnBuildChosen(bool isForest)
    {
        if (isForest)
            Player.natureCount++;
        else
            Player.industryCount++;

        // Advance round
        currentRound++;
        CurrentOrbTarget += orbIncrement;

        // Reset player orb count
        Player.Instance?.ResetOrbCount();

        // Hide panel & resume
        buildChoicePanel.SetActive(false);
        Time.timeScale = 1f;

        RefreshRoundLabel();

        Debug.Log($"[RoundManager] Round {currentRound} started. " +
                  $"Target: {CurrentOrbTarget} orbs. " +
                  $"Ratio: {Player.natureCount}:{Player.industryCount}");
    }

    // ─── UI Helpers ──────────────────────────────────────────────────────────

    void RefreshRoundLabel()
    {
        if (roundLabelText)
            roundLabelText.text = $"Round {currentRound}";
    }

    /// <summary>Human-readable verdict of current ratio.</summary>
    static string GetRatioVerdict(int forest, int industry)
    {
        if (industry == 0) return "(forest only – very easy)";

        float r = (float)forest / industry;
        if (r > 5f) return "(forest heavy – healing, slow)";
        if (r == 5f) return "(balanced ✓)";
        if (r >= 3f) return "(slightly industry)";
        return "(industry heavy – hard!)";
    }

    /// <summary>Flavour text nudging the player toward balance.</summary>
    static string GetBuildFlavourText(int forest, int industry)
    {
        if (industry == 0 && forest > 2)
            return "You're all forest. Industry picks will make things harder – but more exciting.";
        if (forest == 0 && industry > 0)
            return "Pure industry! Your health is draining fast. Plant some trees!";
        float r = (float)forest / Mathf.Max(1, industry);
        if (r > 5f) return "Very forest-heavy. Consider industry to increase difficulty.";
        if (r < 2f) return "Industry is dominating! Build forest to stabilise your health.";
        return "Pick your next biome. The 5:1 ratio is balanced.";
    }
}
