using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : ScriptableObject
{
    public float uniformScale = 2.5f;
    public AnimationCurve meshHeightCurve;
    public float meshHeightMultiplier = 1;
    public bool useFalloff;

    public float minHeight
    {
        get { return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0); }
    }
    public float maxHeight
    {
        get { return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1); }
    }
}
