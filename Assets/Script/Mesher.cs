using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Mesher : MonoBehaviour
{
    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Vector3> normals = new List<Vector3>();

    private void Start()
    {
        mesh = new Mesh();
    }

    public void AddGeometryToMesh(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale)
    {
        Vector3[] newVertices = new Vector3[4]
        {
            Vector3.Scale(pos, worldScale),
            Vector3.Scale(pos + new Vector3(size.x, 0, 0), worldScale),
            Vector3.Scale(pos + new Vector3(0, 0, size.z) , worldScale),
            Vector3.Scale(pos + new Vector3(size.x, 0, size.z), worldScale)
        };
        
        Vector3[] newNormals = new Vector3[4]
        {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
        };
        
        Vector2[] newUvs = new Vector2[4]
        {
            new Vector2(pos.x, pos.z),
            Vector2.Scale(new Vector2(pos.x + size.x, pos.z), uvScale),
            Vector2.Scale(new Vector2(pos.x, pos.z + size.z), uvScale),
            Vector2.Scale(new Vector2(pos.x + size.x, pos.z + size.z), uvScale)
        };
        
        int[] newTriangles = new int[6]
        {
            vertices.Count, 
            vertices.Count + 2, 
            vertices.Count + 1,
            vertices.Count + 2, 
            vertices.Count + 3, 
            vertices.Count + 1
        };
        
        vertices.AddRange(newVertices);
        triangles.AddRange(newTriangles);
        uvs.AddRange(newUvs);
        normals.AddRange(newNormals);
    }

    public void UpdateMesh()
    {
        if (mesh == null)
        {
            Debug.LogError("Mesh is null");
            mesh = new Mesh();
        }
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        
        GetComponent<MeshFilter>().mesh = mesh;
    }
    
    public void ClearMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        normals.Clear();
        
        GetComponent<MeshFilter>().mesh = null;
        mesh = new Mesh();
    }

}
