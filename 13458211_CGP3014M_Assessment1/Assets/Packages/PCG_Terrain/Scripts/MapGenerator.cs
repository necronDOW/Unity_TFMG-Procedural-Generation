using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapData
{
    public float[,] heightMap;
    public ResourceData[,] resourceSegments;

    public MapData(float[,] heightMap, int segmentCount, float plainsHeight, float mountainsHeight, float preferedHeightVariance)
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
                    resourceSegments[i, j] = new ResourceData(heightMap, x, y, segmentSize, plainsHeight, mountainsHeight);
            }
        }
    }
}

public class ResourceData
{
    private enum ResourceWeighting
    {
        Iron = 70,
        Marble = 50,
        Wood = 40,
        None = 0
    }

    public int weighting
    {
        get { return (heightWeight + varianceWeight + (short)resourceWeight) / 3; }
        private set { }
    }

    public Vector3 center { get; private set; }

    private short heightWeight = 0;
    private short varianceWeight = 0;
    private ResourceWeighting resourceWeight = ResourceWeighting.None;
    
    public ResourceData(float[,] heightMap, int x, int y, int segmentSize, float plainsHeight, float mountainsHeight)
    {
        int halfSegmentSize = segmentSize / 2;
        float height = heightMap[x + halfSegmentSize, y + halfSegmentSize];

        int quartSegmentSize = halfSegmentSize / 2;
        center = new Vector3(x - quartSegmentSize, 30.0f, -y + quartSegmentSize);

        heightWeight = (short)((1 - Mathf.Abs(height - plainsHeight)) * 100);
        varianceWeight = (short)((1 - Variance(heightMap, x, y, segmentSize)) * 100);
        
        System.Random rand = new System.Random(Thread.CurrentThread.ManagedThreadId + x + y);
        if (height > mountainsHeight)
        {
            if (rand.Next(0, 5) == 0)
                resourceWeight = ResourceWeighting.Iron;
        }
        else
        {
            int chance = rand.Next(0, 20);
            if (chance > 15)
                resourceWeight = ResourceWeighting.Marble;
            else if (chance > 10)
                resourceWeight = ResourceWeighting.Wood;
        }
    }

    public string type
    {
        get
        {
            switch (resourceWeight)
            {
                case ResourceWeighting.Iron: return "Iron";
                case ResourceWeighting.Marble: return "Marble";
                case ResourceWeighting.Wood: return "Wood";
                default: return "None";
            }
        }
        private set { }
    }

    private float Variance(float[,] heightMap, int x, int y, int segmentSize)
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
        return new MapData(noiseMap, resourceSegments, plainsHeight, mountainsHeight, preferedHeightVariance);
    }
}