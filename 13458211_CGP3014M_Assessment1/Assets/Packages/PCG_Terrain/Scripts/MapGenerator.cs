using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        FalloffMap,
        Mesh
    }
    public DrawMode drawMode;

    public int mapChunkSize = 95; // 95 for flat shading, 239 for normal.
    public int borderedChunkSize;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, 6)]
    public int editorLOD;
    
    public bool autoUpdate = false;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        borderedChunkSize = mapChunkSize + 2;

        if (terrainData.useFalloff)
            falloffMap = Falloff.Generate(borderedChunkSize);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
            DrawMap_Editor();
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMap_Editor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = GetComponent<MapDisplay>();
        if (display)
        {
            if (drawMode == DrawMode.NoiseMap)
                display.DrawTexture(TextureGenerator.FromHeightMap(mapData.heightMap));
            else if (drawMode == DrawMode.FalloffMap)
                display.DrawTexture(TextureGenerator.FromHeightMap(falloffMap));
            else if (drawMode == DrawMode.Mesh)
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightCurve, terrainData.meshHeightMultiplier, editorLOD, terrainData.flatShading));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);

        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightCurve, terrainData.meshHeightMultiplier, lod, terrainData.flatShading);

        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
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

    public MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.Perlin(borderedChunkSize, borderedChunkSize, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);
        
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

    private void OnValidate()
    {
        borderedChunkSize = mapChunkSize + 2;

        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        if (terrainData.useFalloff)
            falloffMap = Falloff.Generate(borderedChunkSize);

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

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
}

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}