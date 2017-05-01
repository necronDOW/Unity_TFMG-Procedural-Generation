using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdateableData
{
    public float uniformScale = 2.5f;
    public bool flatShading;
    public bool useFalloff;
    public AnimationCurve meshHeightCurve;
    public float meshHeightMultiplier = 1;
}
