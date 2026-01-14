using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    public static WorldManager instance;
    public Tilemap tilemap;
    public List<Tile> tiles; 
    WorldData worldData;
    public bool DEBUG_REGENERATE;
    
    void Awake()
    {
        instance = this;
        worldData = GetComponent<WorldData>();

        InitData();
    }

    void Update()
    {
        if (DEBUG_REGENERATE)
        {
            DEBUG_REGENERATE = false;

            InitData();
        }
    }

    void InitData()
    {
        worldData.worldSeed = Mathf.FloorToInt(Random.value * int.MaxValue);
        Random.InitState(worldData.worldSeed);

        worldData.noiseOffset = Random.Range(-100000, 100000f);

        GenerateHeightMap();
    }

    float[,] GenerateHeightMap()
    {
        Vector2Int size = worldData.gridSize;
        
        float[,] heightGrid = new float[size.x,size.y];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int position = new(x, y);
                heightGrid[x, y] = GetHeightAt(position);

                int tileIndex = 0;
                if (heightGrid[x, y] > 0.5f) tileIndex = 1;
                tilemap.SetTile((Vector3Int)position, tiles[tileIndex]);
            }
        }

        return heightGrid;
    }

    float GetHeightAt(Vector2Int position)
    {
        float height;
        
        height = GetNoise(position, 3);
        float distance = GetCenterDistance(position);

        height = Mathf.Lerp(height, 1-distance, worldData.centerHeightFocus);

        return height;
    }

    float GetNoise(Vector2Int position, int octaves)
    {
        float x = position.x * worldData.noiseScale + worldData.noiseOffset;
        float y = position.y * worldData.noiseScale + worldData.noiseOffset;
        float noise = 0;

        if (IsInBorders(position)){
            noise = Mathf.PerlinNoise(x, y);

            for (int i = 1; i < octaves; i++)
            {
                int power = (int)Mathf.Pow(2, i);
                noise += 1f/power * Mathf.PerlinNoise(x*power, y*power);
            }

            noise /= octaves/Mathf.Sqrt(2f);
        }


        return Mathf.Clamp01(noise);
    }

    float GetCenterDistance(Vector2Int position)
    {
        float distance;
        float xDist = 2f * position.x / (worldData.gridSize.x + worldData.borderSize) - 1;
        float yDist = 2f * position.y / (worldData.gridSize.y + worldData.borderSize) - 1;

        distance = Mathf.Sqrt(xDist*xDist + yDist*yDist) / Mathf.Sqrt(2);

        return distance;
    }

    bool IsInBorders(Vector2Int position)
    {
        // Checks if the given tile is within the map borders
        int x = position.x;
        int y = position.y;

        if (x < worldData.borderSize || x > worldData.gridSize.x - worldData.borderSize || y < worldData.borderSize || y > worldData.gridSize.y - worldData.borderSize) return false;
        return true;
    }
}
