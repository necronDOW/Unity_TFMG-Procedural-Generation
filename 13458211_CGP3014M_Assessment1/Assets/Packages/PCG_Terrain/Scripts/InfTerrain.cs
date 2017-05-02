using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfTerrain : MonoBehaviour
{
    const float scale = 5f;
    const float viewerMoveThreshold = 25f;
    const float sqrViewerMoveThreshold = viewerMoveThreshold * viewerMoveThreshold;

    public Transform viewer;
    public static Vector2 viewerPosition;
    public static Vector2 viewerPositionOld;
    public LODInfo[] detailLevels;
    public static float maxViews;
    
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLast = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();

        maxViews = detailLevels[detailLevels.Length - 1].visibilityThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViews / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThreshold)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLast.Count; i++)
            terrainChunksVisibleLast[i].SetVisible(false);

        terrainChunksVisibleLast.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                else terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapGenerator.terrainMaterial));
            }
        }
    }

    private void OnDrawGizmos()
    {
        //for (int i = 0; i < terrainChunksVisibleLast.Count; i++)
        //{
        //    MapData mapData = terrainChunksVisibleLast[i].mapData;

        //    int resourceSize = mapData.resourceSegments.GetLength(0);
        //    for (int j = 0; j < resourceSize; j++)
        //    {
        //        for (int k = 0; k < resourceSize; k++)
        //        {
        //            if (mapData.resourceSegments[j, k] != null)
        //            {
        //                if (mapData.resourceSegments[j, k].type != ResourceDataGenerator.ResourceWeighting.None)
        //                {
        //                    switch (mapData.resourceSegments[j, k].type)
        //                    {
        //                        case ResourceDataGenerator.ResourceWeighting.Iron:
        //                            Gizmos.color = Color.red;
        //                            break;
        //                        case ResourceDataGenerator.ResourceWeighting.Marble:
        //                            Gizmos.color = Color.grey;
        //                            break;
        //                        case ResourceDataGenerator.ResourceWeighting.Wood:
        //                            Gizmos.color = Color.green;
        //                            break;
        //                    }
                            
        //                    Gizmos.DrawCube(terrainChunksVisibleLast[i].cornerWorldPosition + (mapData.resourceSegments[j, k].center * mapGenerator.terrainData.uniformScale), new Vector3(10, 10, 10));
        //                }
        //            }
        //        }
        //    }
        //}
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Vector2 coord;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        public MapData mapData;
        public ResourceData[] resourceData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            this.coord = coord;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);

            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;

            meshFilter = meshObject.AddComponent<MeshFilter>();
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            
            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistance = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistance <= maxViews;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistance > detailLevels[i].visibilityThreshold)
                            lodIndex = i + 1;
                        else break;
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.meshReceived)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;

                            if (lodIndex == 0)
                                resourceData = mapGenerator.GenerateResources(meshFilter.mesh.vertices);
                        }
                        else if (!lodMesh.meshRequested)
                            lodMesh.RequestMesh(mapData);
                    }

                    terrainChunksVisibleLast.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void GenerateTrees()
        {
            int size = mapData.resourceSegments.GetLength(0);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    ResourceData segment = mapData.resourceSegments[i, j];
                    if (segment != null && segment.type == ResourceDataGenerator.ResourceWeighting.Wood)
                    {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);

                        Vector3 center = meshFilter.mesh.vertices[(int)((segment.center.z * 239) + segment.center.x)];
                        center.y *= mapGenerator.terrainData.uniformScale;
                        go.transform.position = cornerWorldPosition + center;
                    }
                }
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool Visible
        {
            get { return meshObject.activeSelf; }
            private set { }
        }

        public Vector3 cornerWorldPosition
        {
            get { return meshObject.transform.position - (new Vector3(bounds.extents.x, 0, -bounds.extents.y) * 2);  }
            private set { }
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool meshRequested;
        public bool meshReceived;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            meshReceived = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            meshRequested = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibilityThreshold;
    }
}
