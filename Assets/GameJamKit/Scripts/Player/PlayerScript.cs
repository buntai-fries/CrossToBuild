//using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("Movement")]
    public float jumpForce = 10f;
    public float coyoteTime = 0.15f;

    [Header("Orb Collection")]
    public float magnetRange = 2f;
    public GameObject orbParticlePrefab;

    [Header("Health")]
    public float maxHealth = 100f;
    public float baseDrainRate = 2f;          // HP/sec – always draining
    public float industryDrainMultiplier = 3f; // Multiplier when ratio is 0:1 (all industry)
    public float regenRate = 1f;            // HP/sec bonus regen when ratio > 5:1 (forest heavy)

    [Header("Biome Balance")]
    [Tooltip("5 means 5:1 forest:industry is the balanced point")]
    public float balancedRatio = 5f;

    [Header("UI Refs")]
    public TextMeshProUGUI orbCounterText;
    public TextMeshProUGUI healthText;

    // ─── Static biome counters (set by RoundManager) ─────────────────────────

    /// <summary>Number of Forest biome builds chosen by player.</summary>
    public static int natureCount = 0;
    /// <summary>Number of Industry biome builds chosen by player.</summary>
    public static int industryCount = 0;

    // ─── Singleton ───────────────────────────────────────────────────────────

    public static Player Instance { get; private set; }

    // ─── Public accessors for other systems ──────────────────────────────────

    public int CurrentOrbs { get; private set; }
    public float CurrentHealth => currentHealth;

    /// <summary>Returns (forestBuilt, industryBuilt) for parallax/spawner.</summary>
    public static (int forest, int industry) GetBiomeCounts() => (natureCount, industryCount);

    /// <summary>Raw forest:industry ratio. Clamped to prevent Lerp issues.</summary>
    public static float GetRatio()
        => (float)natureCount / Mathf.Max(1, industryCount);

    // ─── Private ─────────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float currentHealth;
    private bool isGrounded;
    private float coyoteTimer;

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
        UpdateUI();
    }

    void Update()
    {
        GroundCheck();
        HandleJump();
        HandleHealth();

        // Lock X – the world scrolls, not the player
        transform.position = new Vector3(0f, transform.position.y, 0f);
    }

    // ─── Ground & Jump ───────────────────────────────────────────────────────

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

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimer = 0f;
            // Squash-and-stretch feedback
            //transform.DOScaleY(0.8f, 0.1f).SetLoops(2, LoopType.Yoyo);
        }

        // Variable height – release space early for shorter jump
        if (Input.GetKey(KeyCode.Space) && rb.linearVelocity.y > 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * 0.5f * Time.deltaTime;
    }

    // ─── Health ──────────────────────────────────────────────────────────────

    void HandleHealth()
    {
        float ratio = GetRatio(); // forest:industry

        // t=0 → all industry (worst), t=1 → balanced or better
        float t = Mathf.Clamp01(ratio / balancedRatio);
        float drainMult = Mathf.Lerp(industryDrainMultiplier, 1f, t);

        // Always draining – multiplier controls speed
        currentHealth -= baseDrainRate * drainMult * Time.deltaTime;

        // Bonus regen only when clearly forest-heavy (ratio > balancedRatio)
        if (ratio > balancedRatio)
        {
            // Scales gently: 5:1 = tiny regen, 10:1 = full regenRate
            float regenScale = Mathf.Clamp01((ratio - balancedRatio) / balancedRatio);
            currentHealth += regenRate * regenScale * Time.deltaTime;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f) Die();

        UpdateUI();
    }

    // ─── Orb Collection ──────────────────────────────────────────────────────

    /// <summary>Called by OrbPickup when player touches an orb.</summary>
    public void CollectOrb()
    {
        CurrentOrbs++;

        if (orbParticlePrefab)
            Instantiate(orbParticlePrefab, transform.position, Quaternion.identity);

        // Notify round manager – it decides if round is complete
        RoundManager.Instance?.OnOrbCollected(CurrentOrbs);

        UpdateUI();
    }

    /// <summary>Called by RoundManager after a build choice is made.</summary>
    public void ResetOrbCount()
    {
        CurrentOrbs = 0;
        UpdateUI();
    }

    // ─── UI ──────────────────────────────────────────────────────────────────

    void UpdateUI()
    {
        int target = RoundManager.Instance != null
            ? RoundManager.Instance.CurrentOrbTarget
            : 5;

        if (orbCounterText) orbCounterText.text = $"{CurrentOrbs} / {target}";
        if (healthText) healthText.text = $"HP  {Mathf.Round(currentHealth)}";
    }

    // ─── Death ───────────────────────────────────────────────────────────────

    public void Die()
    {
        Debug.Log("Player died – hook up GameOver screen here.");
        enabled = false;
        rb.simulated = false;
        // TODO: show GameOver canvas
    }
}
