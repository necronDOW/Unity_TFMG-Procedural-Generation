using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    public string[] randomAxioms;
    public GameObject roadPrefab;
    public GameObject[] buildingPrefabs;
    public float scalar = 1.0f;

    private const int MAX_BUILDING_VALUE = 4;

    public string randomAxiom(System.Random rng)
    {
        return randomAxioms[rng.Next(0, randomAxioms.Length)];
    }

    public void BuildCity(Vector3 position, string axiom, int size)
    {
        CityBuilder builder = new GameObject("City Builder").AddComponent<CityBuilder>();

        int[,] generatedGrid = builder.Activate(axiom, size);
        int sizeX = generatedGrid.GetLength(0);
        int sizeY = generatedGrid.GetLength(1);
        int midX = sizeX / 2, midY = sizeY / 2;
        Vector3 offset = new Vector3(-midX, 0, -midY) * scalar;
        
        for (int x = 1; x < sizeX - 1; x++)
        {
            for (int y = 1; y < sizeY - 1; y++)
                Evaluate(generatedGrid, x, y, midX, midY, position + offset);
        }
    }
    
    private int GridBuildingValue(int[,] grid, ref float rotation, int x, int y)
    {
        if (grid[x, y] == 1)
            return 1;
        else
        {
            if (grid[x + 1, y] == 1)
                rotation = 180.0f;
            else if (grid[x, y - 1] == 1)
                rotation = -90.0f;
            else if (grid[x - 1, y] == 1)
                rotation = 0.0f;
            else if (grid[x, y + 1] == 1)
                rotation = 90.0f;

            return 0;
        }
    }

    bool build = true;
    private void Evaluate(int[,] generatedGrid, int x, int y, int midX, int midY, Vector3 offset)
    {
        float rotation = -1.0f;
        int value = GridBuildingValue(generatedGrid, ref rotation, x, y);

        if (value == 1)
        {
            GameObject road = Instantiate(roadPrefab, offset + new Vector3(x, 0, y) * scalar, Quaternion.identity);
            road.transform.localScale *= scalar;
            road.hideFlags = HideFlags.HideInHierarchy;
        }
        else if (value == 0 && rotation != -1.0f && build)
        {
            int index = Mathf.Clamp((-Mathf.Abs(midX - x) + midX - Mathf.Abs(midY - y) + midY) / (buildingPrefabs.Length * 2), 0, buildingPrefabs.Length - 1);
            GameObject building = Instantiate(buildingPrefabs[index], offset + (new Vector3(x, 0, y) * scalar), buildingPrefabs[index].transform.rotation);
            building.transform.Rotate(transform.forward, rotation);
            building.transform.localScale *= scalar;
            building.hideFlags = HideFlags.HideInHierarchy;

            build = false;
        }
        else build = true;
    }
}
