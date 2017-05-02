using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapData
{
    public float[,] heightMap;
    public ResourceData[,] resourceSegments;

    public MapData(float[,] heightMap, float scale, int segmentCount, float plainsHeight, float mountainsHeight, float preferedHeightVariance)
    {
        this.heightMap = heightMap;
        resourceSegments = new ResourceData[segmentCount, segmentCount];

        int heightMapSize = heightMap.GetLength(0);
        int segmentSize = heightMapSize / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            for (int j = 0; j < segmentCount; j++)
            {
                int x = i * segmentSize;
                int y = j * segmentSize;

                int halfSegmentSize = segmentSize / 2;
                if (heightMap[x + halfSegmentSize, y + halfSegmentSize] > plainsHeight)
                    resourceSegments[i, j] = ResourceDataGenerator.Generate(heightMap, x, y, segmentSize, plainsHeight, mountainsHeight);
            }
        }
    }
}

public class ResourceData
{
    public Vector3 center { get; private set; }
    public ResourceDataGenerator.ResourceWeighting type { get; private set; }
    public short weighting { get; private set; }

    public ResourceData(Vector3 center, int weighting, ResourceDataGenerator.ResourceWeighting type)
    {
        this.center = center;
        this.weighting = (short)weighting;
        this.type = type;
    }
}

public class ResourceDataGenerator
{
    public enum ResourceWeighting
    {
        Iron = 70,
        Marble = 50,
        Wood = 40,
        None = 0
    }

    public static ResourceData Generate(float[,] heightMap, int x, int y, int segmentSize, float plainsHeight, float mountainsHeight)
    {
        int halfSegmentSize = segmentSize / 2;
        int quartSegmentSize = halfSegmentSize / 2;
        float height = heightMap[x + halfSegmentSize, y + halfSegmentSize];

        Vector3 centralIndex = new Vector3(x + halfSegmentSize, 0, y + halfSegmentSize);
        int heightWeight = (short)((1 - Mathf.Abs(height - plainsHeight)) * 100);
        int varianceWeight = (short)((1 - Variance(heightMap, x, y, segmentSize)) * 100);
        ResourceWeighting resourceWeight = Resource(height, plainsHeight, mountainsHeight, new System.Random(Thread.CurrentThread.ManagedThreadId + x + y));

        return new ResourceData(centralIndex, (heightWeight + varianceWeight + (short)resourceWeight) / 3, resourceWeight);
    }

    private static ResourceWeighting Resource(float height, float plainsHeight, float mountainsHeight, System.Random rand)
    {
        ResourceWeighting rW = ResourceWeighting.None;

        if (height > mountainsHeight)
        {
            if (rand.Next(0, 5) == 0)
                rW =  ResourceWeighting.Iron;
        }
        else
        {
            int chance = rand.Next(0, 25);
            if (chance > 20)
                rW =  ResourceWeighting.Marble;
            else if (chance > 10)
                rW =  ResourceWeighting.Wood;
        }

        return rW;
    }

    private static float Variance(float[,] heightMap, int x, int y, int segmentSize)
    {
        float min = heightMap[x,y], max = heightMap[x, y];
        int xRange = x + segmentSize;
        int yRange = y + segmentSize;

        for (int i = x+1; i < xRange; i++)
        {
            for (int j = y+1; j < yRange; j++)
            {
                float value = heightMap[i, j];
                if (value < min) min = value;
                else if (value > max) max = value;
            }
        }

        return Mathf.Clamp01(max - min);
    }
}

public class MapGenerator : MonoBehaviour
{
    public int mapChunkSize = 239;
    public int borderedChunkSize;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;
    public Material terrainMaterial;

    float[,] falloffMap;

    [Range(1,16)]
    public int resourceSegments = 4;
    [Range(0,1)]
    public float plainsHeight;
    [Range(0,1)]
    public float mountainsHeight;
    [Range(0,1)]
    public float preferedHeightVariance = 0.1f;

    #region Threading
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(center, callback); };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center, resourceSegments, plainsHeight, mountainsHeight);

        lock (mapDataThreadInfoQueue)
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, lod, callback); };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightCurve, terrainData.meshHeightMultiplier, lod);

        lock (meshDataThreadInfoQueue)
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    private void Update()
    {
        for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
        {
            MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }

        for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
        {
            MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }
    }
    #endregion

    private void Awake()
    {
        borderedChunkSize = mapChunkSize + 2;
        
        if (terrainData.useFalloff)
            falloffMap = MaskLib.Falloff(borderedChunkSize);

        if (textureData != null)
            textureData.ApplyToMaterial(terrainMaterial);
    }

    public MapData GenerateMapData(Vector2 center, int resourceSegments, float preferedHeight, float preferedHeightVariance)
    {
        float[,] noiseMap = MaskLib.Perlin(borderedChunkSize, borderedChunkSize, noiseData.seed, noiseData.scale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, center + noiseData.offset);
        
        for (int y = 0; y < borderedChunkSize; y++)
        {
            for (int x = 0; x < borderedChunkSize; x++)
            {
                if (terrainData.useFalloff)
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
            }
        }

        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        return new MapData(noiseMap, terrainData.uniformScale, resourceSegments, plainsHeight, mountainsHeight, preferedHeightVariance);
    }

    public ResourceData[,] GenerateResources(Vector3[] vertices)
    {
        ResourceData[,] resourceData = new ResourceData[resourceSegments, resourceSegments];
        int segmentSize = mapChunkSize / resourceSegments;
        int halfSegmentSize = segmentSize / 2;

        for (int i = 0; i < resourceSegments; i++)
        {
            for (int j = 0; j < resourceSegments; j++)
            {
                int x = i * segmentSize;
                int y = j * segmentSize;
                int vIndex = (j * mapChunkSize) + i;
                int cIndex = vIndex + ((halfSegmentSize * mapChunkSize) + halfSegmentSize);

                float height = vertices[cIndex].y;
                int heightWeight = (int)((1 - Mathf.Abs(height - plainsHeight)) * 100);
                int varianceWeight = (int)((1 - Variance(vertices, x, y, segmentSize, mapChunkSize)) * 100);
                resourceData[i, j] = new ResourceData()
            }
        }

        ResourceWeighting resourceWeight = Resource(height, plainsHeight, mountainsHeight, new System.Random(Thread.CurrentThread.ManagedThreadId + x + y));

        return new ResourceData(centralIndex, (heightWeight + varianceWeight + (short)resourceWeight) / 3, resourceWeight);
    }

    private float Variance(Vector3[] vertices, int x, int y, int segmentSize, int mapChunkSize)
    {
        int index = Index1D(x, y, mapChunkSize);
        float min = vertices[index].y, max = vertices[index].y;
        int xRange = x + segmentSize;
        int yRange = y + segmentSize;

        for (int i = x + 1; i < xRange; i++)
        {
            for (int j = y + 1; j < yRange; j++)
            {
                float value = vertices[Index1D(i, j, mapChunkSize];
                if (value < min) min = value;
                else if (value > max) max = value;
            }
        }

        return Mathf.Clamp01(max - min);
    }

    private int Index1D(int x, int y, int size)
    {
        return (y * size) + x;
    }
}