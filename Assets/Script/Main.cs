using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    DungeonGenerator dungeonGenerator;
    [SerializeField] DungeonGenerator.MinimumMutators minimumMutators;
    [SerializeField] Vector3Int size;
    [SerializeField] Material floorMaterial;
    [SerializeField] int seed = 0;
    [SerializeField] DungeonGenerator.Direction startDirection = DungeonGenerator.Direction.North;
    [SerializeField] GameObject meshLayerPrefab;

    private MeshLayer doorLayer;
    private MeshLayer roomLayer;
    private MeshLayer corridorLayer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        doorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        dungeonGenerator = gameObject.AddComponent<DungeonGenerator>();;
        dungeonGenerator.GenerateDungeon(size, minimumMutators, seed, startDirection);
        foreach (var section in dungeonGenerator.rooms)
        {
            if (section.isCorridor)
                corridorLayer.AddGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            else
                roomLayer.AddGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
        }

        foreach (var section in dungeonGenerator.doors)
        {
            doorLayer.AddGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
        }
        
        corridorLayer.UpdateMesh();
        roomLayer.UpdateMesh();
        doorLayer.UpdateMesh();
        roomLayer.SetMaterial(floorMaterial);
        corridorLayer.SetMaterial(floorMaterial);
    }
}
