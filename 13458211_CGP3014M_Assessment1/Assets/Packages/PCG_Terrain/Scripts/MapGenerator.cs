using UnityEngine;

public class MapGenerator : MonoBehaviour
{
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

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.Perlin(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);

        MapDisplay display = GetComponent<MapDisplay>();
        if (display)
        {
            display.DrawNoiseMap(noiseMap);
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
