using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer renderer;

    public void DrawTexture(Texture2D texture)
    {
        if (renderer)
        {
            renderer.sharedMaterial.mainTexture = texture;
        }

        renderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
}
