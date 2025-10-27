using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    DungeonGenerator dungeonGenerator;
    Mesher mesher;
    [SerializeField] DungeonGenerator.MinimumMutators minimumMutators;
    [SerializeField] Vector3Int size;
    [SerializeField] Material floorMaterial;
    [SerializeField] int seed = 0;
    [SerializeField] DungeonGenerator.Direction startDirection = DungeonGenerator.Direction.North;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        
        //HashSet<DungeonGenerator.Door> doors = new HashSet<DungeonGenerator.Door>();
        mesher = gameObject.AddComponent<Mesher>();
        dungeonGenerator = gameObject.AddComponent<DungeonGenerator>();;
        dungeonGenerator.GenerateDungeon(size, minimumMutators, seed, startDirection);
        foreach (var section in dungeonGenerator.rooms)
        {
            mesher.AddGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
        }

        foreach (var section in dungeonGenerator.doors)
        {
            mesher.AddGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
        }
        
        mesher.UpdateMesh();
        mesher.SetMaterial(floorMaterial);
    }
}
