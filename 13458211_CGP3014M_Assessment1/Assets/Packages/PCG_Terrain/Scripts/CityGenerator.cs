using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    public string[] axioms;
    public GameObject roadPrefab;
    public GameObject[] buildingPrefabs;

    private const int MAX_BUILDING_VALUE = 4;

    float timeSinceLast = 0.0f;
    public void Update()
    {
        timeSinceLast += Time.deltaTime;

        if (timeSinceLast > 5.0f)
        {
            BuildCity(transform.position, new System.Random().Next(0, axioms.Length), 3);
            timeSinceLast = 0.0f;
        }
    }

    private void BuildCity(Vector3 position, int axiomIndex, int size)
    {
        CityBuilder builder = new GameObject("City Builder").AddComponent<CityBuilder>();
        builder.transform.position = position;

        int[,] generatedGrid = builder.Activate("A", size);
        int sizeX = generatedGrid.GetLength(0);
        int sizeY = generatedGrid.GetLength(1);

        for (int x = 1; x < sizeX - 1; x++)
        {
            for (int y = 1; y < sizeY - 1; y++)
                Evaluate(generatedGrid, x, y);
        }
    }

    private int GridBuildingValue(int[,] grid, int x, int y)
    {
        if (grid[x, y] == 1)
            return 0;

        return grid[x, y] + grid[x-1, y] + grid[x+1, y] + grid[x, y-1] + grid[x, y+1];
    }

    private void Evaluate(int[,] generatedGrid, int x, int y)
    {
        int value = GridBuildingValue(generatedGrid, x, y);

        if (generatedGrid[x,y] == 1)
        {
            GameObject road = Instantiate(roadPrefab, new Vector3(x, 0, y) * 10.0f, Quaternion.identity);
            road.hideFlags = HideFlags.HideInHierarchy;
        }
        else if (value != 0 && value <= MAX_BUILDING_VALUE && value < buildingPrefabs.Length)
        {
            GameObject building = Instantiate(buildingPrefabs[value-1], new Vector3(x, 0, y) * 10.0f, Quaternion.identity);
            building.hideFlags = HideFlags.HideInHierarchy;
        }
    }
}
