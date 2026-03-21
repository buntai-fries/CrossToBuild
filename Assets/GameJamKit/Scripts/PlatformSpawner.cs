using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    public static PlatformSpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject forestPlatformPrefab;
    [SerializeField] private GameObject industryPlatformPrefab;
    [SerializeField] private GameObject orbPrefab;

    [Header("Lane Heights (Y) — match your scene")]
    [SerializeField] private float laneLOW = -2.5f;
    [SerializeField] private float laneMID = -1.0f;
    [SerializeField] private float laneHIGH = 0.8f;

    [Header("Platform Width")]
    [SerializeField] private float platformWidthStart = 4.5f;
    [SerializeField] private float widthShrinkPerRound = 0.15f;
    [SerializeField] private float minPlatformWidth = 2f;

    [Header("Gap Width")]
    [SerializeField] private float gapWidthStart = 3f;
    [SerializeField] private float gapGrowPerRound = 0.2f;
    [SerializeField] private float maxGapWidth = 6.5f;
    [SerializeField] private float gapRandomVariance = 0.5f;

    [Header("Orb Chance On Platform")]
    [Range(0f, 1f)][SerializeField] private float forestOrbChance = 0.6f;
    [Range(0f, 1f)][SerializeField] private float industryOrbChance = 0.25f;
    [SerializeField] private float orbFloatHeight = 0.7f;

    // ─── Runtime ─────────────────────────────────────────────────────────────

    private Camera cam;
    private float nextSpawnX;
    private float prefabWidth;
    private int currentLane = 1; // Start MID for immediate variation

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (forestPlatformPrefab == null || industryPlatformPrefab == null)
        {
            Debug.LogError("[PlatformSpawner] Platform prefabs not assigned!");
            enabled = false;
            return;
        }

        cam = Camera.main;

        // ── Measure prefab width from live probe ──────────────────────────────
        var probe = Instantiate(forestPlatformPrefab,
                        new Vector3(-9999f, -9999f, 0f), Quaternion.identity);
        var sr = probe.GetComponent<SpriteRenderer>();
        prefabWidth = (sr != null && sr.bounds.size.x > 0.01f) ? sr.bounds.size.x : 1f;
        Destroy(probe);

        // ── Start spawning cursor at right edge of screen ─────────────────────
        // Platforms immediately start appearing from off-screen right
        nextSpawnX = cam.transform.position.x
                   + cam.orthographicSize * cam.aspect
                   + 2f; // 2 units past right edge so first platform is hidden until it scrolls in

        // Randomise starting lane so first real platform isn't always MID
        currentLane = Random.Range(0, 3);

        Debug.Log($"[PlatformSpawner] Ready — prefabWidth={prefabWidth:F2}  " +
                  $"nextSpawnX={nextSpawnX:F1}  startLane={currentLane}");
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // ── Scroll cursor left with the world ─────────────────────────────────
        nextSpawnX -= GameManager.WorldSpeed * Time.deltaTime;

        // ── Spawn when cursor approaches right edge ───────────────────────────
        float screenRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;

        // Use while so if somehow cursor falls behind, it catches up immediately
        while (nextSpawnX < screenRight + 5f)
            SpawnPlatform();
    }

    // ─── Spawn ────────────────────────────────────────────────────────────────

    void SpawnPlatform()
    {
        int round = RoundManager.Instance != null
            ? RoundManager.Instance.CurrentRound : 1;

        // ── Sizes ─────────────────────────────────────────────────────────────
        float platformWidth = Mathf.Max(
            minPlatformWidth,
            platformWidthStart - (round - 1) * widthShrinkPerRound);

        float gap = Mathf.Max(0.5f, Mathf.Min(
            maxGapWidth,
            gapWidthStart + (round - 1) * gapGrowPerRound
            + Random.Range(-gapRandomVariance, gapRandomVariance)));

        // ── Lane ──────────────────────────────────────────────────────────────
        currentLane = PickNextLane(currentLane, round);
        float spawnY = LaneToY(currentLane);

        // ── Biome ─────────────────────────────────────────────────────────────
        BiomeType biome = ParallaxScript.Instance != null
            ? ParallaxScript.Instance.GetBiomeAt(nextSpawnX)
            : BiomeType.Forest;

        float biomeWidthMod = biome == BiomeType.Forest ? 0.3f : -0.3f;
        platformWidth = Mathf.Max(minPlatformWidth, platformWidth + biomeWidthMod);

        GameObject prefab = biome == BiomeType.Forest
            ? forestPlatformPrefab : industryPlatformPrefab;

        // ── Instantiate ───────────────────────────────────────────────────────
        float centreX = nextSpawnX + platformWidth * 0.5f;

        var platform = Instantiate(prefab,
                           new Vector3(centreX, spawnY, 0f),
                           Quaternion.identity);

        platform.transform.localScale = new Vector3(
            platformWidth / prefabWidth,
            platform.transform.localScale.y,
            1f);

        // ── Orb on platform ───────────────────────────────────────────────────
        if (orbPrefab != null)
        {
            float orbChance = biome == BiomeType.Forest
                ? forestOrbChance : industryOrbChance;

            if (currentLane == 2)
                orbChance = Mathf.Min(1f, orbChance + 0.2f); // High lane bonus

            if (Random.value < orbChance)
            {
                float offsetX = Random.Range(-platformWidth * 0.25f, platformWidth * 0.25f);
                Instantiate(orbPrefab,
                    new Vector3(centreX + offsetX, spawnY + orbFloatHeight, 0f),
                    Quaternion.identity);
            }
        }

        // ── Advance cursor ────────────────────────────────────────────────────
        nextSpawnX += platformWidth + gap;

        Debug.Log($"[PlatformSpawner] Platform — lane={currentLane}  " +
                  $"y={spawnY:F1}  width={platformWidth:F1}  gap={gap:F1}  " +
                  $"biome={biome}  round={round}");
    }

    // ─── Lane Logic ───────────────────────────────────────────────────────────

    int PickNextLane(int lane, int round)
    {
        float highW = Mathf.Clamp(0.15f + (round - 1) * 0.03f, 0.15f, 0.50f);
        float lowW = Mathf.Clamp(0.40f - (round - 1) * 0.02f, 0.15f, 0.40f);
        float midW = Mathf.Clamp(1f - highW - lowW, 0.1f, 0.5f);

        switch (lane)
        {
            case 0: // LOW → LOW or MID only
                return WeightedPick(new[] { 0, 1 }, new[] { lowW, midW + highW });
            case 1: // MID → any
                return WeightedPick(new[] { 0, 1, 2 }, new[] { lowW, midW, highW });
            case 2: // HIGH → MID or HIGH only
                return WeightedPick(new[] { 1, 2 }, new[] { midW + lowW, highW });
            default:
                return 1;
        }
    }

    int WeightedPick(int[] options, float[] weights)
    {
        float total = 0f;
        foreach (float w in weights) total += w;
        float roll = Random.value * total;
        float cum = 0f;
        for (int i = 0; i < options.Length; i++)
        {
            cum += weights[i];
            if (roll <= cum) return options[i];
        }
        return options[options.Length - 1];
    }

    float LaneToY(int lane)
    {
        switch (lane)
        {
            case 0: return laneLOW;
            case 1: return laneMID;
            case 2: return laneHIGH;
            default: return laneMID;
        }
    }
}
