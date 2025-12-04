using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static Directions;
using static Walls;
using static Helpers;
using static Doors;

public static class Sections
{
    public enum DivisionType
    {
        None,
        Broom,
        Buffer,
        ThreeSection,
        Macro,
        BufferedQuad
    }
    
    public struct SectionBounds
    {
        public SectionBounds(Vector3Int position, Vector3Int size)
        {
            maxX = position.x + size.x;
            maxZ = position.z + size.z;
            minX = position.x;
            minZ = position.z;
            yPos = position.y;
            height = size.y;
        }

        public SectionBounds(int yPos, int height)
        {
            maxX = 0;
            maxZ = 0;
            minX = 0;
            minZ = 0;
            this.yPos = yPos;
            this.height = height;
        }
        
        public int maxX;
        public int maxZ;
        public int minX;
        public int minZ;
        public int yPos;
        public int height;

        public Vector3Int GetPosition()
        {
            return new Vector3Int(minX, yPos, minZ);
        }
        
        public Vector3Int GetSize()
        {
            return new Vector3Int(maxX - minX, height, maxZ - minZ);
        }
    }

    public struct Space
    {
        public int width;
        public int depth;

        public Space(SectionBounds bounds, Direction direction)
        {
            if (DirectionIsVertical(direction))
            {
                width = bounds.GetSize().x;
                depth = bounds.GetSize().z;
            }
            else
            {
                width = bounds.GetSize().z;
                depth = bounds.GetSize().x;
            }
        }

        public SectionBounds GetBounds(Direction direction, Vector3Int position, int height)
        {
            if (DirectionIsVertical(direction))
                return new SectionBounds(position, new Vector3Int(width, height, depth));
            else
                return new SectionBounds(position, new Vector3Int(depth, height, width));
        }
    }
    
    public class Section
    {
        public Section parent;
        public Section leadingRoom;
        public List<Section> children = new List<Section>();
        public Vector3Int position;
        public Vector3Int size;
        public int startFloor;
        public int endFloor;
        public Direction direction;
        public bool isRoom;
        public bool isCorridor;
        public bool isMacroMainCorridor = false;
        public bool isMacroSideCorridor = false;
        public int corridorOffset;
        public DivisionType divisionType = DivisionType.None;
        public int regionIndex = -1;
        public Door leadingDoor;
        
        public List<DoorOffset> northDoors;
        public List<DoorOffset> southDoors;
        public List<DoorOffset> eastDoors;
        public List<DoorOffset> westDoors;

        public Section()
        {
            this.position = Vector3Int.zero;
            this.size = Vector3Int.zero;
            this.startFloor = 0;
            this.endFloor = 0;
            this.direction = Direction.North;
            this.isRoom = false;
            this.isCorridor = false;
            this.parent = null;
            
            northDoors = new List<DoorOffset>();
            southDoors = new List<DoorOffset>();
            eastDoors = new List<DoorOffset>();
            westDoors = new List<DoorOffset>();
        }
        
        public Section(Section other, bool copyCorridorOffset = false, bool copyParent = true, bool copyChildren = false)
        {
            this.position = other.position;
            this.size = other.size;
            this.startFloor = other.startFloor;
            this.endFloor = other.endFloor;
            this.direction = other.direction;
            this.isRoom = other.isRoom;
            this.isCorridor = other.isCorridor;
            this.children = copyChildren ? new List<Section>(other.children) : new List<Section>();
            this.parent = copyParent ? other.parent : null;
            this.corridorOffset = copyCorridorOffset ? other.corridorOffset : 0;
            this.regionIndex = other.regionIndex;
            
            northDoors = new List<DoorOffset>();
            southDoors = new List<DoorOffset>();
            eastDoors = new List<DoorOffset>();
            westDoors = new List<DoorOffset>();
        }
            
        
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

        public Wall[] GetWalls(int doorHeight, int doorWidth)
        {
            // Calculate total wall count: 4 base walls + 2 additional segments per door
            int wallCount = 4 + northDoors.Count * 2 + southDoors.Count * 2 + eastDoors.Count * 2 + westDoors.Count * 2;
            Wall[] walls = new Wall[wallCount];
            
            northDoors.Sort();
            southDoors.Sort();
            eastDoors.Sort();
            westDoors.Sort();
            
            int wallIndex = 0;
            
            // North wall
            if (northDoors.Count > 0)
            {
                // Start wall from left edge to first door
                Vector3Int pos = position + new Vector3Int(0, 0, size.z);
                Vector3Int currentPos = pos + new Vector3Int(northDoors[0].offset - position.x, size.y, 0);
                walls[wallIndex++] = new Wall(pos, currentPos - pos, Direction.South);
                
                // Process each door
                for (int i = 0; i < northDoors.Count; i++)
                {
                    // Add wall segment above door
                    Vector3Int doorWallPos = pos + new Vector3Int(northDoors[i].offset - position.x, doorHeight, 0);
                    Vector3Int doorWallSize = new Vector3Int(doorWidth, size.y - doorHeight, 0);
                    walls[wallIndex++] = new Wall(doorWallPos, doorWallSize, Direction.South);
                    
                    // Add wall segment to next door or end
                    int nextX = (i + 1 < northDoors.Count) ? northDoors[i + 1].offset - position.x : size.x;
                    Vector3Int wallStart = pos + new Vector3Int(northDoors[i].offset - position.x + doorWidth, 0, 0);
                    Vector3Int wallEnd = pos + new Vector3Int(nextX, size.y, 0);
                    walls[wallIndex++] = new Wall(wallStart, wallEnd - wallStart, Direction.South);
                }
            }
            else
            {
                // Single wall segment if no doors
                Vector3Int pos = position + new Vector3Int(0, 0, size.z);
                walls[wallIndex++] = new Wall(pos, new Vector3Int(size.x, size.y, 0), Direction.South);
            }

            // South wall
            if (southDoors.Count > 0)
            {
                Vector3Int pos = position;
                Vector3Int currentPos = pos + new Vector3Int(southDoors[0].offset - position.x, size.y, 0);
                walls[wallIndex++] = new Wall(pos, currentPos - pos, Direction.North);
                
                for (int i = 0; i < southDoors.Count; i++)
                {
                    Vector3Int doorWallPos = pos + new Vector3Int(southDoors[i].offset - position.x, doorHeight, 0);
                    Vector3Int doorWallSize = new Vector3Int(doorWidth, size.y - doorHeight, 0);
                    walls[wallIndex++] = new Wall(doorWallPos, doorWallSize, Direction.North);
                    
                    int nextX = (i + 1 < southDoors.Count) ? southDoors[i + 1].offset - position.x : size.x;
                    Vector3Int wallStart = pos + new Vector3Int(southDoors[i].offset - position.x + doorWidth, 0, 0);
                    Vector3Int wallEnd = pos + new Vector3Int(nextX, size.y, 0);
                    walls[wallIndex++] = new Wall(wallStart, wallEnd - wallStart, Direction.North);
                }
            }
            else
            {
                walls[wallIndex++] = new Wall(position, new Vector3Int(size.x, size.y, 0), Direction.North);
            }

            // East wall
            if (eastDoors.Count > 0)
            {
                Vector3Int pos = position + new Vector3Int(size.x, 0, 0);
                Vector3Int currentPos = pos + new Vector3Int(0, size.y, eastDoors[0].offset - position.z);
                walls[wallIndex++] = new Wall(pos, currentPos - pos, Direction.West);
                
                for (int i = 0; i < eastDoors.Count; i++)
                {
                    Vector3Int doorWallPos = pos + new Vector3Int(0, doorHeight, eastDoors[i].offset - position.z);
                    Vector3Int doorWallSize = new Vector3Int(0, size.y - doorHeight, doorWidth);
                    walls[wallIndex++] = new Wall(doorWallPos, doorWallSize, Direction.West);
                    
                    int nextZ = (i + 1 < eastDoors.Count) ? eastDoors[i + 1].offset - position.z : size.z;
                    Vector3Int wallStart = pos + new Vector3Int(0, 0, eastDoors[i].offset - position.z + doorWidth);
                    Vector3Int wallEnd = pos + new Vector3Int(0, size.y, nextZ);
                    walls[wallIndex++] = new Wall(wallStart, wallEnd - wallStart, Direction.West);
                }
            }
            else
            {
                walls[wallIndex++] = new Wall(position + new Vector3Int(size.x, 0, 0), new Vector3Int(0, size.y, size.z), Direction.West);
            }

            // West wall
            if (westDoors.Count > 0)
            {
                Vector3Int pos = position;
                Vector3Int currentPos = pos + new Vector3Int(0, size.y, westDoors[0].offset - position.z);
                walls[wallIndex++] = new Wall(pos, currentPos - pos, Direction.East);
                
                for (int i = 0; i < westDoors.Count; i++)
                {
                    Vector3Int doorWallPos = pos + new Vector3Int(0, doorHeight, westDoors[i].offset - position.z);
                    Vector3Int doorWallSize = new Vector3Int(0, size.y - doorHeight, doorWidth);
                    walls[wallIndex++] = new Wall(doorWallPos, doorWallSize, Direction.East);
                    
                    int nextZ = (i + 1 < westDoors.Count) ? westDoors[i + 1].offset - position.z : size.z;
                    Vector3Int wallStart = pos + new Vector3Int(0, 0, westDoors[i].offset - position.z + doorWidth);
                    Vector3Int wallEnd = pos + new Vector3Int(0, size.y, nextZ);
                    walls[wallIndex++] = new Wall(wallStart, wallEnd - wallStart, Direction.East);
                }
            }
            else
            {
                walls[wallIndex++] = new Wall(position, new Vector3Int(0, size.y, size.z), Direction.East);
            }

            return walls;
        }

        public void RollToSetRegion(float entropy, List<Region> regions)
        {
            entropy = Mathf.Clamp(entropy, 0, 1);
            float inverseEntropy = 1 - entropy;

            if (regionIndex >= 0 || Random.Range(0.1f, 1) > inverseEntropy)
                return;
            
            List<int> availableRegions = new List<int>();

            for (int i = 0; i < regions.Count; i++)
                if (RangeContainsFloatInclusive(entropy, regions[i].minEntropy, regions[i].maxEntropy))
                    availableRegions.Add(i);
            
            Assert.IsTrue(availableRegions.Count > 0, "No regions available for entropy value of: " + entropy);
            
            regionIndex = availableRegions[Random.Range(0, availableRegions.Count)];
        }
    }
}
