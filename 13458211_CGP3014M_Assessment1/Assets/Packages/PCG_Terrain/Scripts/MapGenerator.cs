using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
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
        MapData mapData = GenerateMapData(center);

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

    public MapData GenerateMapData(Vector2 center)
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
        return new MapData(noiseMap);
    }
}