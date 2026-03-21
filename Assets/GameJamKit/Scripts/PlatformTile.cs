using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlatformTile : MonoBehaviour
{
    [SerializeField] private float destroyOffscreenX = -25f;

    void Start()
    {
        // Must be solid — player stands on it
        GetComponent<Collider2D>().isTrigger = false;

        // Layer must be Ground for Player's raycast to detect landing
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1)
            Debug.LogError("[PlatformTile] 'Ground' layer doesn't exist! " +
                           "Add it in Edit → Project Settings → Tags and Layers.");
        else
            gameObject.layer = groundLayer;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        transform.position += Vector3.left * GameManager.WorldSpeed * Time.deltaTime;

        if (transform.position.x < destroyOffscreenX)
            Destroy(gameObject);
    }
}
