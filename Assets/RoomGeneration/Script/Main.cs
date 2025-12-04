using System.Collections.Generic;
using UnityEngine;
using static DungeonGenerator;
using static Directions;
using static Walls;
using static Doors;

public class Main : MonoBehaviour
{
    private DungeonGenerator dungeonGenerator;
    [SerializeField] private DungeonGenerator.Specifications specifications;
    [SerializeField] private Vector3Int size;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private int seed = 0;
    [SerializeField] private Direction startDirection = Direction.North;
    [SerializeField] private GameObject meshLayerPrefab;
    [SerializeField] private GameObject regionMeshPrefab;
    [SerializeField] private bool debugMode = false;
    
    private List<RegionMesh> regionMeshes = new List<RegionMesh>();
    
    // For testing /////////////////////////////////////////////////////////////////////////////////////////////////////
    public static Main instance;
    private bool generationCompleted = false;
    public bool GetGenerationCompleted() => generationCompleted;
    public DungeonGenerator GetDungeonGenerator() => dungeonGenerator;
    public Specifications GetSpecs() => specifications;
    public int GetSeed() => seed;
    // DEBUG
    private MeshLayer doorFloorLayer;
    private MeshLayer roomFloorLayer;
    private MeshLayer corridorFloorLayer;
    private MeshLayer macroMainFloorLayer;
    private MeshLayer macroSideFloorLayer;
    private MeshLayer corridorWallLayer;
    private MeshLayer roomWallLayer;
    private MeshLayer macroMainCorridor;
    private MeshLayer macroSideCorridor;
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
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
        if (specifications.regions.Count == 0)
        {
            Debug.LogError("No regions specified, running in debug mode.");
            CreateDungeonDebug();
            return;
        }
        
        foreach (var region in specifications.regions)
        {
            regionMeshes.Add(Instantiate(regionMeshPrefab).GetComponent<RegionMesh>());
            regionMeshes[^1].Initialize(region);
        }
        
        dungeonGenerator = gameObject.AddComponent<DungeonGenerator>();;
        dungeonGenerator.GenerateDungeon(size, specifications, seed, startDirection);
        Debug.Log($"Dungeon generated with {dungeonGenerator.rooms.Count} rooms.");
        
        foreach (var section in dungeonGenerator.rooms)
        {
            List<DoorOffset> doors = new List<DoorOffset>();
            doors.AddRange(section.northDoors);
            doors.AddRange(section.eastDoors);
            doors.AddRange(section.southDoors);
            doors.AddRange(section.westDoors);
            
            if (section.isCorridor)
            {
                regionMeshes[section.regionIndex].AddCorridorFloorGeometry(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
                regionMeshes[section.regionIndex].AddCorridorCeilingGeometry(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));

                Vector3 doorSize = section.leadingDoor.size;
                doorSize.y = 0;
                regionMeshes[section.regionIndex].AddRoomFloorGeometry(section.leadingDoor.position,
                    section.leadingDoor.size, specifications.UVScale, new Vector3(1, 1, 1));
                
                if (specifications.regions[section.regionIndex].doorPrefab != null)
                    Instantiate(specifications.regions[section.regionIndex].doorPrefab, 
                        Vector3.Lerp(section.leadingDoor.position, section.leadingDoor.position + doorSize, 0.5f), 
                        Quaternion.LookRotation(DirectionToVector(section.direction), Vector3.up));
            }
            else
            { 
                regionMeshes[section.regionIndex].AddRoomFloorGeometry(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
                regionMeshes[section.regionIndex].AddRoomCeilingGeometry(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
                
                Vector3 doorSize = section.leadingDoor.size;
                doorSize.y = 0;
                regionMeshes[section.regionIndex].AddRoomFloorGeometry(section.leadingDoor.position,
                    section.leadingDoor.size, specifications.UVScale, new Vector3(1, 1, 1));
                
                if (specifications.regions[section.regionIndex].doorPrefab != null)
                    Instantiate(specifications.regions[section.regionIndex].doorPrefab, 
                        Vector3.Lerp(section.leadingDoor.position, section.leadingDoor.position + doorSize, 0.5f), 
                        Quaternion.LookRotation(DirectionToVector(section.direction), Vector3.up));
            }
            
            Wall[] walls = section.GetWalls(specifications.doorHeight, specifications.doorWidth);

            foreach (Wall wall in walls)
            {
                bool isVertical = DirectionIsVertical(wall.direction);
                bool flip = isVertical && wall.direction == Direction.North || !isVertical && wall.direction == Direction.West;
                
                if (section.isCorridor)
                    regionMeshes[section.regionIndex].AddCorridorWallGeometry(wall.position, wall.size, specifications.UVScale, new Vector3(1,1,1), isVertical, flip);
                else
                    regionMeshes[section.regionIndex].AddRoomWallGeometry(wall.position, wall.size,  specifications.UVScale, new Vector3(1,1,1), isVertical, flip);
            }
        }

        foreach (RegionMesh regionMesh in regionMeshes)
        {
            regionMesh.UpdateMeshes();
        }
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
        dungeonGenerator.GenerateDungeon(size, specifications, seed, startDirection);
        Debug.Log($"Dungeon generated with {dungeonGenerator.rooms.Count} rooms.");
        foreach (var section in dungeonGenerator.rooms)
        {
            if (section.isMacroMainCorridor)
                macroMainFloorLayer.AddFloorGeometryToMesh(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
            else if (section.isMacroSideCorridor)
                macroSideFloorLayer.AddFloorGeometryToMesh(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
            else if (section.isCorridor)
                corridorFloorLayer.AddFloorGeometryToMesh(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
            else
                roomFloorLayer.AddFloorGeometryToMesh(section.position, section.size, specifications.UVScale, new Vector3(1,1,1));
            
            Wall[] walls = section.GetWalls(specifications.doorHeight, specifications.doorWidth);

            foreach (Wall wall in walls)
            {
                bool isVertical = DirectionIsVertical(wall.direction);
                bool flip = isVertical && wall.direction == Direction.North || !isVertical && wall.direction == Direction.West;
                
                if (section.isMacroMainCorridor)
                    macroMainCorridor.AddWallGeometryToMesh(wall.position, wall.size, specifications.UVScale, new Vector3(1,1,1), isVertical, flip);
                else if (section.isMacroSideCorridor)
                    macroSideCorridor.AddWallGeometryToMesh(wall.position, wall.size, specifications.UVScale, new Vector3(1,1,1), isVertical, flip);
                else if (section.isCorridor)
                    corridorWallLayer.AddWallGeometryToMesh(wall.position, wall.size, specifications.UVScale, new Vector3(1,1,1), isVertical, flip);
                else
                    roomWallLayer.AddWallGeometryToMesh(wall.position, wall.size,  specifications.UVScale, new Vector3(1,1,1), isVertical, flip);
            }
        }

        foreach (Door door in dungeonGenerator.doors)
        {
            doorFloorLayer.AddFloorGeometryToMesh(door.position, door.size, specifications.UVScale, new Vector3(1,1,1));
        }
        
        foreach (Door door in dungeonGenerator.corridorDoors)
        {
            doorFloorLayer.AddFloorGeometryToMesh(door.position, door.size, specifications.UVScale, new Vector3(1,1,1));
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
