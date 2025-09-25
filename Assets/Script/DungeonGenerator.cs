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

    public struct SectionBounds
    {
        public SectionBounds(Vector3Int position, Vector3Int size)
        {
            maxX = position.x + size.x;
            maxZ = position.z + size.z;
            minX = position.x;
            minZ = position.z;
        }
        public int maxX;
        public int maxZ;
        public int minX;
        public int minZ;
    }
    
    public class Section
    {
        public Section parent;
        List<Section> children = new List<Section>();
        public Vector3Int position;
        public Vector3Int size;
        public uint startFloor;
        public uint endFloor;
        public Vector2Int roomDirection;

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
    List<Section> rooms = new List<Section>();
    
    public void GenerateDungeon(Vector3Int size, MinimumMutators minimumMutators)
    {
        this.minimumMutators = minimumMutators;
        minimumSectionSize = minimumMutators.roomSize * 2 + minimumMutators.corridorSize + minimumMutators.wallThickness * 2;
        
    }

    private void GenerateFloor(uint floor)
    {
        
        
    }

    private void SubdivideSection(ref Section section, uint floor)
    {
        if (section.CheckSectionCanBeDivide(minimumSectionSize))
        {
            SectionBounds parentBounds = new SectionBounds(section.position, section.size);
            SectionBounds corridor = new SectionBounds();
            SectionBounds rightRoom = new SectionBounds();
            SectionBounds leftRoom = new SectionBounds();
            SectionBounds endRoom = new SectionBounds();
            
            // TODO Add check that the section isn't abnormal shape and handle edge cases
            
            
            
            if (section.roomDirection.x == 0 && section.roomDirection.y != 0)
            {
                bool upwards = section.roomDirection.y == 1;
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
                    minZ = Random.Range(parentBounds.minZ + minimumMutators.roomSize + 1, 
                        parentBounds.maxZ - minimumMutators.roomSize);
                    maxZ = parentBounds.maxZ;
                }

                corridor.minZ = minZ;
                corridor.maxZ = maxZ;
                corridor.minX = Random.Range(minimumMutators.roomSize + 1, 
                    parentBounds.maxX - minimumMutators.roomSize - minimumMutators.corridorSize - 1);
                corridor.maxX = corridor.minX + minimumMutators.corridorSize;
                
                rightRoom.minZ = minZ;
                rightRoom.maxZ = maxZ;
                rightRoom.minX = upwards ? corridor.maxX + 1 : parentBounds.minX;
                rightRoom.maxX = !upwards ? corridor.minX - 1 : parentBounds.maxX;
                
                leftRoom.minZ = minZ;
                leftRoom.maxZ = maxZ;
                leftRoom.minX = !upwards ? corridor.maxX + 1 : parentBounds.minX;
                leftRoom.maxX = upwards ? corridor.minX - 1 : parentBounds.maxX;
                
                endRoom.minZ = upwards ? maxZ + 1 : parentBounds.minZ;
                endRoom.maxZ = !upwards ? minZ - 1 : parentBounds.maxZ;
                endRoom.minX = parentBounds.minX;
                endRoom.maxX = parentBounds.maxX;
            }
            else if (section.roomDirection.x != 0 && section.roomDirection.y == 0)
            {
                bool rightwards = section.roomDirection.x == 1;
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
                    minX = Random.Range(parentBounds.minX + minimumMutators.roomSize + 1, 
                        parentBounds.maxX - minimumMutators.roomSize);
                    maxX = parentBounds.maxX;
                }

                corridor.minX = minX;
                corridor.maxX = maxX;
                corridor.minZ = Random.Range(minimumMutators.roomSize + 1, 
                    parentBounds.maxZ - minimumMutators.roomSize - minimumMutators.corridorSize - 1);
                corridor.maxZ = corridor.minZ + minimumMutators.corridorSize;
    
                rightRoom.minX = minX;
                rightRoom.maxX = maxX;
                rightRoom.minZ = rightwards ? corridor.maxZ + 1 : parentBounds.minZ;
                rightRoom.maxZ = !rightwards ? corridor.minZ - 1 : parentBounds.maxZ;
    
                leftRoom.minX = minX;
                leftRoom.maxX = maxX;
                leftRoom.minZ = !rightwards ? corridor.maxZ + 1 : parentBounds.minZ;
                leftRoom.maxZ = rightwards ? corridor.minZ - 1 : parentBounds.maxZ;
    
                endRoom.minX = rightwards ? maxX + 1 : parentBounds.minX;
                endRoom.maxX = !rightwards ? minX - 1 : parentBounds.maxX;
                endRoom.minZ = parentBounds.minZ;
                endRoom.maxZ = parentBounds.maxZ;
            }
            else
            {
                Debug.LogError("Section direction is not valid");
                return;
            }
            
        }
        else
        {
            rooms.Add(section);
        }

    }
    
    private int GetHeightFromFloor(int floor)
    {
        
    }

}
