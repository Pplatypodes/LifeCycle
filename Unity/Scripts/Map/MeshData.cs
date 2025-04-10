using UnityEngine;

public class MeshData
{
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    private Vector3[] borderVertices;
    private int[] borderTriangles;

    private int triangleIndex;
    private int borderTriangleIndex;

    /* Constructeur : Initialise les tableaux de vertices, UVs, triangles et bordures */
    public MeshData(int verticesPerLine)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        // Calculer une taille sécurisée pour les vertices et triangles de bordure
        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    /* Ajoute un vertex ; les indices négatifs représentent des vertices de bordure */
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
            // Ajoute dans le tableau de bordure en ajustant l'indice
            borderVertices[-vertexIndex - 1] = vertexPosition;
        else
        {
            // Stocke la position et l'UV dans le tableau principal
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    /* Ajoute un triangle en utilisant les indices de vertices */
    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            // Ajoute le triangle dans les triangles de bordure
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            // Ajoute le triangle dans le tableau principal de triangles
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    /* Calcule les normales pour chaque vertex en combinant les normales de chaque triangle */
    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        
        // Calculer les normales pour les triangles principaux
        for (int i = 0; i < triangleCount; i++)
        {
            int index = i * 3;
            int vertexIndexA = triangles[index];
            int vertexIndexB = triangles[index + 1];
            int vertexIndexC = triangles[index + 2];

            // Calculer la normale du triangle
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        
        // Calculer les normales pour les triangles de bordure
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int index = i * 3;
            int vertexIndexA = borderTriangles[index];
            int vertexIndexB = borderTriangles[index + 1];
            int vertexIndexC = borderTriangles[index + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0) vertexNormals[vertexIndexC] += triangleNormal;
        }

        // Normaliser chaque vecteur normal
        for (int i = 0; i < vertexNormals.Length; i++)
            vertexNormals[i].Normalize();

        return vertexNormals;
    }

    /* Calcule la normale de surface pour un triangle donné par ses indices */
    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        // Récupérer les positions en tenant compte des vertices de bordure
        Vector3 pointA = indexA < 0 ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = indexB < 0 ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = indexC < 0 ? borderVertices[-indexC - 1] : vertices[indexC];

        // Calculer les vecteurs de côté et utiliser le produit vectoriel
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    /* Crée et retourne un Mesh Unity en utilisant les vertices, triangles, UV et normales calculées */
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        
        // Assigner les vertices, triangles et UVs
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Calculer et assigner les normales
        mesh.normals = CalculateNormals();
        return mesh;
    }
}
