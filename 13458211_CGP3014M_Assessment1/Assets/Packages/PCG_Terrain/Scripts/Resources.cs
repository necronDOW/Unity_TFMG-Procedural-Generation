using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Resource
{
    public enum Type
    {
        Iron = 90,
        Marble = 50,
        Wood = 30,
    }

    public int seed = 0;
    public Vector3 position;
    public Vector2 coords;
    public int weighting
    {
        get { return ((int)type + heightWeighting + varianceWeighting); }
        private set { }
    }

    public Type type;
    private int heightWeighting = 0;
    private int varianceWeighting = 0;

    public Resource(Vector3[] vertices, int seed, int chunkSize, float minHeight, float mountainHeight)
    {
        this.seed = seed;
        int index = seed;
        
        for(int i = 0; i < chunkSize; i++)
        {
            index = new System.Random(index).Next(0, chunkSize);
            index += new System.Random(index).Next(0, chunkSize) * chunkSize;

            if (vertices[index].y > minHeight)
            {
                position = vertices[index];
                heightWeighting = Mathf.Clamp(100 - (int)Mathf.Abs((vertices[index].y - minHeight)), 0, 100);
                varianceWeighting = Mathf.Clamp(100 - VarianceY(vertices, index, chunkSize, 10), 0, 100);
                type = GetType(vertices[index].y, minHeight, mountainHeight);
                break;
            }
        }
    }

    private int VarianceY(Vector3[] vertices, int index, int chunkSize, int radius)
    {
        float min = Mathf.Infinity, max = -Mathf.Infinity;
        int x = (index % chunkSize) - radius;
        int y = (index / chunkSize) - radius;

        int xLimit = x + radius + ((x < 0) ? x : ((x + radius > chunkSize) ? chunkSize - x : 0));
        x = Mathf.Clamp(x, 0, chunkSize);

        int yLimit = y + radius + ((y < 0) ? y : ((y + radius > chunkSize) ? chunkSize - y : 0));
        y = Mathf.Clamp(y, 0, chunkSize);

        MinMax(vertices[(y * chunkSize) + x].y, ref min, ref max);
        for (int i = x+1; i < xLimit; i++)
        {
            for (int j = y+1; j < yLimit; j++)
                MinMax(vertices[(j * chunkSize) + i].y, ref min, ref max);
        }

        coords = new Vector2(x, y);
        return (int)(max - min);
    }

    private void MinMax(float value, ref float min, ref float max)
    {
        if (value < min) min = value;
        if (value > max) max = value;
    }

    private Type GetType(float height, float minHeight, float mountainHeight)
    {
        float heightDifference = mountainHeight - minHeight;

        if (height > mountainHeight)
            return Type.Iron;
        else if (height > minHeight + (heightDifference * 0.5f))
            return Type.Marble;
        else return Type.Wood;
    }
}