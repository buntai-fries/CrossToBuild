using UnityEngine;

public class DeathFloor : MonoBehaviour
{
    void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[DeathFloor] No Collider2D found! Add a BoxCollider2D.");
            return;
        }
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[DeathFloor] Player fell! Triggering death.");
        PlayerScript.Instance?.Die();
    }
}
