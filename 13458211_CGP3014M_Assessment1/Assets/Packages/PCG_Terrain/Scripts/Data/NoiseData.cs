using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdateableData
{
    public Noise.NormalizeMode normalizeMode;
    public float noiseScale = 30f;
    [Range(1, 6)]
    public int octaves = 3;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed;
    public Vector2 offset;

    protected override void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;

        base.OnValidate();
    }
}
