using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCreator : MonoBehaviour {

    public Texture2D heightMap;
    public float MaxHeight;
    public int terrainSize = 100;
    public int flowersCountInEachBlock = 2;
    public int Mount = 5;
    public Material terrainMaterial;
    public Material grassMaterial;

    void Start()
    {
        CreateTerrain();
        CreateGrassField();
    }

    void CreateTerrain()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        for (int i = 0; i < this.terrainSize; i++)
        {
            for (int j = 0; j < this.terrainSize; j++)
            {
                verts.Add(new Vector3(i, heightMap.GetPixel(i, j).grayscale * MaxHeight, j));
                if (i == 0 || j == 0)
                    continue;
                tris.Add(terrainSize * i + j);
                tris.Add(terrainSize * i + j - 1);
                tris.Add(terrainSize * (i - 1) + j - 1);
                tris.Add(terrainSize * (i - 1) + j - 1);
                tris.Add(terrainSize * (i - 1) + j);
                tris.Add(terrainSize * i + j);
            }
        }
        Vector2[] uvs = new Vector2[verts.Count];

        for (var i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(verts[i].x, verts[i].z);
        }
        GameObject plane = new GameObject("groundPlane");
        plane.AddComponent<MeshFilter>();
        MeshRenderer renderer = plane.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = terrainMaterial;

        Mesh groundMesh = new Mesh();
        groundMesh.vertices = verts.ToArray();
        groundMesh.uv = uvs;
        groundMesh.triangles = tris.ToArray();
        groundMesh.RecalculateNormals();
        plane.GetComponent<MeshFilter>().mesh = groundMesh;
    }

    void CreateGrassField()
    {
        GameObject grassField = new GameObject("GrassField");
        MeshFilter mf = grassField.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        MeshRenderer mr = grassField.AddComponent<MeshRenderer>();
        mr.sharedMaterial = grassMaterial;
        List<int> indices = new List<int>();
        List<Vector3> verts = new List<Vector3>();
        int p = 0;
        for (int i = 0; i < this.terrainSize; i++)
        {
            for (int j = 0; j < this.terrainSize; j++)
            {
                for (int z = 0; z < Mount; z++)
                {
                    verts.Add(new Vector3(i + Random.Range(-1f, 1f), heightMap.GetPixel(i, j).grayscale * MaxHeight, j + Random.Range(-1f, 1f)));
                    indices.Add(p);
                    p++;
                }
            }
        }
        mesh.vertices = verts.ToArray();
        mesh.SetIndices(indices.GetRange(0, verts.Count).ToArray(), MeshTopology.Points, 0);
        mf.mesh = mesh;
    }
}
