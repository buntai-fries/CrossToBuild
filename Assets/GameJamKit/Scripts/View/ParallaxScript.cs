using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scrolls background segments left endlessly.
/// Uses REPOSITION (not Destroy/Instantiate) so there are zero
/// MissingReferenceExceptions — the same objects loop forever.
/// </summary>
public class ParallaxScript : MonoBehaviour
{
    public static ParallaxScript Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private BackgroundSegment forestPrefab;
    [SerializeField] private BackgroundSegment industryPrefab;

    [Header("Layout")]
    [SerializeField] private float yPosition = 0f;

    // ─── Runtime ─────────────────────────────────────────────────────────────

    // Sorted left→right at all times
    private readonly List<BackgroundSegment> active = new();
    private float segmentWidth;
    private Camera cam;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (forestPrefab == null || industryPrefab == null)
        {
            Debug.LogError("[ParallaxScript] Prefabs not assigned in inspector!");
            enabled = false;
            return;
        }

        cam = Camera.main;

        // ── Measure width via a probe instantiation ───────────────────────────
        // Instantiate off-screen so it's never visible during measurement
        var probe = Instantiate(forestPrefab,
            new Vector3(-9999f, yPosition, 0f), Quaternion.identity, transform);

        var sr = probe.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("[ParallaxScript] Forest prefab has no SpriteRenderer or no Sprite assigned!");
            Destroy(probe.gameObject);
            enabled = false;
            return;
        }

        segmentWidth = sr.bounds.size.x;

        if (segmentWidth <= 0.01f)
        {
            Debug.LogError($"[ParallaxScript] segmentWidth={segmentWidth} — sprite bounds returned zero. " +
                           "Check your sprite's Pixels Per Unit and that the prefab has a Sprite assigned.");
            Destroy(probe.gameObject);
            enabled = false;
            return;
        }

        // Move probe into position as segment 0 and keep it
        probe.transform.position = new Vector3(0f, yPosition, 0f);
        probe.width = segmentWidth;
        active.Add(probe);

        // ── Spawn enough segments to cover full screen + 2 buffer ─────────────
        float screenWidth = cam.orthographicSize * cam.aspect * 2f;
        int needed = Mathf.Clamp(Mathf.CeilToInt(screenWidth / segmentWidth) + 2, 2, 20);

        Debug.Log($"[ParallaxScript] segmentWidth={segmentWidth:F2}  " +
                  $"screenWidth={screenWidth:F2}  totalSegments={needed}");

        for (int i = 1; i < needed; i++)
            SpawnNew(i * segmentWidth);
    }

    void Update()
    {
        if (active.Count == 0) return;
        if (Time.timeScale == 0f) return;

        float dx = GameManager.WorldSpeed * Time.deltaTime;

        // ── Scroll every segment left ─────────────────────────────────────────
        foreach (var seg in active)
        {
            // Null check — safety net, should never fire with reposition approach
            if (seg == null) continue;
            seg.transform.position += Vector3.left * dx;
        }

        // ── Recycle: move leftmost to the right of rightmost ──────────────────
        Recycle();
    }

    // ─── Recycling (reposition, never Destroy) ────────────────────────────────

    void Recycle()
    {
        BackgroundSegment leftmost = active[0];
        BackgroundSegment rightmost = active[active.Count - 1];

        if (leftmost == null || rightmost == null) return;

        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float segRight = leftmost.transform.position.x + segmentWidth * 0.5f;

        // Fully past the left edge of the camera?
        if (segRight < camLeft - segmentWidth * 0.5f)
        {
            // Pick a new biome for this recycled segment
            SwapBiome(leftmost);

            // Reposition it to the right of the rightmost segment
            leftmost.transform.position = new Vector3(
                rightmost.transform.position.x + segmentWidth,
                yPosition, 0f);

            // Maintain sorted order: move it from front to back of list
            active.RemoveAt(0);
            active.Add(leftmost);
        }
    }

    /// <summary>
    /// Swaps the sprite on a recycled segment to match the newly chosen biome.
    /// This way we reuse the same GameObjects but visually change the biome.
    /// </summary>
    void SwapBiome(BackgroundSegment seg)
    {
        BiomeType newType = ChooseBiome();
        seg.biomeType = newType;

        BackgroundSegment sourcePrefab = newType == BiomeType.Forest
            ? forestPrefab : industryPrefab;

        // Copy the sprite from the prefab's SpriteRenderer
        var targetSR = seg.GetComponent<SpriteRenderer>();
        var sourceSR = sourcePrefab.GetComponent<SpriteRenderer>();
        if (targetSR != null && sourceSR != null)
            targetSR.sprite = sourceSR.sprite;
    }

    // ─── Initial Spawn (only called in Start) ────────────────────────────────

    void SpawnNew(float x)
    {
        BiomeType type = ChooseBiome();
        var prefab = type == BiomeType.Forest ? forestPrefab : industryPrefab;
        var seg = Instantiate(prefab,
                               new Vector3(x, yPosition, 0f),
                               Quaternion.identity, transform);
        seg.width = segmentWidth;
        active.Add(seg);
    }

    // ─── Biome Selection ─────────────────────────────────────────────────────

    BiomeType ChooseBiome()
    {
        var (forestBuilt, industryBuilt) = Player.GetBiomeCounts();
        int total = forestBuilt + industryBuilt;
        if (total == 0) return BiomeType.Forest;

        float forestChance = Mathf.Clamp((float)forestBuilt / total, 0.1f, 0.9f);
        return Random.value < forestChance ? BiomeType.Forest : BiomeType.Industry;
    }

    // ─── Public API for SpawnManager ─────────────────────────────────────────

    public BiomeType GetBiomeAt(float worldX)
    {
        float halfW = segmentWidth * 0.5f;
        foreach (var seg in active)
        {
            if (seg == null) continue;
            float left = seg.transform.position.x - halfW;
            float right = seg.transform.position.x + halfW;
            if (worldX >= left && worldX <= right)
                return seg.biomeType;
        }
        return BiomeType.Forest;
    }
}
