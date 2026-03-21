using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Orb Prefab")]
    [SerializeField] private GameObject orbPrefab;

    [Header("Spawn Timing")]
    [Tooltip("Seconds between spawn attempts. Recommended: 2")]
    [SerializeField] private float spawnInterval = 2f;

    [Header("Max Orbs On Screen At Once")]
    [Tooltip("Hard cap to prevent clusters. Recommended: 3")]
    [SerializeField] private int maxOrbsAlive = 3;

    [Header("Orb Chances (0 to 1)")]
    [Range(0f, 1f)]
    [SerializeField] private float forestOrbChance = 0.65f;
    [Range(0f, 1f)]
    [SerializeField] private float industryOrbChance = 0.25f;

    [Header("Spawn Position")]
    [Tooltip("Extra units past the right camera edge. 1.5 is safe.")]
    [SerializeField] private float spawnXPadding = 1.5f;

    [Tooltip("Y of the ground surface TOP. " +
             "Select Ground tilemap → check Y in Transform → add 0.5. " +
             "Example: ground Transform Y = -3 → set this to -2.5")]
    [SerializeField] private float groundY = -2f;

    [Tooltip("How high above ground/platform the orb floats.")]
    [SerializeField] private float orbFloatHeight = 0.6f;

    [Tooltip("Optional fixed platform heights before dynamic platforms exist.")]
    [SerializeField] private float[] staticPlatformHeights;

    // ─── Runtime ─────────────────────────────────────────────────────────────

    private Camera cam;
    private float timer;
    private float safeInterval;
    private readonly List<GameObject> liveOrbs = new();
    private readonly List<float> platformYs = new();

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // ── Null checks ───────────────────────────────────────────────────────
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[SpawnManager] Camera.main not found! " +
                           "Make sure Main Camera tag is set to 'MainCamera'.");
            enabled = false;
            return;
        }

        if (orbPrefab == null)
        {
            Debug.LogError("[SpawnManager] orbPrefab not assigned in inspector!");
            enabled = false;
            return;
        }

        // ── Enforce minimum interval ──────────────────────────────────────────
        safeInterval = Mathf.Max(spawnInterval, 1.5f);
        if (safeInterval != spawnInterval)
            Debug.LogWarning($"[SpawnManager] spawnInterval was {spawnInterval}s — " +
                             $"clamped to 1.5s minimum. Please set Spawn Interval = 2 in Inspector.");

        // Delay first spawn so player has time to settle
        timer = safeInterval * 0.5f;

        // ── Load static platform heights ──────────────────────────────────────
        if (staticPlatformHeights != null)
            foreach (float h in staticPlatformHeights)
                platformYs.Add(h);

        // ── Log spawn X so we can verify it's off-screen ──────────────────────
        float debugSpawnX = cam.transform.position.x
                          + cam.orthographicSize * cam.aspect
                          + spawnXPadding;

        Debug.Log($"[SpawnManager] Ready — " +
                  $"interval={safeInterval}s  " +
                  $"maxAlive={maxOrbsAlive}  " +
                  $"camX={cam.transform.position.x:F1}  " +
                  $"orthoSize={cam.orthographicSize:F1}  " +
                  $"aspect={cam.aspect:F2}  " +
                  $"spawnX={debugSpawnX:F1}  " +
                  $"groundY={groundY}");
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // Clean up orbs that were destroyed (collected or off-screen)
        liveOrbs.RemoveAll(o => o == null);

        timer += Time.deltaTime;
        if (timer < safeInterval) return;
        timer = 0f;

        TrySpawnOrb();
    }

    // ─── Spawning ─────────────────────────────────────────────────────────────

    void TrySpawnOrb()
    {
        // Hard cap — never spawn if too many orbs already exist
        if (liveOrbs.Count >= maxOrbsAlive)
        {
            Debug.Log($"[SpawnManager] Cap reached ({liveOrbs.Count}/{maxOrbsAlive}) — skip.");
            return;
        }

        // Always spawn relative to camera right edge
        // Camera X is fixed (camera doesn't move horizontally)
        // This correctly places orbs off-screen regardless of player position
        float spawnX = cam.transform.position.x
                     + cam.orthographicSize * cam.aspect
                     + spawnXPadding;

        // Ask parallax what biome is at spawnX
        BiomeType biome = ParallaxScript.Instance != null
            ? ParallaxScript.Instance.GetBiomeAt(spawnX)
            : BiomeType.Forest;

        float chance = biome == BiomeType.Forest ? forestOrbChance : industryOrbChance;

        // Roll for spawn
        if (Random.value > chance)
        {
            Debug.Log($"[SpawnManager] Roll failed (chance={chance * 100f:F0}%) — no orb.");
            return;
        }

        float spawnY = PickSpawnHeight();

        GameObject orb = Instantiate(orbPrefab,
            new Vector3(spawnX, spawnY, 0f),
            Quaternion.identity);

        liveOrbs.Add(orb);

        Debug.Log($"[SpawnManager] ✓ Orb at ({spawnX:F1}, {spawnY:F1})  " +
                  $"biome={biome}  alive={liveOrbs.Count}/{maxOrbsAlive}");
    }

    // ─── Height Selection ─────────────────────────────────────────────────────

    float PickSpawnHeight()
    {
        int total = 1 + platformYs.Count;
        int index = Random.Range(0, total);
        float baseY = index == 0 ? groundY : platformYs[index - 1];
        return baseY + orbFloatHeight;
    }

    // ─── Platform Registration (called by PlatformTile.cs) ───────────────────

    public void RegisterPlatform(float worldY)
    {
        if (!platformYs.Contains(worldY))
            platformYs.Add(worldY);
    }

    public void UnregisterPlatform(float worldY)
    {
        platformYs.Remove(worldY);
    }
}
