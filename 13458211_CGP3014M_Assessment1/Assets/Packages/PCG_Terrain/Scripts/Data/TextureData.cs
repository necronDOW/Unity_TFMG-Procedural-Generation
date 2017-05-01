using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdateableData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers;

    public void ApplyToMaterial(Material material)
    {
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
        material.SetFloat("minHeight", min);
        material.SetFloat("maxHeight", max);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
            textureArray.SetPixels(textures[i].GetPixels(), i);

        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public float textureScale;

        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0, 1)]
        public float blendStrength;

        [Range(0, 1)]
        public float startHeight;
    }
}
