using UnityEngine;

/// <summary>
/// Attach to your Orb prefab.
/// The orb scrolls left with the world and is collected on Player touch.
/// SpawnManager already places orbs at height-varied positions.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OrbPickup : MonoBehaviour
{
    [SerializeField] private float destroyOffscreenX = -15f; // Despawn if missed

    void Start()
    {
        // Make sure the collider is a trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // Scroll with the world (same speed as ground & parallax)
        transform.position += Vector3.left * GameManager.WorldSpeed * Time.deltaTime;

        // Clean up if player missed it and it's far off-screen
        if (transform.position.x < destroyOffscreenX)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player.Instance?.CollectOrb();
        Destroy(gameObject);
    }
}
