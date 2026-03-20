using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 5.0f;
    public float jumpForce = 10f;
    public float coyoteTime = 0.1f;

    [Header("Orb Collection")]
    public int maxOrbs = 10;
    public float magnetRange = 2f;
    public GameObject orbParticlePrefab; // Assign particle

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float regenRate = 1f; // Forest good
    public float drainRate = 0.5f; // Factory bad
    public float targetRatio = 1.5f; // Nature:industry ideal

    [Header("UI Refs")]
    public TextMeshProUGUI orbCounter;
    public TextMeshProUGUI healthBar; // Image.fillAmount script if Slider

    // Privates
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim; // Optional
    private int currentOrbs = 0;
    public static int natureCount = 0, industryCount = 0; // Global ratio
    private bool isGrounded;
    private float coyoteTimer;
    private float industryExcess; // Calc from builds

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        UpdateUI();

    }

    // Update is called once per frame
    void Update()
    {
        GroundCheck();
        HealthUpdate();
        // ...
        if (Keyboard.current.spaceKey.isPressed && rb.linearVelocity.y > 0)
        { // GetKey equiv
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * 0.5f * Time.deltaTime;
        }

    }

    void FixedUpdate()
    {
        // Auto-run right (endless style)
        rb.linearVelocity = new Vector2(runSpeed + (industryExcess * 0.2f),
                            rb.linearVelocity.y);
        FlipSprite();
    }

    void GroundCheck()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.6f,
                     LayerMask.GetMask("Ground"));
        coyoteTimer -= Time.deltaTime;
        if (isGrounded) coyoteTimer = coyoteTime;
    }

    void Jump(InputAction.CallbackContext context)
    { // New method, auto-called
        if (context.started && coyoteTimer > 0)
        { // "started" = GetKeyDown
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimer = 0;
            transform.DOScaleY(0.8f, 0.1f).SetLoops(2, LoopType.Yoyo);
        }
    }

    void FlipSprite()
    {
        sr.flipX = rb.linearVelocity.x < 0; // Rare left
    }

    void HealthUpdate()
    {
        float ratio = (float)natureCount / (industryCount + 1); // Avoid div0
        industryExcess = Mathf.Max(0, (2f - ratio)); // >2:1 bad

        if (ratio > targetRatio)
        {
            currentHealth += regenRate * Time.deltaTime;
        }

        else
        {
            currentHealth -= drainRate * industryExcess * Time.deltaTime;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0) Die();
        UpdateUI();
    }

    void UpdateUI()
    {
        orbCounter.text = $"{currentOrbs}/{maxOrbs}";
        healthBar.text = $"{Mathf.Round(currentHealth)}%";

    }

    public void CollectOrb()
    {
        if (currentOrbs < maxOrbs)
        {
            currentOrbs++;
            // Magnet future orbs in range
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, magnetRange,
                                  LayerMask.GetMask("Orb"));
            foreach (var orb in nearby)
            {
                orb.transform.DOMove(transform.position, 0.3f).OnComplete(() =>
                Destroy(orb.gameObject));
            }

            Instantiate(orbParticlePrefab, transform.position, Quaternion.identity);
            if (currentOrbs >= maxOrbs) TriggerBuildChoice();
            UpdateUI();

        }
    }
    void TriggerBuildChoice()
    {
        Time.timeScale = 0;

    }

    void Die()
    {
        // Fade/DOTween to dystopia scene
        DOTween.To(() => currentHealth, x => currentHealth = x, 0, 1f).OnComplete(() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene(0));

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Orb"))
        {
            CollectOrb();
            Destroy(other.gameObject);

        }
    }

}
