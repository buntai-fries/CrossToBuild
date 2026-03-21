using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns orbs off the right edge of the screen.
/// 
/// BIOME AWARENESS:
///   Queries ParallaxScript.GetBiomeAt(spawnX) to find what biome is
///   at the spawn point. Forest → high orb chance. Industry → low.
///
/// HEIGHT AWARENESS:
///   Orbs spawn at ground level OR at registered platform heights.
///   Platforms call SpawnManager.RegisterPlatform / UnregisterPlatform
///   when they enter/exit the scene. SpawnManager picks a random
///   available height each time an orb spawns.
///
/// All spawned orbs scroll left via OrbPickup.cs (already written).
/// </summary>
public class SpawnerScript : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────
    public static SpawnerScript Instance { get; private set; }

    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("Orb Prefab")]
    [SerializeField] private GameObject orbPrefab;

    [Header("Spawn X")]
    [Tooltip("How far past the right camera edge orbs spawn. 1–2 units works well.")]
    [SerializeField] private float spawnXPadding = 1.5f;

    [Header("Orb Chances by Biome")]
    [Range(0f, 1f)]
    [SerializeField] private float forestOrbChance = 0.60f; // 60 % chance per interval
    [Range(0f, 1f)]
    [SerializeField] private float industryOrbChance = 0.20f; // 20 % chance per interval

    [Header("Spawn Interval")]
    [Tooltip("Seconds between each spawn attempt.")]
    [SerializeField] private float spawnInterval = 1.2f;

    [Header("Heights")]
    [Tooltip("Y position of the ground. Orbs can always spawn here.")]
    [SerializeField] private float groundY = -2.2f;   // Match your ground tile Y + tile half-height

    [Tooltip("Extra fixed heights before platforms register dynamically (optional). " +
             "You can pre-fill this with test platform heights in the inspector.")]
    [SerializeField] private float[] staticPlatformHeights;

    // ─── Private ─────────────────────────────────────────────────────────────

    private Camera cam;
    private float timer;

    /// <summary>
    /// Dynamic platform Y positions.
    /// Platform scripts call RegisterPlatform / UnregisterPlatform.
    /// </summary>
    private readonly List<float> platformYs = new();

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        cam = Camera.main;

        // Pre-load any static test heights you set in the inspector
        if (staticPlatformHeights != null)
            foreach (float h in staticPlatformHeights)
                platformYs.Add(h);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Paused during build choice

        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        TrySpawnOrb();
    }

    // ─── Orb Spawning ────────────────────────────────────────────────────────

    void TrySpawnOrb()
    {
        // Spawn X = just off the right edge of the camera
        float spawnX = cam.transform.position.x
                     + cam.orthographicSize * cam.aspect
                     + spawnXPadding;

        // Ask parallax what biome is visible at spawnX
        BiomeType biome = ParallaxScript.Instance != null
            ? ParallaxScript.Instance.GetBiomeAt(spawnX)
            : BiomeType.Forest;

        float chance = biome == BiomeType.Forest ? forestOrbChance : industryOrbChance;

        if (Random.value > chance) return; // Roll failed – no orb this interval

        // Pick a spawn height: ground OR one of the registered platform heights
        float spawnY = PickSpawnHeight();

        GameObject orb = Instantiate(orbPrefab,
            new Vector3(spawnX, spawnY, 0f),
            Quaternion.identity);

        // Optional: log for debugging
        Debug.Log($"[SpawnManager] Orb spawned at ({spawnX:F1}, {spawnY:F1}) " +
                  $"– biome: {biome}, chance was {chance * 100f:F0}%");
    }

    // ─── Height Selection ────────────────────────────────────────────────────

    float PickSpawnHeight()
    {
        // Build a combined list: ground + all live platform heights
        // We do this inline to avoid allocations; list is short.
        int totalOptions = 1 + platformYs.Count; // 1 = ground
        int index = Random.Range(0, totalOptions);

        if (index == 0)
            return groundY + 0.5f; // Slightly above ground so orb floats visibly

        return platformYs[index - 1] + 0.5f; // Slightly above platform surface
    }

    // ─── Platform Registration API ───────────────────────────────────────────

    /// <summary>
    /// Call this from your platform script's Start() / OnEnable().
    /// Adds the platform's Y to the pool of possible orb heights.
    /// </summary>
    public void RegisterPlatform(float worldY)
    {
        if (!platformYs.Contains(worldY))
            platformYs.Add(worldY);
    }

    /// <summary>
    /// Call this from your platform script's OnDestroy() / OnDisable().
    /// Removes the platform's Y so orbs stop trying to spawn there.
    /// </summary>
    public void UnregisterPlatform(float worldY)
    {
        platformYs.Remove(worldY);
    }
}
