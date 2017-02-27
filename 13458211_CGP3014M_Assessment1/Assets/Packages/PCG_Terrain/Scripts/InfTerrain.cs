using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfTerrain : MonoBehaviour
{
    public const float maxViews = 300;
    public Transform viewer;

    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisible;

    private void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViews / chunkSize);
    }

    void UpdateVisibleChunks()
    {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
    }
}
