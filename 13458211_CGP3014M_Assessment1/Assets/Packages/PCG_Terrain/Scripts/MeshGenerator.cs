using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, AnimationCurve _heightCurve, float heightMultiplier, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int simplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2;
        int meshSizeSimplified = borderedSize - 2 * simplificationIncrement;

        float topLeftX = (meshSize - 1) / -2f;
        float topLeftZ = (meshSize - 1) / 2f;
        int verticesPerLine = (meshSizeSimplified - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine);
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderedVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                if (y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1)
                    vertexIndicesMap[x, y] = borderedVertexIndex--;
                else vertexIndicesMap[x, y] = meshVertexIndex++;
            }
        }

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2((x - simplificationIncrement) / (float)meshSizeSimplified, (y - simplificationIncrement) / (float)meshSizeSimplified);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSize, height, topLeftZ - percent.y * meshSize);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + simplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + simplificationIncrement];
                    int d = vertexIndicesMap[x + simplificationIncrement, y + simplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData
{
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    public MeshData(int vertsPerLine)
    {
        vertices = new Vector3[vertsPerLine * vertsPerLine];
        triangles = new int[(vertsPerLine - 1) * (vertsPerLine - 1) * 6];
        uvs = new Vector2[vertsPerLine * vertsPerLine];

        borderVertices = new Vector3[vertsPerLine * 4 + 4];
        borderTriangles = new int[24 * vertsPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;

            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int indexA = triangles[i];
            int indexB = triangles[i + 1];
            int indexC = triangles[i + 2];

            Vector3 triangleNormal = SurfaceNormal(indexA, indexB, indexC);

            vertexNormals[indexA] += triangleNormal;
            vertexNormals[indexB] += triangleNormal;
            vertexNormals[indexC] += triangleNormal;
        }

        for (int i = 0; i < borderTriangles.Length; i += 3)
        {
            int indexA = borderTriangles[i];
            int indexB = borderTriangles[i + 1];
            int indexC = borderTriangles[i + 2];

            Vector3 triangleNormal = SurfaceNormal(indexA, indexB, indexC);

            if (indexA >= 0)
                vertexNormals[indexA] += triangleNormal;

            if (indexB >= 0)
                vertexNormals[indexB] += triangleNormal;

            if (indexC >= 0)
                vertexNormals[indexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++)
            vertexNormals[i].Normalize();

        return vertexNormals;
    }

    Vector3 SurfaceNormal(int a, int b, int c)
    {
        Vector3 pointA = (a < 0 ? borderVertices[-a - 1] : vertices[a]);
        Vector3 vecAB = (b < 0 ? borderVertices[-b - 1] : vertices[b]) - pointA;
        Vector3 vecAC = (c < 0 ? borderVertices[-c - 1] : vertices[c]) - pointA;

        return Vector3.Cross(vecAB, vecAC).normalized;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.normals = CalculateNormals();

        return mesh;
    }
}
