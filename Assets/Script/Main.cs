using System.Collections.Generic;
using UnityEngine;
using static DungeonGenerator;
using static Directions;
using static Walls;



public class Main : MonoBehaviour
{
    private DungeonGenerator dungeonGenerator;
    [SerializeField] private DungeonGenerator.MinimumMutators minimumMutators;
    [SerializeField] private Vector3Int size;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private int seed = 0;
    [SerializeField] private Direction startDirection = Direction.North;
    [SerializeField] private GameObject meshLayerPrefab;
    [SerializeField] private bool debugMode = false;
    
    // For testing /////////////////////////////////////////////////////////////////////////////////////////////////////
    public static Main instance;
    private bool generationCompleted = false;
    public bool GetGenerationCompleted() => generationCompleted;
    public DungeonGenerator GetDungeonGenerator() => dungeonGenerator;
    public MinimumMutators GetMinimumMutators() => minimumMutators;
    
    public int GetSeed() => seed;
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private MeshLayer doorFloorLayer;
    private MeshLayer roomFloorLayer;
    private MeshLayer corridorFloorLayer;
    private MeshLayer macroMainFloorLayer;
    private MeshLayer macroSideFloorLayer;
    private MeshLayer corridorWallLayer;
    private MeshLayer roomWallLayer;
    private MeshLayer macroMainCorridor;
    private MeshLayer macroSideCorridor;
    
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
        
        if(debugMode)
            CreateDungeonDebug();
        else
            CreateDungeon();
        
    }

    public void CreateDungeon()
    {
        
    }

    public void CreateDungeonDebug()
    {
        doorFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        doorFloorLayer.gameObject.name = "Door Floor Layer";
        roomFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomFloorLayer.gameObject.name = "Room Floor Layer";
        corridorFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorFloorLayer.gameObject.name = "Corridor Floor Layer";
        macroMainFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        macroMainFloorLayer.gameObject.name = "Macro Main Floor";
        macroSideFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        macroSideFloorLayer.gameObject.name = "Macro Side Floor";
        roomWallLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomWallLayer.gameObject.name = "Room Wall Layer";
        corridorWallLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorWallLayer.gameObject.name = "Corridor Wall Layer";
        macroMainCorridor = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        macroMainCorridor.gameObject.name = "Macro Main Corridor";
        macroSideCorridor = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        macroSideCorridor.gameObject.name = "Macro Side Corridor";
        
        dungeonGenerator = gameObject.AddComponent<DungeonGenerator>();;
        dungeonGenerator.GenerateDungeon(size, minimumMutators, seed, startDirection);
        foreach (var section in dungeonGenerator.rooms)
        {
            if (section.isMacroMainCorridor)
                macroMainFloorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            else if (section.isMacroSideCorridor)
                macroSideFloorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            else if (section.isCorridor)
                corridorFloorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            else
                roomFloorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            
            Wall[] walls = section.GetWalls(minimumMutators.doorHeight, minimumMutators.doorWidth);

            foreach (Wall wall in walls)
            {
                bool isVertical = DirectionIsVertical(wall.direction);
                bool flip = isVertical && wall.direction == Direction.North || !isVertical && wall.direction == Direction.West;
                
                if (section.isMacroMainCorridor)
                    macroMainCorridor.AddWallGeometryToMesh(wall.position, wall.size, new Vector2(1,1), new Vector3(1,1,1), isVertical, flip);
                else if (section.isMacroSideCorridor)
                    macroSideCorridor.AddWallGeometryToMesh(wall.position, wall.size, new Vector2(1,1), new Vector3(1,1,1), isVertical, flip);
                else if (section.isCorridor)
                    corridorWallLayer.AddWallGeometryToMesh(wall.position, wall.size, new Vector2(1,1), new Vector3(1,1,1), isVertical, flip);
                else
                    roomWallLayer.AddWallGeometryToMesh(wall.position, wall.size,  new Vector2(1,1), new Vector3(1,1,1), isVertical, flip);
            }
        }

        foreach (var section in dungeonGenerator.doors)
        {
            doorFloorLayer.AddFloorGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
        }
        roomFloorLayer.SetMaterial(floorMaterial);
        corridorFloorLayer.SetMaterial(floorMaterial);
        macroMainFloorLayer.SetMaterial(floorMaterial);
        macroSideFloorLayer.SetMaterial(floorMaterial);
        doorFloorLayer.SetMaterial(floorMaterial);
        roomWallLayer.SetMaterial(wallMaterial);
        corridorWallLayer.SetMaterial(wallMaterial);
        macroMainCorridor.SetMaterial(wallMaterial);
        macroSideCorridor.SetMaterial(wallMaterial);
        corridorFloorLayer.UpdateMesh();
        macroMainFloorLayer.UpdateMesh();
        macroSideFloorLayer.UpdateMesh();
        roomFloorLayer.UpdateMesh();
        doorFloorLayer.UpdateMesh();
        roomWallLayer.UpdateMesh();
        corridorWallLayer.UpdateMesh();
        macroMainCorridor.UpdateMesh();
        macroSideCorridor.UpdateMesh();
        generationCompleted = true;
    }
}
