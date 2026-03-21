using UnityEngine;

public enum BiomeType
{
    Forest,
    Industry
}

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundSegment : MonoBehaviour
{
    public BiomeType biomeType;

    [HideInInspector] public float width;

    void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        width = sr.bounds.size.x;
    }
}
