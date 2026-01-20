using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Mathematics;
using System.Net;
using Unity.VisualScripting;

public class WorldManager : MonoBehaviour
{
    public static WorldManager instance;
    public Tilemap tilemapWorld;
    public Tilemap tilemapMountains;
    public List<Tile> tilesWorld; 
    public List<Tile> tilesMountains;
    public GameObject riverPrefab;
    List<River> riverList = new();
    WorldData worldData;
    public bool DEBUG_REGENERATE;
    public bool loadFromSeed;
    bool[,] eligibleMountainDecorSpots;
    public bool[,] riverOccupancy;
    List<Vector2Int> coastalTiles;
    
    void Awake()
    {
        instance = this;
        worldData = GetComponent<WorldData>();
        riverOccupancy = new bool[worldData.gridSize.x, worldData.gridSize.y];

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
        // Initializes and starts the map gen process
        if (!loadFromSeed) worldData.worldSeed = Mathf.FloorToInt(UnityEngine.Random.value * int.MaxValue);
        UnityEngine.Random.InitState(worldData.worldSeed);

        worldData.noiseOffset = UnityEngine.Random.Range(-100000, 100000f);
        worldData.islandStretchCoefficient = UnityEngine.Random.Range(0.5f, 1.5f);
        
        eligibleMountainDecorSpots = new bool[worldData.gridSize.x, worldData.gridSize.y];

        tilemapWorld.ClearAllTiles();
        tilemapMountains.ClearAllTiles();

        for (int x = 0; x < worldData.gridSize.x; x++)
        {
            for (int y = 0; y < worldData.gridSize.y; y++)
            {
                riverOccupancy[x, y] = false;
            }
        }

        for (int i = riverList.Count-1; i >= 0; i--)
        {
            Destroy(riverList[i].gameObject);
        }
        riverList.Clear();

        worldData.heightMap = GenerateHeightMap();
        worldData.heightMap = GenerateMountainsOnHeightMap();
        coastalTiles = GetAllCoastalTiles();
        PaintWorld();
        GenerateRivers(worldData.riverAmount);
    }

    float[,] GenerateHeightMap()
    {
        // Generates a matrix of heights for the map
        Vector2Int size = worldData.gridSize;
        
        float[,] heightGrid = new float[size.x,size.y];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int position = new(x, y);
                heightGrid[x, y] = GetHeightAt(position);
            }
        }

        return heightGrid;
    }

    float GetHeightAt(Vector2Int position)
    {
        // Get the height value of a given position
        float height;
        
        height = GetHeightNoise(position, 3);
        float distance = GetCenterDistance(position);

        height = Mathf.Lerp(height, 1-distance, worldData.centerHeightFocus);

        return height;
    }

    float GetHeightNoise(Vector2Int position, int octaves)
    {
        // Get noise with a certain amount of octaves in a random position
        float x = position.x * worldData.noiseScale + worldData.noiseOffset;
        float y = position.y * worldData.noiseScale + worldData.noiseOffset;
        float noise = 0;

        if (IsInBorders(position)){
            for (int i = 0; i < octaves; i++)
            {
                int power = (int)Mathf.Pow(2, i);
                noise += 1f/power * Mathf.PerlinNoise(x*power, y*power);
            }

            noise /= octaves/Mathf.Sqrt(octaves);
        }

        return Mathf.Clamp01(noise);
    }

    float GetCenterDistance(Vector2Int position)
    {
        // Get the distance to the center of the map
        float distance;
        float xDist = 2f * position.x / (worldData.gridSize.x + worldData.borderSize) - 1;
        float yDist = 2f * position.y / (worldData.gridSize.y + worldData.borderSize) - 1;

        if (worldData.islandStretchCoefficient > 1f) xDist *= worldData.islandStretchCoefficient;
        else                                         yDist /= worldData.islandStretchCoefficient;

        distance = Mathf.Sqrt(xDist*xDist + yDist*yDist) / worldData.islandSizeCoefficient;

        return distance;
    }

    public static bool IsInBorders(Vector2Int position)
    {
        // Checks if the given tile is within the map borders
        int x = position.x;
        int y = position.y;

        if (x < WorldData.instance.borderSize || x > WorldData.instance.gridSize.x - WorldData.instance.borderSize 
        ||  y < WorldData.instance.borderSize || y > WorldData.instance.gridSize.y - WorldData.instance.borderSize) return false;
        return true;
    }

    void PaintWorld()
    {   
        // Place tiles depending on height in all cells
        Vector2Int size = worldData.gridSize;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                
                Vector2Int position = new(x, y);
                int tileIndex = 0;
                if (IsInBorders(position)){
                    if (worldData.heightMap[x, y] > worldData.waterHeight) tileIndex = 1;

                    if (eligibleMountainDecorSpots[x, y])
                    {
                        float height = worldData.heightMap[x, y];
                        float rand = UnityEngine.Random.value;
                        if (rand < Mathf.Pow(height, 2f)){
                            int index = 4 * UnityEngine.Random.Range(0, 1);
                            

                            if (height > worldData.mountainS4Height){
                                index += 3;
                                ClearMountainEligibilitySlots(11, 7, position, 1-height);
                            }else if (height > worldData.mountainS3Height){
                                index += 2;
                                ClearMountainEligibilitySlots(7, 5, position, 1-height);
                            }else if (height > worldData.mountainS2Height){
                                index += 1;
                                ClearMountainEligibilitySlots(5, 3, position, 1-height);
                            }else{
                                index += 0;
                                ClearMountainEligibilitySlots(3, 3, position, 1-height);
                            }
     
                            tilemapMountains.SetTile((Vector3Int)position, tilesMountains[index]);
                        }
                    }
                }
                
                tilemapWorld.SetTile((Vector3Int)position, tilesWorld[tileIndex]);
            }
        }
    }
    float[,] GenerateMountainsOnHeightMap()
    {
        // Updates the height matrix of the map to account for mountains
        Vector2Int size = worldData.gridSize;

        float[,] heightMap = new float[size.x, size.y];

        for (int x = 0; x < size.x; x++)
        {
           for (int y = 0; y < size.y; y++)
            {
                Vector2Int position = new(x, y);
                float mountainHeight = GetMountainNoise(position, 2);

                if (worldData.heightMap[x, y] < worldData.mountainS1Height)
                {
                    mountainHeight = Mathf.Lerp(0, worldData.mountainS4Height, mountainHeight);
                }
                heightMap[x, y] = Mathf.Max(worldData.heightMap[x, y], mountainHeight);
                if (heightMap[x, y] >= worldData.mountainS1Height)
                {
                    eligibleMountainDecorSpots[x, y] = true;
                }

            } 
        }

        return heightMap;
    }

    void ClearMountainEligibilitySlots(int clearRangeX, int clearRangeY, Vector2Int position, float chanceToFail)
    {
        // Makes mountain eligibility spots from true to false in a square with clearRange side lenght. 
        // Use only even side lenghts I can't be bothered to make it work for evens too.
        int halfLenghtX = Mathf.FloorToInt(clearRangeX/2);
        int halfLenghtY = Mathf.FloorToInt(clearRangeY/2);
        for (int x = -halfLenghtX; x <= halfLenghtX; x++)
        {
            for (int y = -halfLenghtY; y <= halfLenghtY; y++)
            {
                int rx = x + position.x;
                int ry = y + position.y;
                if (rx < 0 || rx >= worldData.gridSize.x || ry < 0 || ry >= worldData.gridSize.y) continue;
                if (UnityEngine.Random.value < chanceToFail) continue;

                eligibleMountainDecorSpots[rx, ry] = false;
            }
        }
    }
    float GetMountainNoise(Vector2Int position, int octaves)
    {
        // Get noise with a certain amount of octaves in a random position
        float x = position.x * worldData.noiseScale*2 + worldData.noiseOffset;
        float y = position.y * worldData.noiseScale*2 + worldData.noiseOffset;
        float noise = 0;

        if (worldData.heightMap[position.x, position.y] > worldData.waterHeight){
            for (int i = 0; i < octaves; i++)
            {
                int power = (int)Mathf.Pow(2, i);
                float2 coordsAsFloats = new(x*power, y*power);
                float2 noiseAsF2 = Unity.Mathematics.noise.cellular(coordsAsFloats);
                noise += 1f/power * (noiseAsF2.x + noiseAsF2.y)/2;
            }
            noise /= octaves/Mathf.Sqrt(octaves);
            noise *= 2;
            noise += Mathf.PerlinNoise(x, y);
            noise /= 3;
        }

        return Mathf.Clamp01(noise);
    }

    void GenerateRivers(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2Int startPosition = Vector2Int.zero;
            bool foundStartPosition;
            int killSwitch = 0;
            do
            {
                killSwitch++;
                foundStartPosition = true;

                int x = UnityEngine.Random.Range(0, worldData.gridSize.x);
                int y = UnityEngine.Random.Range(0, worldData.gridSize.y);
                if (worldData.heightMap[x, y] < worldData.mountainS1Height){
                    foundStartPosition = false;
                    continue;
                }
                startPosition = new Vector2Int(x, y);

                for (int j = 0; j < riverList.Count; j++)
                {
                    if (Vector2.Distance((Vector2)startPosition, riverList[j].transform.position) < worldData.riverSourceMinDistance)
                    {
                        foundStartPosition = false;
                        break;
                    }
                }
                if (killSwitch > 1000) break;
            }while (!foundStartPosition);
            
            if (killSwitch > 1000) continue;

            Vector2Int endPosition = coastalTiles[0];
            killSwitch = 0;
            do
            {
                killSwitch++;
                if (killSwitch > 100) break;
                endPosition = coastalTiles[UnityEngine.Random.Range(0, coastalTiles.Count)];
            }while(Vector2.Distance(startPosition, endPosition) > worldData.riverMaxLenght);

            if (killSwitch > 100)
            {
                i--;
                continue;
            }

            River river = Instantiate(riverPrefab, (Vector2)startPosition, Quaternion.identity).GetComponent<River>();
            riverList.Add(river);

            river.GenerateRiver(endPosition, startPosition);
        }
    }

    public static List<Vector2Int> GetNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if ((x == 0 || y == 0) && x != y){
                    int rx = position.x + x;
                    int ry = position.y + y;
                    Vector2Int relativePosition = new Vector2Int(rx, ry);
                    if (!IsInBorders(relativePosition)) continue;
                    neighbors.Add(relativePosition);
                }
            }
        }

        return neighbors;
    }

    List<Vector2Int> GetAllCoastalTiles()
    {
        List<Vector2Int> coastalTiles = new();
        for (int x = 0; x < worldData.gridSize.x; x++)
        {
            for (int y = 0; y < worldData.gridSize.y; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                if (worldData.heightMap[x, y] > worldData.waterHeight)
                {
                    List<Vector2Int> neighbors = GetNeighbors(position);
                    foreach (Vector2Int neighbor in neighbors)
                    {
                        if (worldData.heightMap[neighbor.x, neighbor.y] <= worldData.waterHeight)
                        {
                            coastalTiles.Add(position);
                            break;
                        }
                    }
                }
            }
        }
        return coastalTiles;
    }
}
