using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class DungeonGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct MinimumMutators
    {
        public int roomSize;
        public int corridorSize;
        public int doorSize;
        public int floorHeight;
        public float maxFloorUsage;
        public int wallThickness;
    }

    public class Section
    {
        public Section parent;
        List<Section> children = new List<Section>();
        public Vector3Int position;
        public Vector3Int size;
        public int startFloor;
        public int endFloor;

        public Bounds Bounds => new Bounds(
            position + size / 2,
            size
        );

        public bool IntersectsFloor(int floor)
        {
            return floor >= startFloor && floor <= endFloor;
        }
    }

    public void GenerateDungeon(Vector3Int size, MinimumMutators minimumMutators)
    {
        
    }

    public void GenerateFloor()
    {
        
    }
    

}
