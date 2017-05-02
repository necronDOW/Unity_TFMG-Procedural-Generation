using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : ScriptableObject
{
    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public float textureScale;

        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float blendStrength;

        [Range(0, 1)]
        public float startHeight;
    }
    public Layer[] layers;

    private float savedHeightMin;
    private float savedHeightMax;

    public void ApplyToMaterial(Material material)
    {
        material.SetFloat("minHeight", savedHeightMin);
        material.SetFloat("maxHeight", savedHeightMax);

        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrengths", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        material.SetTexture("baseTextures", GenerateTextureArray(layers.Select(x => x.texture).ToArray()));
    }

    public void UpdateMeshHeights(Material material, float min, float max)
    {
        savedHeightMin = min;
        savedHeightMax = max;
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(512, 512, textures.Length, TextureFormat.RGB565, true);
        for (int i = 0; i < textures.Length; i++)
            textureArray.SetPixels(textures[i].GetPixels(), i);

        textureArray.Apply();
        return textureArray;
    }
}
