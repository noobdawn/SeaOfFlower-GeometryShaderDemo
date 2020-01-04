using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WindField))]
public class FieldManager : MonoBehaviour
{
    [SerializeField] Texture2D _heightMap;
    [SerializeField] float _maxHeight = 10;
    [SerializeField] int _terrainSize = 100;
    [SerializeField] int _frequency = 5;
    [SerializeField] Material _terrainMat, _grassMat;

    private WindField windField;

    private void Awake()
    {
        GameObject terrain = new GameObject("_Terrain");
        GameObject grass = new GameObject("_Grass");
        terrain.transform.parent = grass.transform.parent = transform;
        var mfT = terrain.AddComponent<MeshFilter>();
        var mfG = grass.AddComponent<MeshFilter>();
        mfT.mesh = FieldMesh.CreateField(_heightMap, _maxHeight, _terrainSize);
        mfG.mesh = FieldMesh.CreateGrass(_heightMap, _maxHeight, _terrainSize, _frequency);
        var mrT = terrain.AddComponent<MeshRenderer>();
        var mrG = grass.AddComponent<MeshRenderer>();
        mrT.material = _terrainMat;
        mrG.material = _grassMat;
        windField = GetComponent<WindField>();
    }

    private void Update()
    {
        if (windField._windTex != null)
            _grassMat.SetTexture("_WindField", windField._windTex);
        windField.AddWind(Vector2.zero, new Vector2(Random.Range(-1, 1), -1), Random.Range(1, 5), 0.2f);

    }
}
