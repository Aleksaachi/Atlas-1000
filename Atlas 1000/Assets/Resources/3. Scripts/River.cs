using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;

public class River : MonoBehaviour
{
    LineRenderer lineRenderer;
    WorldData worldData;
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        worldData = WorldData.instance;
    }

    public void GenerateRiver(Vector2Int startPoint, Vector2Int endPoint)
    {
        List<Vector2Int> path = Dijstra(startPoint, endPoint);
        if (path.Count > 0)
        {
            lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++){
                lineRenderer.SetPosition(i, (Vector2)path[i]);
                WorldManager.instance.riverOccupancy[path[i].x, path[i].y] = true;
            }
        }
        else
        {
            Debug.Log("Destroying river: Unconnectable start and end points");
            Destroy(gameObject);
        }
    }

    List<Vector2Int> Dijstra(Vector2Int startPoint, Vector2Int endPoint)
    {
        float[,] dijkstraValues = new float[worldData.gridSize.x, worldData.gridSize.y];
        Vector2Int[,] entranceMarkers = new Vector2Int[worldData.gridSize.x, worldData.gridSize.y];
        List<Vector2Int> queue = new();
        queue.Add(startPoint);
        List<Vector2Int> path = new();
        List<Vector2Int> ignoreList = new();
        bool failed = false;
        while (!failed)
        {
            if (queue.Count == 0){failed = true; break;}

            float minValue = dijkstraValues[queue[0].x, queue[0].y];
            int index = 0;
            for (int i = 0; i < queue.Count; i++)
            {
                Vector2Int point = queue[i];
                float dv = dijkstraValues[point.x, point.y];
                if (dv < minValue)
                {
                    minValue = dv;
                    index = i;
                }
            }
            Vector2Int current = queue[index];
            queue.RemoveAt(index);
            ignoreList.Add(current);
            if (current != endPoint){
                List<Vector2Int> neighbors = WorldManager.GetNeighbors(current);
                foreach (Vector2Int neighbor in neighbors){
                    if (worldData.heightMap[neighbor.x, neighbor.y] <= worldData.waterHeight) continue;

                    float currentValue = dijkstraValues[neighbor.x, neighbor.y];
                    float newValue = GetWeight(current, neighbor);
                    if ((currentValue == 0f || currentValue > newValue) && !ignoreList.Contains(neighbor)){
                        dijkstraValues[neighbor.x, neighbor.y] = dijkstraValues[current.x, current.y] + newValue;
                        entranceMarkers[neighbor.x, neighbor.y] = current;
                        if (!queue.Contains(neighbor))
                        {
                            queue.Add(neighbor);
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        if (!failed){
            Vector2Int pathCurrent = endPoint;
            while (!path.Contains(startPoint))
            {
                path.Add(pathCurrent);
                if (WorldManager.instance.riverOccupancy[pathCurrent.x, pathCurrent.y])
                {
                    break;
                }
                pathCurrent = entranceMarkers[pathCurrent.x, pathCurrent.y];  
            }
        }

        return path;
    }

    float GetWeight(Vector2Int startPoint, Vector2Int endPoint)
    {
        float hs = worldData.heightMap[startPoint.x, startPoint.y];
        float he = worldData.heightMap[endPoint.x, endPoint.y];
        if (Mathf.Abs(hs - he) < worldData.riverUphillPenaltyThreshold) return 1;
        return Mathf.Max(hs, he) / Mathf.Min(hs, he) * worldData.riverUphillPenalty;
        
    }
}
