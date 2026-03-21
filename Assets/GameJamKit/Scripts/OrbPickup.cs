using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OrbPickup : MonoBehaviour
{
    [SerializeField] private float destroyOffscreenX = -15f;
    private bool collected = false;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[OrbPickup] No Collider2D on orb prefab! Add a CircleCollider2D.");
            return;
        }
        col.isTrigger = true;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // Scroll left with the world
        transform.position += Vector3.left * GameManager.WorldSpeed * Time.deltaTime;

        // Missed by player — clean up
        if (transform.position.x < destroyOffscreenX)
            Destroy(gameObject);
    }

    // ─── Trigger: orb isTrigger=true, player isTrigger=false ─────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    // ─── Collision fallback: in case both are non-trigger ─────────────────────
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        Collect();
    }

    void Collect()
    {
        if (collected) return;
        collected = true;
        PlayerScript.Instance?.CollectOrb();
        Destroy(gameObject);
    }
}
