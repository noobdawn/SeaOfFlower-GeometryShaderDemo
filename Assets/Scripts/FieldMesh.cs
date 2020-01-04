using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成地面网格
/// </summary>
public static class FieldMesh
{
    /// <summary>
    /// 创建地面网格
    /// </summary>
    /// <param name="heightMap">高度图</param>
    /// <param name="maxHeight">高度上限</param>
    /// <param name="terrainSize">网格规模</param>
    /// <returns></returns>
    public static Mesh CreateField(
        Texture2D heightMap,
        float maxHeight,
        int terrainSize
        )
    {
        // 分配顶点
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        for (int i = 0; i < terrainSize; i++)
        {
            for (int j = 0; j < terrainSize; j++)
            {
                verts.Add(new Vector3(i, heightMap.GetPixel(i, j).grayscale * maxHeight, j));
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
        //分配UV
        Vector2[] uvs = new Vector2[verts.Count];
        for (var i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(verts[i].x, verts[i].z);
        }
        Mesh res = new Mesh();
        res.vertices = verts.ToArray();
        res.uv = uvs;
        res.triangles = tris.ToArray();
        res.RecalculateNormals();
        return res;
    }

    /// <summary>
    /// 创建草场网格
    /// </summary>
    /// <param name="heightMap">高度图</param>
    /// <param name="maxHeight">高度上限</param>
    /// <param name="terrainSize">网格规模</param>
    /// <param name="frequency">创建密度</param>
    /// <returns></returns>
    public static Mesh CreateGrass(
        Texture2D heightMap,
        float maxHeight,
        int terrainSize,
        int frequency)
    {
        Mesh res = new Mesh();
        List<int> indices = new List<int>();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        int p = 0;
        for (int i = 0; i < terrainSize; i++)
        {
            for (int j = 0; j < terrainSize; j++)
            {
                for (int z = 0; z < frequency; z++)
                {
                    var point = new Vector3(i + Random.Range(-1f, 1f),
                        heightMap.GetPixel(i, j).grayscale * maxHeight,
                        j + Random.Range(-1f, 1f));
                    verts.Add(point);
                    indices.Add(p);
                    uvs.Add(new Vector2(point.x / terrainSize, point.z / terrainSize));
                    p++;
                }
            }
        }
        res.vertices = verts.ToArray();
        res.uv = uvs.ToArray();
        // 把Indices设置成点型拓扑
        res.SetIndices(indices.GetRange(0, verts.Count).ToArray(), MeshTopology.Points, 0);
        return res;
    }
}
