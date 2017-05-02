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

    private void SegmentData(float[,] data, int segmentCount)
    {
        float[] suitableChunks = new float[segmentCount * segmentCount];
        int dataSize = data.GetLength(0);
        int segmentSize = dataSize / segmentCount;
        int halfSegmentSize = segmentSize / 2;

        for (int i = 0; i < segmentCount; i++)
        {
            for (int j = 0; j < segmentCount; j++)
            {
                int x = i * segmentSize, y = j * segmentSize;
                int suitableIndex = (j * segmentCount) + i;
                float midValue = data[x + halfSegmentSize, y + halfSegmentSize];
                float variance = ChunkVariance(data, x, y, segmentSize);

                if (midValue < upperHeightCap && midValue > lowerHeightCap && variance < acceptableVariance)
                    suitableChunks[suitableIndex] = variance;
            }
        }

        for (int i = 0; i < frequency; i++)
        {
            int mostSuitable = SuitableIndex(suitableChunks);
        }
    }

    private float ChunkVariance(float[,] data, int x, int y, int searchSize)
    {
        float min = Mathf.Infinity;
        float max = -Mathf.Infinity;

        for (int i = x; i < x + searchSize; i++)
        {
            for (int j = y; j < y + searchSize; j++)
                MinMax(data[i, j], ref min, ref max);
        }

        return max - min;
    }

    private void MinMax(float value, ref float min, ref float max)
    {
        if (value < min) min = value;
        else if (value > max) max = value;
    }

    private int SuitableIndex(float[] values)
    {
        int current = 0;

        for (int i = 1; i < values.Length; i++)
        {
            if (values[current] == 0 || values[i] < values[current])
                current = i;
        }

        return current;
    }

    public class CityChunk
    {
        Vector2 chunkCoord;
    }
}
