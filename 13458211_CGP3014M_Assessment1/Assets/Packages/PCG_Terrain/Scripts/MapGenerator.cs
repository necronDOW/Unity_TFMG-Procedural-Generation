using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public int mapChunkSize = 239;
    public int borderedChunkSize;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;
    public Material terrainMaterial;

    float[,] falloffMap;

    [Range(0,100)]
    public float plainsHeight;
    [Range(0,100)]
    public float mountainsHeight;

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

    Queue<MapThreadInfo<float[,]>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<float[,]>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void RequestMapData(Vector2 center, Action<float[,]> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(center, callback); };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<float[,]> callback)
    {
        float[,] heightMap = GenerateMapData(center, plainsHeight, mountainsHeight);

        lock (mapDataThreadInfoQueue)
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<float[,]>(callback, heightMap));
    }

    public void RequestMeshData(float[,] heightMap, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(heightMap, lod, callback); };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(float[,] heightMap, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, terrainData.meshHeightCurve, terrainData.meshHeightMultiplier, lod);

        lock (meshDataThreadInfoQueue)
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    private void Update()
    {
        for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
        {
            MapThreadInfo<float[,]> threadInfo = mapDataThreadInfoQueue.Dequeue();
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
        plainsHeight /= terrainData.uniformScale;
        mountainsHeight /= terrainData.uniformScale;
        
        if (terrainData.useFalloff)
            falloffMap = MaskLib.Falloff(borderedChunkSize);

        if (textureData != null)
            textureData.ApplyToMaterial(terrainMaterial);
    }

    public float[,] GenerateMapData(Vector2 center, float preferedHeight, float preferedHeightVariance)
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
        return noiseMap;
    }
}