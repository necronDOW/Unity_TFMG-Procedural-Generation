using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        Mesh
    }
    public DrawMode drawMode;

    const int mapChunkSize = 241;
    [Range(0, 6)]
    public int levelOfDetail;
    public float noiseScale = 30f;

    [Range(1, 6)]
    public int octaves = 3;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    public int seed;
    public Vector2 offset;

    public AnimationCurve meshHeightCurve;
    public float meshHeightMultiplier = 1;

    public bool autoUpdate = false;

    public TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.Perlin(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        MapDisplay display = GetComponent<MapDisplay>();
        if (display)
        {
            if (drawMode == DrawMode.NoiseMap)
                display.DrawTexture(TextureGenerator.FromHeightMap(noiseMap));
            else if (drawMode == DrawMode.ColourMap)
                display.DrawTexture(TextureGenerator.FromColourMap(colourMap, mapChunkSize, mapChunkSize));
            else if (drawMode == DrawMode.Mesh)
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightCurve, meshHeightMultiplier, levelOfDetail), TextureGenerator.FromColourMap(colourMap, mapChunkSize, mapChunkSize));
        }
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}