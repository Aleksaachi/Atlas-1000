using UnityEngine;

public class WorldData : MonoBehaviour
{
    [Header("Seed:")]
    public int worldSeed;
    [Header("Settings:")]
    public Vector2Int gridSize;
    public int borderSize;
    public float noiseScale;
    public float centerHeightFocus;
    public float islandStretchCoefficient;
    [Header("Generated on creation:")]
    public float noiseOffset;
    
}
