using UnityEngine;

/// <summary>
/// Camera follows the player's Y position only.
/// X stays fixed (world scrolls, player and camera don't move horizontally).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Drag Player here

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 6f;
    [SerializeField] private float yOffset = 1f;   // Camera sits slightly above player centre
    [SerializeField] private float minY = 0f;   // Don't dip below the ground level

    void LateUpdate()
    {
        if (target == null) return;

        float desiredY = Mathf.Max(minY, target.position.y + yOffset);
        float smoothedY = Mathf.Lerp(transform.position.y, desiredY, smoothSpeed * Time.unscaledDeltaTime);

        // Keep X and Z locked
        transform.position = new Vector3(
            transform.position.x,
            smoothedY,
            transform.position.z
        );
    }
}
