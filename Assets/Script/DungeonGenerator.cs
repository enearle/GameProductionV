using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
        public int floorThickness;
    }
    
    public enum Direction
    {
        North,
        South,
        West,
        East
    }

    public struct Door
    {
        public Vector3Int position;
        public Vector3Int size;
    }

    public struct SectionBounds
    {
        public SectionBounds(Vector3Int position, Vector3Int size)
        {
            maxX = position.x + size.x;
            maxZ = position.z + size.z;
            minX = position.x;
            minZ = position.z;
            y = position.y;
            height = size.y;
        }
        public int maxX;
        public int maxZ;
        public int minX;
        public int minZ;
        public int y;
        public int height;

        public Vector3Int GetPosition()
        {
            return new Vector3Int(minX, y, minZ);
        }
        
        public Vector3Int GetSize()
        {
            return new Vector3Int(maxX - minX, height, maxZ - minZ);
        }
    }
    
    public class Section
    {
        public Section parent;
        public List<Section> children = new List<Section>();
        public Vector3Int position;
        public Vector3Int size;
        public int startFloor;
        public int endFloor;
        public Direction roomDirection;
        public bool isRoom;
        public bool isCorridor;
        
        public bool IntersectsFloor(int floor)
        {
            return floor >= startFloor && floor <= endFloor;
        }
        
        public bool CheckSectionCanBeDivide(int minimumSectionSize)
        {
            if (size.x > minimumSectionSize && size.z > minimumSectionSize)
            {
                return true;
            }
            return false;
        }
    }

    public MinimumMutators minimumMutators;
    int minimumSectionSize;
    public List<Section> rooms = new List<Section>();
    public List<Section> floors = new List<Section>(); // In case I want to retroactively make changes to the dungeon
    public List<Door> doors = new List<Door>();
    
    public void GenerateDungeon(Vector3Int size, MinimumMutators minimumMutators)
    {
        this.minimumMutators = minimumMutators;
        minimumSectionSize = minimumMutators.roomSize * 2 + minimumMutators.corridorSize + minimumMutators.wallThickness * 2;

        for (int i = 0; i < size.y / minimumMutators.floorHeight; i++)
        {
            Vector3Int floorSize = new Vector3Int(size.x, minimumMutators.floorHeight, size.z);
            GenerateFloor(i, floorSize);
        }

        foreach (Section section in rooms)
        {
            doors.Add(CreateDoor(section));
        }
    }

    private void GenerateFloor(int floor, Vector3Int floorSize)
    {
        Section mainSection = new Section();
        mainSection.position = -floorSize / 2;
        mainSection.position.y = floor * minimumMutators.floorHeight;
        mainSection.size = floorSize;
        mainSection.size.y -= minimumMutators.floorThickness;
        mainSection.startFloor = floor;
        mainSection.endFloor = floor;
        mainSection.roomDirection = Direction.North;
        mainSection = SubdivideSection(mainSection, floor);
        floors.Add(mainSection);
    }

    private Section SubdivideSection(Section section, int floor)
    {
        if (section.CheckSectionCanBeDivide(minimumSectionSize))
        {
            SectionBounds parentBounds = new SectionBounds(section.position, section.size);
            SectionBounds corridorBounds = new SectionBounds();
            corridorBounds.y = parentBounds.y;
            corridorBounds.height = parentBounds.height;
            SectionBounds rightBounds = new SectionBounds();
            rightBounds.y = parentBounds.y;
            rightBounds.height = parentBounds.height;
            SectionBounds leftBounds = new SectionBounds();
            leftBounds.y = parentBounds.y;
            leftBounds.height = parentBounds.height;
            SectionBounds endBounds = new SectionBounds();
            endBounds.y = parentBounds.y;
            endBounds.height = parentBounds.height;
            
            
            // TODO Add checks that the section isn't abnormal shape and handle edge cases
            
            if (section.roomDirection == Direction.North || section.roomDirection == Direction.South)
            {
                bool upwards = section.roomDirection == Direction.North;
                // Either start at the bottom wall or choose somewhere a room's distance away
                int minZ, maxZ;
                
                if (upwards)
                {
                    // Room extends from bottom boundary
                    minZ = parentBounds.minZ;
                    maxZ = Random.Range(parentBounds.minZ + minimumMutators.roomSize + 1, 
                        parentBounds.maxZ - minimumMutators.roomSize);
                }
                else
                {
                    // Room extends from top boundary
                    maxZ = parentBounds.maxZ;
                    minZ = Random.Range(parentBounds.maxZ - minimumMutators.roomSize - 1, 
                        parentBounds.minZ + minimumMutators.roomSize);
                }

                corridorBounds.minZ = minZ;
                corridorBounds.maxZ = maxZ;
                corridorBounds.minX = (parentBounds.minX + parentBounds.maxX) / 2 - minimumMutators.corridorSize / 2;  
                //corridorBounds.minX = Random.Range(parentBounds.minX + minimumMutators.roomSize + 1, 
                //    parentBounds.maxX - minimumMutators.roomSize - minimumMutators.corridorSize - 1);
                corridorBounds.maxX = corridorBounds.minX + minimumMutators.corridorSize;
                
                rightBounds.minZ = minZ;
                rightBounds.maxZ = maxZ;
                rightBounds.minX = upwards ? corridorBounds.maxX + 1 : parentBounds.minX;
                rightBounds.maxX = upwards ? parentBounds.maxX : corridorBounds.minX - 1;
                
                leftBounds.minZ = minZ;
                leftBounds.maxZ = maxZ;
                leftBounds.minX = upwards ? parentBounds.minX : corridorBounds.maxX + 1;
                leftBounds.maxX = upwards ? corridorBounds.minX - 1 : parentBounds.maxX;
                
                endBounds.minZ = upwards ? maxZ + 1 : parentBounds.minZ;
                endBounds.maxZ = upwards ? parentBounds.maxZ : minZ - 1;;
                endBounds.minX = parentBounds.minX;
                endBounds.maxX = parentBounds.maxX;
            }
            else if (section.roomDirection == Direction.West || section.roomDirection == Direction.East)
            {
                bool rightwards = section.roomDirection == Direction.East;
                // Either start at the left wall or choose somewhere a room's distance away
                int minX, maxX;
    
                if (rightwards)
                {
                    // Room extends from left boundary
                    minX = parentBounds.minX;
                    maxX = Random.Range(parentBounds.minX + minimumMutators.roomSize + 1, 
                        parentBounds.maxX - minimumMutators.roomSize);
                }
                else
                {
                    // Room extends from right boundary
                    maxX = parentBounds.maxX;
                    minX = Random.Range(parentBounds.maxX - minimumMutators.roomSize - 1,
                        parentBounds.minX + minimumMutators.roomSize);
                }

                corridorBounds.minX = minX;
                corridorBounds.maxX = maxX;
                corridorBounds.minZ = (parentBounds.minZ + parentBounds.maxZ) / 2 - minimumMutators.corridorSize / 2; 
                //corridorBounds.minZ = Random.Range(parentBounds.minZ + minimumMutators.roomSize + 1, 
                //    parentBounds.maxZ - minimumMutators.roomSize - minimumMutators.corridorSize - 1);
                corridorBounds.maxZ = corridorBounds.minZ + minimumMutators.corridorSize;
    
                rightBounds.minX = minX;
                rightBounds.maxX = maxX;
                rightBounds.minZ = rightwards ? corridorBounds.maxZ + 1 : parentBounds.minZ;
                rightBounds.maxZ = rightwards ? parentBounds.maxZ : corridorBounds.minZ - 1;
    
                leftBounds.minX = minX;
                leftBounds.maxX = maxX;
                leftBounds.minZ = rightwards ? parentBounds.minZ : corridorBounds.maxZ + 1;
                leftBounds.maxZ = rightwards ? corridorBounds.minZ - 1 : parentBounds.maxZ;
    
                endBounds.minX = rightwards ? maxX + 1 : parentBounds.minX;
                endBounds.maxX = rightwards ? parentBounds.maxX : minX - 1;
                endBounds.minZ = parentBounds.minZ;
                endBounds.maxZ = parentBounds.maxZ;
            }
            else
            {
                Debug.LogError("Section direction is not valid");
                return null;
            }
            
            Section corridorSection = new Section();
            corridorSection.parent = section;
            corridorSection.position = corridorBounds.GetPosition();
            corridorSection.size = corridorBounds.GetSize();
            corridorSection.startFloor = floor;
            corridorSection.endFloor = floor;
            corridorSection.roomDirection = section.roomDirection;
            corridorSection.isCorridor = true;
            section.children.Add(corridorSection);
            rooms.Add(corridorSection);
            
            Section endSection = new Section();
            endSection.parent = section;
            endSection.position = endBounds.GetPosition();
            endSection.size = endBounds.GetSize();
            endSection.startFloor = floor;
            endSection.endFloor = floor;
            endSection.roomDirection = section.roomDirection;
            section.children.Add(endSection);
            
            Section rightSection = new Section();
            rightSection.parent = section;
            rightSection.position = rightBounds.GetPosition();
            rightSection.size = rightBounds.GetSize();
            rightSection.startFloor = floor;
            rightSection.endFloor = floor;
            if (section.roomDirection == Direction.North || section.roomDirection == Direction.South)
                rightSection.roomDirection = ClockWise(section.roomDirection);
            else
                rightSection.roomDirection = CounterClockWise(section.roomDirection);
            section.children.Add(rightSection);
            
            Section leftSection = new Section();
            leftSection.parent = section;
            leftSection.position = leftBounds.GetPosition();
            leftSection.size = leftBounds.GetSize();
            leftSection.startFloor = floor;
            leftSection.endFloor = floor;
            if (section.roomDirection == Direction.North || section.roomDirection == Direction.South)
                leftSection.roomDirection = CounterClockWise(section.roomDirection);
            else
                leftSection.roomDirection = ClockWise(section.roomDirection);
            section.children.Add(leftSection);

            for (int i = 0; i < section.children.Count; i++)
                if(!section.children[i].isCorridor)
                    section.children[i] = SubdivideSection(section.children[i], floor);
            
            return section;
        }
        else
        {
            // TODO add logic to expand into multiple floors
            section.isRoom = true;
            rooms.Add(section);
            return section;
        }
        
    }
    
    private Door CreateDoor(Section section)
    {
        switch (section.roomDirection)
        {
            case Direction.South:
                return new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x / 2 - minimumMutators.doorSize / 2, 
                        section.position.y, section.position.z + section.size.z),
                    size = new Vector3Int(minimumMutators.doorSize, minimumMutators.floorHeight, 1)
                };
            case Direction.North:
                return new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x / 2 - minimumMutators.doorSize / 2,
                        section.position.y, section.position.z - 1),
                    size = new Vector3Int(minimumMutators.doorSize, minimumMutators.floorHeight, 1)
                };
            case Direction.East:
                return new Door()
                {
                    position = new Vector3Int(section.position.x - 1, section.position.y, 
                        section.position.z + section.size.z / 2 - minimumMutators.doorSize / 2),
                    size = new Vector3Int(1, minimumMutators.floorHeight, minimumMutators.doorSize)
                };
            case Direction.West:
                return new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x, section.position.y,
                        section.position.z + section.size.z / 2 - minimumMutators.doorSize / 2),
                    size = new Vector3Int(1, minimumMutators.floorHeight, minimumMutators.doorSize)
                };
            default:
                return new Door();
        }
    }

    private Direction ClockWise(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return Direction.East;
            case Direction.East:
                return Direction.South;
            case Direction.South:
                return Direction.West;
            case Direction.West:
                return Direction.North;
            default:
                return direction;
        }
    }

    private Direction CounterClockWise(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return Direction.West;
            case Direction.West:
                return Direction.South;
            case Direction.South:
                return Direction.East;
            case Direction.East:
                return Direction.North;
            default:
                return direction;
        }
    }

}
