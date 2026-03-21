using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerScript : MonoBehaviour
{
    [Header("Movement")]
    public float jumpForce = 10f;
    public float coyoteTime = 0.15f;

    [Header("Health Drain — Round Scaling")]
    public float baseDrainRound1 = 0.5f;
    public float drainPerRound = 0.3f;
    public float maxBaseDrain = 8f;

    [Header("Biome Balance")]
    public float balancedRatio = 5f;
    public float balancedMultiplier = 1f;
    public float maxImbalanceMultiplier = 3f;
    public float regenRate = 1f;

    [Header("Orb Collection")]
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
    private bool isDead = false;

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
    }

    void Update()
    {
        if (isDead) return;

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
        int round = RoundManager.Instance != null
            ? RoundManager.Instance.CurrentRound : 1;
        float ratio = GetRatio();

        float baseDrain = Mathf.Min(
            baseDrainRound1 + (round - 1) * drainPerRound,
            maxBaseDrain);

        float t = Mathf.Clamp01(ratio / balancedRatio);
        float biomeMult = Mathf.Lerp(maxImbalanceMultiplier, balancedMultiplier, t);
        currentHealth -= baseDrain * biomeMult * Time.deltaTime;

        if (ratio > balancedRatio)
        {
            float regenScale = Mathf.Clamp01((ratio - balancedRatio) / balancedRatio);
            currentHealth += regenRate * regenScale * Time.deltaTime;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f) Die();

        UpdateUI();
    }

    // ─── Orb ──────────────────────────────────────────────────────────────────

    public void CollectOrb()
    {
        float timeSinceLast = Time.time - lastCollectTime;
        if (timeSinceLast < orbCollectCooldown) return;

        lastCollectTime = Time.time;
        CurrentOrbs++;

        if (orbParticlePrefab)
            Instantiate(orbParticlePrefab, transform.position, Quaternion.identity);

        RoundManager.Instance?.OnOrbCollected(CurrentOrbs);
        AudioManager.Instance?.PlayOrbCollect();
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
        if (isDead) return; // Prevent Die() firing twice
        isDead = true;

        enabled = false;
        rb.simulated = false;

        Debug.Log("[PlayerScript] Died.");

        // Show game over screen
        GameOverManager.Instance?.ShowGameOver();
    }
}
