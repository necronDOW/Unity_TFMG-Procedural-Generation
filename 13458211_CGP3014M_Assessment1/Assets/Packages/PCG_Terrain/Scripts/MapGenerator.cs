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

    public int mapWidth = 100;
    public int mapHeight = 100;
    public float noiseScale = 30f;

    [Range(1, 6)]
    public int octaves = 3;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate = false;

    public TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.Perlin(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);
        Color[] colourMap = new Color[mapWidth * mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapWidth + x] = regions[i].colour;
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
                display.DrawTexture(TextureGenerator.FromColourMap(colourMap, mapWidth, mapHeight));
            else if (drawMode == DrawMode.Mesh)
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.FromColourMap(colourMap, mapWidth, mapHeight));
        }
    }

    private void OnValidate()
    {
        if (mapWidth < 1)
            mapWidth = 1;

        if (mapHeight < 1)
            mapHeight = 1;

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