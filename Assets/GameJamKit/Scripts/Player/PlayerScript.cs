using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerScript : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("Movement")]
    public float jumpForce = 10f;
    public float coyoteTime = 0.15f;

    [Header("Health Drain — Round Scaling")]
    [Tooltip("HP/sec drain at Round 1 when perfectly balanced. Keep this very low.")]
    public float baseDrainRound1 = 0.5f;

    [Tooltip("How much extra drain is added per round. " +
             "0.3 means Round 1=0.5, Round 5=1.7, Round 10=3.2 HP/sec at balance.")]
    public float drainPerRound = 0.3f;

    [Tooltip("Maximum base drain regardless of round. Cap so it doesn't get absurd.")]
    public float maxBaseDrain = 8f;

    [Header("Biome Balance")]
    [Tooltip("The ideal forest:industry ratio. 5 = 5:1")]
    public float balancedRatio = 5f;

    [Tooltip("Drain multiplier when perfectly balanced (ratio = 5:1). Should be 1.0")]
    public float balancedMultiplier = 1f;

    [Tooltip("Drain multiplier when maximally unbalanced (pure industry, ratio = 0). " +
             "3 means 3× drain at worst case.")]
    public float maxImbalanceMultiplier = 3f;

    [Tooltip("HP/sec regen when ratio is well above balanced. Keeps forest viable.")]
    public float regenRate = 1f;

    [Header("Orb Collection")]
    [Tooltip("Min seconds between collections. Prevents same-frame multi-collect.")]
    public float orbCollectCooldown = 0.3f;
    public GameObject orbParticlePrefab;

    [Header("UI Refs")]
    public TextMeshProUGUI orbCounterText;
    public TextMeshProUGUI healthText;

    // ─── Static biome counters ────────────────────────────────────────────────
    public static int natureCount = 0;
    public static int industryCount = 0;

    // ─── Singleton ───────────────────────────────────────────────────────────
    public static PlayerScript Instance { get; private set; }

    // ─── Public accessors ────────────────────────────────────────────────────
    public int CurrentOrbs { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public static (int forest, int industry) GetBiomeCounts() => (natureCount, industryCount);
    public static float GetRatio() => (float)natureCount / Mathf.Max(1, industryCount);

    // ─── Private ─────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float maxHealth = 100f;
    private float currentHealth;
    private bool isGrounded;
    private float coyoteTimer;
    private float startX;
    private float lastCollectTime = -999f;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        startX = transform.position.x;
        UpdateUI();

        Debug.Log($"[Player] Ready — startX={startX}");
    }

    void Update()
    {
        GroundCheck();
        HandleJump();
        HandleHealth();
        transform.position = new Vector3(startX, transform.position.y, 0f);
    }

    // ─── Ground Check ─────────────────────────────────────────────────────────

    void GroundCheck()
    {
        float halfH = sr.bounds.size.y * 0.5f - 0.05f;
        Vector2 origin = (Vector2)transform.position - Vector2.up * halfH;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.2f,
                                              LayerMask.GetMask("Ground"));
        isGrounded = hit.collider != null;
        Debug.DrawRay(origin, Vector2.down * 0.2f, isGrounded ? Color.green : Color.red);

        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    // ─── Jump ─────────────────────────────────────────────────────────────────

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimer = 0f;
        }

        if (Input.GetKey(KeyCode.Space) && rb.linearVelocity.y > 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * 0.5f * Time.deltaTime;
    }

    // ─── Health ───────────────────────────────────────────────────────────────

    void HandleHealth()
    {
        int round = RoundManager.Instance != null ? RoundManager.Instance.CurrentRound : 1;
        float ratio = GetRatio();

        // ── Base drain scales with round ──────────────────────────────────────
        // Round 1 = baseDrainRound1, each round adds drainPerRound
        // e.g. 0.5 + (1-1)*0.3 = 0.5 HP/s  →  round 10: 0.5 + 9*0.3 = 3.2 HP/s
        float baseDrain = Mathf.Min(
            baseDrainRound1 + (round - 1) * drainPerRound,
            maxBaseDrain
        );

        // ── Biome multiplier ──────────────────────────────────────────────────
        // t=1 → perfectly balanced (ratio == balancedRatio) → multiplier = 1
        // t=0 → worst case (ratio = 0, pure industry)       → multiplier = max
        // Clamped so going OVER the balanced ratio doesn't reduce below 1
        float t = Mathf.Clamp01(ratio / balancedRatio);
        float biomeMultiplier = Mathf.Lerp(maxImbalanceMultiplier, balancedMultiplier, t);

        // ── Final drain ───────────────────────────────────────────────────────
        float drain = baseDrain * biomeMultiplier;
        currentHealth -= drain * Time.deltaTime;

        // ── Regen when forest-heavy ───────────────────────────────────────────
        // Only kicks in when clearly above balanced ratio
        // Scales gently so it's never overpowered
        if (ratio > balancedRatio)
        {
            float regenScale = Mathf.Clamp01((ratio - balancedRatio) / balancedRatio);
            currentHealth += regenRate * regenScale * Time.deltaTime;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f) Die();

        UpdateUI();
    }

    // ─── Orb Collection ──────────────────────────────────────────────────────

    public void CollectOrb()
    {
        float timeSinceLast = Time.time - lastCollectTime;
        if (timeSinceLast < orbCollectCooldown)
        {
            Debug.LogWarning($"[Player] CollectOrb ignored — {timeSinceLast:F3}s since last " +
                             $"(cooldown={orbCollectCooldown}s)");
            return;
        }

        lastCollectTime = Time.time;
        CurrentOrbs++;

        if (orbParticlePrefab)
            Instantiate(orbParticlePrefab, transform.position, Quaternion.identity);

        Debug.Log($"[Player] Orb collected! " +
                  $"{CurrentOrbs}/{RoundManager.Instance?.CurrentOrbTarget}");

        RoundManager.Instance?.OnOrbCollected(CurrentOrbs);
        UpdateUI();
    }

    public void ResetOrbCount()
    {
        CurrentOrbs = 0;
        lastCollectTime = -999f;
        UpdateUI();
    }

    public void AddHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateUI();
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    void UpdateUI()
    {
        int target = RoundManager.Instance != null
            ? RoundManager.Instance.CurrentOrbTarget : 5;

        if (orbCounterText) orbCounterText.text = $"{CurrentOrbs} / {target}";
        if (healthText) healthText.text = $"HP  {Mathf.Round(currentHealth)}";
    }

    // ─── Death ────────────────────────────────────────────────────────────────

    public void Die()
    {
        Debug.Log("[Player] Died — hook up GameOver here.");
        enabled = false;
        rb.simulated = false;
    }
}
