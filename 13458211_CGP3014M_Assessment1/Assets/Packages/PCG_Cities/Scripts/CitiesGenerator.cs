using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CitiesGenerator : MonoBehaviour
{
    public int frequency = 1;
    public float upperHeightCap, lowerHeightCap;
    public float acceptableVariance;

    Dictionary<Vector2, CityChunk> cityChunkDictionary = new Dictionary<Vector2, CityChunk>();

    public void AddNewChunk(Vector2 coord, MapData chunkMapData)
    {
        cityChunkDictionary.Add(coord, new CityChunk());
    }

    private Vector2[] EvaluateData(float[,] data, int divisionCount)
    {
        Vector2[] suitableChunks = new Vector2[frequency];
        int areaSize = data.GetLength(0) / divisionCount;

        for (int x = 0; x < divisionCount; x++)
        {
            for (int y = 0; y < divisionCount; y++)
            {
                int evaluateX = x * areaSize;
                int evaluateY = y * areaSize;
            }
        }
    }

    private bool EvaluateHeight(float[,] data, int x, int y, int areaSize)
    {
        int halfArea = areaSize / 2;
        float value = data[x + halfArea, y + halfArea];

        if (value < upperHeightCap && value > lowerHeightCap)
            return true;
        else return false;
    }

    private float EvaluateHeightVariance(float[,] data, int x, int y, int areaSize)
    {

    }

    public class CityChunk
    {
        Vector2 chunkCoord;
    }
}
