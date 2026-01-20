using UnityEngine;

public class WorldData : MonoBehaviour
{
    public static WorldData instance;
    [Header("Seed:")]
    public int worldSeed;
    [Header("Settings:")]
    public Vector2Int gridSize;
    public int borderSize;
    public float islandSizeCoefficient;
    public float noiseScale;
    public float centerHeightFocus;
    public float waterHeight = 0;
    public float mountainS1Height = 0;
    public float mountainS2Height = 0;
    public float mountainS3Height = 0;
    public float mountainS4Height = 0;
    public int riverAmount = 0;
    public float riverSourceMinDistance = 0;
    public float riverMaxLenght = 0;
    public float riverUphillPenalty = 0;
    public float riverUphillPenaltyThreshold = 0;
    [Header("Generated on creation:")]
    public float noiseOffset;
    public float islandStretchCoefficient;
    public float[,] heightMap;

    void Awake()
    {
        instance = this;
    }
}
