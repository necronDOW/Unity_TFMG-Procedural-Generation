using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

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
    static TreeGenerator treeGenerator;
    static CityGenerator cityGenerator;
    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLast = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        treeGenerator = GetComponent<TreeGenerator>();
        cityGenerator = GetComponent<CityGenerator>();

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

        float[,] heightMap;
        bool mapDataReceived;
        int previousLODIndex = -1;

        int maxResources = 5;
        List<Resource> resources;
        List<GameObject> loadedTrees;

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

        void OnMapDataReceived(float[,] heightMap)
        {
            this.heightMap = heightMap;
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
                            {
                                UpdateResources();
                                GenerateCities();
                            }

                            UpdateForests(lodIndex);
                        }
                        else if (!lodMesh.meshRequested)
                            lodMesh.RequestMesh(heightMap);
                    }

                    terrainChunksVisibleLast.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void UpdateResources()
        {
            if (resources == null)
            {
                resources = new List<Resource>();

                for (int i = 0; i < maxResources; i++)
                {
                    int seed = (int)((Time.deltaTime + position.x + position.y) * 10000) / (i + 1);
                    Resource r = new Resource(meshFilter.mesh.vertices, seed, mapGenerator.mapChunkSize, mapGenerator.plainsHeight, mapGenerator.mountainsHeight);

                    if (r.weighting != 0)
                        resources.Add(r);
                }
            }
        }

        public void UpdateForests(int lodIndex)
        {
            if (!treeGenerator)
                return;

            if (lodIndex == 0)
            {
                Vector3 positionV3 = new Vector3(position.x, 0, position.y);
                for (int i = 0; i < resources.Count; i++)
                {
                    if (resources[i].type == Resource.Type.Wood)
                    {
                        treeGenerator.Generate(ref loadedTrees, resources[i], meshFilter.mesh.vertices, mapGenerator.mapChunkSize,
                            positionV3, mapGenerator.terrainData.uniformScale, mapGenerator.plainsHeight, mapGenerator.mountainsHeight * 2, mapGenerator.mountainsHeight * 3);
                    }
                }
            }
            else if (loadedTrees != null)
            {
                for (int i = 0; i < loadedTrees.Count; i++)
                    DestroyImmediate(loadedTrees[i]);
            }
        }
        
        public void GenerateCities()
        {
            if (!cityGenerator)
                return;

            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            for (int i = 0; i < resources.Count; i++)
            {
                int index = (int)(resources[i].coords.y * mapGenerator.mapChunkSize + (int)resources[i].coords.x);
                Vector3 indexOffset = meshFilter.mesh.vertices[index];
                if (resources[i].type == Resource.Type.Marble)
                {
                    System.Random rng = new System.Random(resources[i].seed);
                    cityGenerator.BuildCity((positionV3 + indexOffset) * mapGenerator.terrainData.uniformScale, cityGenerator.randomAxiom(rng), rng.Next(2,5));
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

        public void RequestMesh(float[,] heightMap)
        {
            meshRequested = true;
            mapGenerator.RequestMeshData(heightMap, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibilityThreshold;
    }
}
