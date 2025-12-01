using System.Collections.Generic;
using UnityEngine;
using static DungeonGenerator;

public class Main : MonoBehaviour
{
    private DungeonGenerator dungeonGenerator;
    [SerializeField] private DungeonGenerator.MinimumMutators minimumMutators;
    [SerializeField] private Vector3Int size;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private int seed = 0;
    [SerializeField] private DungeonGenerator.Direction startDirection = DungeonGenerator.Direction.North;
    [SerializeField] private GameObject meshLayerPrefab;
    
    // For testing /////////////////////////////////////////////////////////////////////////////////////////////////////
    public static Main instance;
    private bool generationCompleted = false;
    public bool GetGenerationCompleted() => generationCompleted;
    public DungeonGenerator GetDungeonGenerator() => dungeonGenerator;
    public MinimumMutators GetMinimumMutators() => minimumMutators;
    
    public int GetSeed() => seed;
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private MeshLayer doorLayer;
    private MeshLayer roomLayer;
    private MeshLayer corridorLayer;
    private MeshLayer wallLayer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (instance == null) 
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        
        doorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        wallLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        dungeonGenerator = gameObject.AddComponent<DungeonGenerator>();;
        dungeonGenerator.GenerateDungeon(size, minimumMutators, seed, startDirection);
        foreach (var section in dungeonGenerator.rooms)
        {
            if (section.isCorridor)
                corridorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            else
                roomLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            
            Wall[] walls = section.GetWalls(minimumMutators.doorHeight, minimumMutators.doorWidth);

            foreach (Wall wall in walls)
            {
                bool isVertical = DirectionIsVertical(wall.direction);
                bool flip = isVertical && wall.direction == Direction.North || !isVertical && wall.direction == Direction.West;
                    
                wallLayer.AddWallGeometryToMesh(wall.position, wall.size,  new Vector2(1,1), new Vector3(1,1,1), isVertical, flip);
            }
        }

        foreach (var section in dungeonGenerator.doors)
        {
            doorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
        }
        roomLayer.SetMaterial(floorMaterial);
        corridorLayer.SetMaterial(floorMaterial);
        doorLayer.SetMaterial(floorMaterial);
        wallLayer.SetMaterial(wallMaterial);
        corridorLayer.UpdateMesh();
        roomLayer.UpdateMesh();
        doorLayer.UpdateMesh();
        wallLayer.UpdateMesh();
        generationCompleted = true;
    }
}
