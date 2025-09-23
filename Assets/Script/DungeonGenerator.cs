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
        public Vector3Int position;
        public Vector3Int size;
        public int startFloor;
        public int endFloor;
        public bool isCorridor;
        public bool isRoom;
        public List<Section> subsections = new List<Section>();
        public List<Door> doors = new List<Door>();

        public Bounds Bounds => new Bounds(
            position + size / 2,
            size
        );

        public bool IntersectsFloor(int floor)
        {
            return floor >= startFloor && floor <= endFloor;
        }
    }

    public struct Door : IEquatable<Door>
    {
        public Vector3Int position;
        public Vector3Int size;
        public Section sectionA;
        public Section sectionB;

        public bool Equals(Door other)
        {
            return position.Equals(other.position);
        }

        public override bool Equals(object obj)
        {
            return obj is Door other && Equals(other);
        }

        public override int GetHashCode()
        {
            return position.GetHashCode();
        }
    }

    private MinimumMutators mutators;
    public Section mainSection;
    public List<Section> sectionsPerFloor;
    private int currentFloor;
    private int floors;

    public void GenerateDungeon(Vector3Int size, MinimumMutators mins)
    {
        mutators = mins;
        floors = mutators.floorHeight > 0 ? Mathf.CeilToInt(size.y / mutators.floorHeight) : 1;
        mainSection = new Section
        {
            position = Vector3Int.zero,
            size = size,
            startFloor = 0,
            endFloor = floors - 1
        };

        sectionsPerFloor = new List<Section>();
        
        for (currentFloor = 0; currentFloor < floors; currentFloor++)
        {
            GenerateFloor(mainSection);
        }
    }

    private void GenerateFloor(Section parentSection)
    {
        // Get sections from floors below that intersect with current floor
        List<Section> intersectingSections = GetIntersectingSectionsFromBelow(currentFloor);
        
        // Calculate available space for this floor
        Bounds availableSpace = CalculateAvailableSpace(parentSection, intersectingSections);
        
        // Generate primary corridors
        List<Section> corridors = GenerateCorridors(availableSpace);
        
        // Divide remaining space into subsections
        List<Section> subsections = DivideSpaceIntoSubsections(availableSpace, corridors);
        
        // Connect sections with doors
        ConnectSectionsWithDoors(corridors, subsections);
        
        // Store sections for this floor
        sectionsPerFloor.AddRange(corridors);
        sectionsPerFloor.AddRange(subsections);
    }

    private List<Section> GetIntersectingSectionsFromBelow(int floor)
    {
        List<Section> intersecting = new List<Section>();
        
        foreach (var section in sectionsPerFloor)
        {
            if (section.IntersectsFloor(floor))
            {
                intersecting.Add(section);
            }
        }
        
        return intersecting;
    }

    private Bounds CalculateAvailableSpace(Section parent, List<Section> intersecting)
    {
        Bounds available = parent.Bounds;
        
        foreach (var section in intersecting)
        {
            // Subtract intersecting section volumes from available space
            // This is a simplified version - you'd need proper CSG operations
            available.Encapsulate(section.Bounds);
        }
        
        return available;
    }

    private List<Section> GenerateCorridors(Bounds availableSpace)
    {
        List<Section> corridors = new List<Section>();
        
        // Example corridor generation strategy:
        // 1. Create main corridor along longest axis
        // 2. Create branching corridors as needed
        Vector3 size = availableSpace.size;
        bool horizontalMain = size.x > size.z;
        
        Section mainCorridor = new Section
        {
            position = Vector3Int.RoundToInt(availableSpace.min),
            size = Vector3Int.RoundToInt(new Vector3(
                horizontalMain ? size.x : mutators.corridorSize,
                mutators.corridorSize,
                horizontalMain ? mutators.corridorSize : size.z
            )),
            startFloor = currentFloor,
            endFloor = currentFloor,
            isCorridor = true
        };
        
        corridors.Add(mainCorridor);
        
        return corridors;
    }

    private List<Section> DivideSpaceIntoSubsections(Bounds availableSpace, List<Section> corridors)
    {
        List<Section> subsections = new List<Section>();
        Queue<Section> sectionsToProcess = new Queue<Section>();
        
        // Start with the entire available space minus corridors
        Section initial = new Section
        {
            position = Vector3Int.RoundToInt(availableSpace.min),
            size = Vector3Int.RoundToInt(availableSpace.size),
            startFloor = currentFloor,
            endFloor = currentFloor
        };
        
        sectionsToProcess.Enqueue(initial);
        
        while (sectionsToProcess.Count > 0)
        {
            Section current = sectionsToProcess.Dequeue();
            
            // Check if section meets minimum room size
            if (IsValidRoomSize(current))
            {
                if (Random.value < 0.5f || 
                    !CanSplitFurther(current))
                {
                    current.isRoom = true;
                    subsections.Add(current);
                    continue;
                }
                
                // Split section
                bool splitHorizontal = current.size.x > current.size.z;
                float ratio = Random.Range(0.3f, 0.7f);
                
                (Section a, Section b) = SplitSection(current, splitHorizontal, ratio);
                sectionsToProcess.Enqueue(a);
                sectionsToProcess.Enqueue(b);
            }
        }
        
        return subsections;
    }

    private bool IsValidRoomSize(Section section)
    {
        return section.size.x >= mutators.roomSize &&
               section.size.z >= mutators.roomSize;
    }

    private bool CanSplitFurther(Section section)
    {
        return section.size.x >= mutators.roomSize * 2 ||
               section.size.z >= mutators.roomSize * 2;
    }

    private (Section, Section) SplitSection(Section parent, bool horizontal, float ratio)
    {
        Vector3Int size1, size2;
        Vector3Int pos1 = parent.position;
        Vector3Int pos2;
        
        if (horizontal)
        {
            int split = Mathf.RoundToInt(parent.size.x * ratio);
            size1 = new Vector3Int(split, parent.size.y, parent.size.z);
            size2 = new Vector3Int(parent.size.x - split, parent.size.y, parent.size.z);
            pos2 = pos1 + new Vector3Int(split, 0, 0);
        }
        else
        {
            int split = Mathf.RoundToInt(parent.size.z * ratio);
            size1 = new Vector3Int(parent.size.x, parent.size.y, split);
            size2 = new Vector3Int(parent.size.x, parent.size.y, parent.size.z - split);
            pos2 = pos1 + new Vector3Int(0, 0, split);
        }
        
        return (
            new Section { position = pos1, size = size1, startFloor = currentFloor, endFloor = currentFloor },
            new Section { position = pos2, size = size2, startFloor = currentFloor, endFloor = currentFloor }
        );
    }

    private void ConnectSectionsWithDoors(List<Section> corridors, List<Section> subsections)
    {
        foreach (var section in subsections)
        {
            // Find nearest corridor
            Section nearestCorridor = FindNearestCorridor(section, corridors);
            
            if (nearestCorridor != null)
            {
                // Create door between section and corridor
                CreateDoor(section, nearestCorridor);
            }
        }
    }

    private Section FindNearestCorridor(Section section, List<Section> corridors)
    {
        float minDistance = float.MaxValue;
        Section nearest = null;
        
        foreach (var corridor in corridors)
        {
            float distance = Vector3.Distance(
                section.Bounds.center,
                corridor.Bounds.center
            );
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = corridor;
            }
        }
        
        return nearest;
    }

    private void CreateDoor(Section sectionA, Section sectionB)
    {
        // Find adjacent wall
        Vector3Int doorPos = CalculateDoorPosition(sectionA, sectionB);
        
        Door door = new Door
        {
            position = doorPos,
            size = new Vector3Int(mutators.doorSize, mutators.doorSize, mutators.wallThickness),
            sectionA = sectionA,
            sectionB = sectionB
        };
        
        sectionA.doors.Add(door);
        sectionB.doors.Add(door);
    }

    private Vector3Int CalculateDoorPosition(Section sectionA, Section sectionB)
    {
        // Simplified door positioning - you'd want more sophisticated placement
        Bounds boundsA = sectionA.Bounds;
        Bounds boundsB = sectionB.Bounds;
        
        return Vector3Int.RoundToInt(
            (boundsA.center + boundsB.center) * 0.5f
        );
    }
}
