using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a pool of ground tiles scrolling left endlessly.
/// When the leftmost tile exits camera left, it jumps to the right of the rightmost.
/// No Destroy/Instantiate at runtime – pure repositioning = zero GC.
/// </summary>
public class GroundManager : MonoBehaviour
{
    [Header("Ground Tile")]
    [SerializeField] private GameObject groundTilePrefab;

    [Header("Layout")]
    [SerializeField] private float yPosition = -3f;  // Vertical position of ground
    [SerializeField] private int extraTiles = 2;    // Extra tiles beyond screen width

    // ─── Runtime ─────────────────────────────────────────────────────────────

    private readonly List<Transform> tiles = new();
    private float tileWidth;
    private Camera cam;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        cam = Camera.main;

        // Instantiate one tile temporarily to measure its width
        GameObject probe = Instantiate(groundTilePrefab);
        var sr = probe.GetComponent<SpriteRenderer>();
        tileWidth = sr ? sr.bounds.size.x : 10f;
        Destroy(probe);

        // How many tiles to cover full screen + buffer
        float screenWidth = cam.orthographicSize * cam.aspect * 2f;
        int count = Mathf.CeilToInt(screenWidth / tileWidth) + extraTiles;

        // Spawn tiles in a continuous horizontal line
        // Start slightly left of screen so there's no gap on the left edge
        float startX = cam.transform.position.x - screenWidth * 0.5f - tileWidth;

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * tileWidth;
            var tile = Instantiate(groundTilePrefab,
                             new Vector3(x, yPosition, 0f),
                             Quaternion.identity, transform);
            tiles.Add(tile.transform);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Respect build-choice pause

        float dx = GameManager.WorldSpeed * Time.deltaTime;

        // Scroll all tiles left
        foreach (var t in tiles)
            t.position += Vector3.left * dx;

        // Recycle the leftmost tile
        Recycle();
    }

    // ─────────────────────────────────────────────────────────────────────────

    void Recycle()
    {
        Transform leftmost = tiles[0];
        Transform rightmost = tiles[tiles.Count - 1];

        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        // Right edge of the leftmost tile
        float tileRight = leftmost.position.x + tileWidth * 0.5f;

        if (tileRight < camLeft - tileWidth)
        {
            // Snap it to just after the rightmost tile
            leftmost.position = new Vector3(
                rightmost.position.x + tileWidth, yPosition, 0f);

            // Maintain sorted order in our list
            tiles.RemoveAt(0);
            tiles.Add(leftmost);
        }
    }
}
