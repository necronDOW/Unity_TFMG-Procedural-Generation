using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, AnimationCurve _heightCurve, float heightMultiplier, int levelOfDetail, bool flatShading)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int simplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * simplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, flatShading);

        int[,] vertexMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex)
                {
                    vertexMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                int vertexIndex = vertexMap[x, y];
                Vector2 uv = new Vector2((x - simplificationIncrement) / (float)meshSize, (y - simplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertex = new Vector3(topLeftX + uv.x * meshSizeUnsimplified, height, topLeftZ - uv.y * meshSizeUnsimplified);

                meshData.AddVertex(vertex, uv, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexMap[x, y];
                    int b = vertexMap[x + simplificationIncrement, y];
                    int c = vertexMap[x, y + simplificationIncrement];
                    int d = vertexMap[x + simplificationIncrement, y + simplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.Finalize();

        return meshData;
    }
}

public class MeshData
{
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private Vector3[] bakedNormals;

    private Vector3[] borderVertices;
    private int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool flatShading;

    public MeshData(int vertsPerLine, bool flatShading = false)
    {
        this.flatShading = flatShading;

        vertices = new Vector3[vertsPerLine * vertsPerLine];
        triangles = new int[(vertsPerLine - 1) * (vertsPerLine - 1) * 6];
        uvs = new Vector2[vertsPerLine * vertsPerLine];

        borderVertices = new Vector3[vertsPerLine * 4 + 4];
        borderTriangles = new int[24 * vertsPerLine];
    }

    public void AddVertex(Vector3 position, Vector3 uv, int index)
    {
        if (index < 0)
        {
            borderVertices[-index - 1] = position;
        }
        else
        {
            vertices[index] = position;
            uvs[index] = uv;
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

        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int triangleIndex = i * 3;
            int indexA = triangles[triangleIndex];
            int indexB = triangles[triangleIndex + 1];
            int indexC = triangles[triangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormals(indexA, indexB, indexC);
            vertexNormals[indexA] += triangleNormal;
            vertexNormals[indexB] += triangleNormal;
            vertexNormals[indexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int triangleIndex = i * 3;
            int indexA = borderTriangles[triangleIndex];
            int indexB = borderTriangles[triangleIndex + 1];
            int indexC = borderTriangles[triangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormals(indexA, indexB, indexC);

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

    Vector3 SurfaceNormals(int a, int b, int c)
    {
        Vector3 pointA = a < 0 ? borderVertices[-a - 1] : vertices[a];
        Vector3 pointB = b < 0 ? borderVertices[-b - 1] : vertices[b];
        Vector3 pointC = c < 0 ? borderVertices[-c - 1] : vertices[c];

        Vector3 left = pointB - pointA;
        Vector3 right = pointC - pointA;

        return Vector3.Cross(left, right).normalized;
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    public void Finalize()
    {
        if (flatShading)
            FlatShading();
        else BakeNormals();
    }

    private void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (flatShading)
            mesh.RecalculateNormals();
        else mesh.normals = bakedNormals;

        return mesh;
    }
}
