using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("World Scroll")]
    [SerializeField] private float baseScrollSpeed = 5f;

    /// <summary>All scrolling objects read this. Same speed for both biomes.</summary>
    public static float WorldSpeed { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        WorldSpeed = baseScrollSpeed;
    }
}
