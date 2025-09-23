using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    DungeonGenerator dungeonGenerator;
    Mesher mesher;
    [SerializeField] DungeonGenerator.MinimumMutators minimumMutators;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HashSet<DungeonGenerator.Door> doors = new HashSet<DungeonGenerator.Door>();
        mesher = gameObject.AddComponent<Mesher>();
        dungeonGenerator = gameObject.AddComponent<DungeonGenerator>();;
        dungeonGenerator.GenerateDungeon(new Vector3Int(500,500,500), minimumMutators);
        foreach (var section in dungeonGenerator.sectionsPerFloor)
        {
            mesher.AddGeometryToMesh(section.position, section.size, new Vector2(1,1), new Vector3(1,1,1));
            foreach (var door in section.doors)
            { 
                doors.Add(door);
            }
        }

        foreach (var door in doors)
        {
            mesher.AddGeometryToMesh(door.position, door.size, new Vector2(1,1), new Vector3(1,1,1) );
        }
        
        mesher.UpdateMesh();
    }
}
