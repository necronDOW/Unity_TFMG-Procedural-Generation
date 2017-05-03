using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
    public int maxGeneration = 5;
    public int startSpread = 2;

    public GameObject[] normalPrefabs;
    public GameObject[] snowPrefabs;

    struct Tree
    {
        public Vector2 coord { get; private set; }
        public int generation { get; private set; }

        public Tree(Vector2 coord, int generation)
        {
            this.coord = coord;
            this.generation = generation;
        }
    }

    public void Generate(ref List<GameObject> target, Resource resource, Vector3[] vertices, int verticesSize, Vector3 midPoint, float scalar = 1.0f, float minHeight = 0.0f, float snowHeight = float.MaxValue, float maxHeight = float.MaxValue)
    {
        if (target == null)
            target = new List<GameObject>();

        System.Random indexingRNG = new System.Random();

        List<Tree> trees = new List<Tree>();
        trees.Add(new Tree(resource.coords, maxGeneration));
        ForestAlgorithm(ref trees, maxGeneration, resource.coords, startSpread);

        for (int i = 0; i < trees.Count; i++)
        {
            int index = (int)(trees[i].coord.y * verticesSize) + (int)trees[i].coord.x;

            if (index > 0 && index < vertices.Length)
            {
                Vector3 vertexPosition = vertices[index];
                if (vertexPosition.y > minHeight && vertexPosition.y < maxHeight)
                {
                    Vector3 position = (midPoint + vertexPosition) * scalar;

                    if (vertexPosition.y > snowHeight)
                        target.Add(InstantiatePrefab(snowPrefabs, position, indexingRNG, trees[i].generation/maxGeneration));
                    else target.Add(InstantiatePrefab(normalPrefabs, position, indexingRNG, trees[i].generation/maxGeneration));
                }
            }
        }
    }

    private void ForestAlgorithm(ref List<Tree> treeCoords, int generation, Vector2 start, int spread)
    {
        if (generation == 0)
            return;

        int offsetX = new System.Random((int)(start.x * start.y)).Next(-spread, spread + 1);
        int offsetY = new System.Random((int)offsetX).Next(-spread, spread + 1);

        Vector2 coord1 = start + new Vector2(offsetX, offsetY);
        treeCoords.Add(new Tree(coord1, generation));
        ForestAlgorithm(ref treeCoords, generation - 1, coord1, spread + 1);

        Vector2 coord2 = start - new Vector2(offsetX, offsetY);
        treeCoords.Add(new Tree(coord2, generation));
        ForestAlgorithm(ref treeCoords, generation - 1, coord2, spread + 1);
    }

    private GameObject InstantiatePrefab(GameObject[] set, Vector3 position, System.Random rng, float scalar = 0.0f)
    {
        int index = rng.Next(0, set.Length);

        GameObject instance = Instantiate(set[index], position, set[index].transform.rotation);
        instance.transform.localScale += Vector3.one * scalar;
        instance.hideFlags = HideFlags.HideInHierarchy;

        return instance;
    }
}
