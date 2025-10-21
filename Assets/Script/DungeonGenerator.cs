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
        public int floorThickness;
    }
    
    public enum Direction
    {
        North,
        South,
        West,
        East
    }
    
    static bool RoomDirectionVertical(Direction direction)
    {
        return direction == Direction.North || direction == Direction.South;
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
            YPos = position.y;
            height = size.y;
        }

        public SectionBounds(int yPos, int height)
        {
            maxX = 0;
            maxZ = 0;
            minX = 0;
            minZ = 0;
            this.YPos = yPos;
            this.height = height;
        }
        
        public int maxX;
        public int maxZ;
        public int minX;
        public int minZ;
        public int YPos;
        public int height;

        public Vector3Int GetPosition()
        {
            return new Vector3Int(minX, YPos, minZ);
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
            if (RoomDirectionVertical(direction))
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
            if (RoomDirectionVertical(direction))
                return new SectionBounds(position, new Vector3Int(width, height, depth));
            else
                return new SectionBounds(position, new Vector3Int(depth, height, width));
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

        public Section()
        {
            this.position = Vector3Int.zero;
            this.size = Vector3Int.zero;
            this.startFloor = 0;
            this.endFloor = 0;
            this.roomDirection = Direction.North;
            this.isRoom = false;
            this.isCorridor = false;
            this.parent = null;
        }
        public Section(Section other, bool copyParent = true, bool copyChildren = false)
        {
            this.position = other.position;
            this.size = other.size;
            this.startFloor = other.startFloor;
            this.endFloor = other.endFloor;
            this.roomDirection = other.roomDirection;
            this.isRoom = other.isRoom;
            this.isCorridor = other.isCorridor;
            this.children = copyChildren ? new List<Section>(other.children) : new List<Section>();
            this.parent = copyParent ? other.parent : null;
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
    }

    private static UInt64 calls = 0;
    public MinimumMutators minimums;
    int minimumSectionSize;
    public List<Section> rooms = new List<Section>();
    public List<Section> floors = new List<Section>(); // In case I want to retroactively make changes to the dungeon
    public List<Door> doors = new List<Door>();
    public float deepRatio = 0.9f;
    
    public void GenerateDungeon(Vector3Int size, MinimumMutators minimumMutators)
    {
        this.minimums = minimumMutators;
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
        mainSection.position.y = floor * minimums.floorHeight;
        mainSection.size = floorSize;
        mainSection.size.y -= minimums.floorThickness;
        mainSection.startFloor = floor;
        mainSection.endFloor = floor;
        mainSection.roomDirection = Direction.North;
        mainSection = SubdivideSection(mainSection, floor);
        floors.Add(mainSection);
    }

    private Section SubdivideSection(Section section, int floor)
    {
        calls++;
        if (calls >= 1000)
            Debug.Log("Calls: " + calls);
        try
        {
            if (section.CheckSectionCanBeDivide(minimumSectionSize))
            {
              return ThreeSectionDivide(section, floor);
            }
            else
            {
              // TODO add logic to expand into multiple floors
              section.isRoom = true;
              rooms.Add(section);
              return section;
            }  
        }
        catch (Exception e)
        {
            Debug.Log(e + " Subdivide calls: " + calls);;
            return section;
        }
    }
    
    private Door CreateDoor(Section section)
    {
        int wallThickness = minimums.wallThickness;
        switch (section.roomDirection)
        {
            case Direction.South:
                return new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x / 2 - minimums.doorSize / 2, 
                        section.position.y, section.position.z + section.size.z),
                    size = new Vector3Int(minimums.doorSize, minimums.floorHeight, wallThickness)
                };
            case Direction.North:
                return new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x / 2 - minimums.doorSize / 2,
                        section.position.y, section.position.z - wallThickness),
                    size = new Vector3Int(minimums.doorSize, minimums.floorHeight, wallThickness)
                };
            case Direction.East:
                return new Door()
                {
                    position = new Vector3Int(section.position.x - wallThickness, section.position.y, 
                        section.position.z + section.size.z / 2 - minimums.doorSize / 2),
                    size = new Vector3Int(wallThickness, minimums.floorHeight, minimums.doorSize)
                };
            case Direction.West:
                return new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x, section.position.y,
                        section.position.z + section.size.z / 2 - minimums.doorSize / 2),
                    size = new Vector3Int(wallThickness, minimums.floorHeight, minimums.doorSize)
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

    private Section BroomDivide(Section section)
    {
        Section corridorSection = new Section(section);
        corridorSection.parent = section;
        Section broomSection = new Section(section);
        broomSection.parent = section;
        
        SectionBounds parentBounds = new SectionBounds(section.position, section.size);
        Space space = new Space(parentBounds, section.roomDirection);
        int corridorAndWall = minimums.corridorSize + minimums.wallThickness;
        int broomAndWall = space.depth - minimums.corridorSize;
        // TODO Fix this
        bool isVertical = RoomDirectionVertical(section.roomDirection);
        if (isVertical)
        {
            corridorSection.size -= new Vector3Int(0, 0, broomAndWall);
            broomSection.size -= new Vector3Int(0, 0, corridorAndWall);
            if (section.roomDirection == Direction.North)
            {
                broomSection.position += new Vector3Int(0, 0, corridorAndWall);
            }
            else
            {
                corridorSection.position += new Vector3Int(0, 0, broomAndWall);
            }
        }
        else
        {
            corridorSection.size -= new Vector3Int(broomAndWall, 0, 0);
            broomSection.size -= new Vector3Int(corridorAndWall, 0, 0);
            if (section.roomDirection == Direction.East)
            {
                broomSection.position += new Vector3Int(corridorAndWall, 0, 0);
            }
            else
            {
                corridorSection.position += new Vector3Int(broomAndWall, 0, 0);
            }
        }
        
        broomSection = BufferDivide(broomSection);
        corridorSection.isCorridor = true;
        section.children.Add(corridorSection);
        section.children.AddRange(broomSection.children);
        return section;
    }
 
    private Section BufferDivide(Section section)
    {
        // TODO Add checks to dectect problems here
        SectionBounds parentBounds = new SectionBounds(section.position, section.size);
        Space space = new Space(parentBounds, section.roomDirection);
        int subDivisions = WideSubDiv(space);
        List<SectionBounds> subSections = new List<SectionBounds>();
        
        int subDivSize = space.width / subDivisions;
        
        bool isVertical = RoomDirectionVertical(section.roomDirection);
        for (int i = 0; i < subDivisions - 1; i++)
        {
            Vector3Int posOffset = new Vector3Int(i * (subDivSize + minimums.corridorSize), 0, 0);
            Vector3Int sizeOffset = new Vector3Int(subDivSize, parentBounds.height, space.depth);
            Vector3Int position = parentBounds.GetPosition() + (isVertical ? ZYXSwizzle(posOffset) : posOffset);
            Vector3Int size = position + (isVertical ? ZYXSwizzle(sizeOffset) : sizeOffset);
            SectionBounds bounds = new SectionBounds(position, size);
            subSections.Add(bounds);
        }

        int spaceUsed = (subDivisions - 1) * (subDivSize + minimums.corridorSize);
        int spaceLeft = space.width - spaceUsed;
        Vector3Int lastDivPosOffset = new Vector3Int(spaceUsed, 0, 0);
        Vector3Int lastDivPosition = parentBounds.GetPosition() + (isVertical ? ZYXSwizzle(lastDivPosOffset) : lastDivPosOffset);
        Vector3Int lastDivSizeOffset = new Vector3Int(spaceLeft, parentBounds.height, space.depth);
        Vector3Int lastDivSize = isVertical ? ZYXSwizzle(lastDivSizeOffset) : lastDivSizeOffset;
        SectionBounds lastDivBounds = new SectionBounds(lastDivPosition, lastDivSize);
        subSections.Add(lastDivBounds);

        foreach (SectionBounds bounds in subSections)
        {
            Section subSection = new Section(section);
            subSection.position = bounds.GetPosition();
            subSection.size = bounds.GetSize();
            section.children.Add(subSection);
        }
        
        return section;
    }

    private int WideSubDiv(Space space, float entropy = 0)
    {
        int subDivByDepth = space.width / (space.depth + minimums.wallThickness);
        int subDivByMin = space.width / (minimums.roomSize + minimums.wallThickness);
        int randomSubDiv = Random.Range(subDivByMin, subDivByDepth);
        int entropicSubDiv = Mathf.RoundToInt(Mathf.Lerp(subDivByDepth, randomSubDiv, entropy));
        return entropicSubDiv;
    }

    static private Vector3Int ZYXSwizzle(Vector3Int vector)
    {
        return new Vector3Int(vector.z, vector.y, vector.x);
    }
    
    private Section ThreeSectionDivide(Section section, int floor)
    { 
        SectionBounds parentBounds = new SectionBounds(section.position, section.size);
        SectionBounds corridorBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        SectionBounds rightBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        SectionBounds leftBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        SectionBounds endBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        
        int wallThickness = minimums.wallThickness;
        bool isDeep = false;
        
        if (RoomDirectionVertical(section.roomDirection))
        {
            isDeep = ((float)parentBounds.GetSize().z / parentBounds.GetSize().x) > deepRatio;
            bool isZPositive = section.roomDirection == Direction.North;
            // Either start at the bottom wall or choose somewhere a room's distance away
            int minZ, maxZ;
            
            if (isZPositive)
            {
                // Room extends from bottom boundary
                minZ = parentBounds.minZ;
                if (isDeep)
                {
                    // Push the end of the corridor back to force subsections to be more square
                    int squaringDepth = parentBounds.minZ + parentBounds.GetSize().z / 2;
                    maxZ = Mathf.Clamp(squaringDepth, parentBounds.minZ + minimums.roomSize + wallThickness, 
                        parentBounds.maxZ - minimums.roomSize);
                }
                else
                {
                    maxZ = parentBounds.maxZ;
                }
            }
            else
            {
                // Room extends from top boundary
                maxZ = parentBounds.maxZ;

                if (isDeep)
                {
                    // Push the end of the corridor back to force subsections to be more square
                    int squaringDepth = parentBounds.minZ + parentBounds.GetSize().z / 2;
                    minZ = Mathf.Clamp(squaringDepth, parentBounds.minZ + minimums.roomSize, 
                        parentBounds.maxZ - minimums.roomSize - wallThickness);
                }
                else
                {
                    minZ = parentBounds.minZ;   
                }
            }

            corridorBounds.minZ = minZ;
            corridorBounds.maxZ = maxZ;
            corridorBounds.minX = (parentBounds.minX + parentBounds.maxX) / 2 - minimums.corridorSize / 2;  
            corridorBounds.maxX = corridorBounds.minX + minimums.corridorSize;
            
            rightBounds.minZ = minZ;
            rightBounds.maxZ = maxZ;
            rightBounds.minX = isZPositive ? corridorBounds.maxX + wallThickness : parentBounds.minX;
            rightBounds.maxX = isZPositive ? parentBounds.maxX : corridorBounds.minX - wallThickness;
            
            leftBounds.minZ = minZ;
            leftBounds.maxZ = maxZ;
            leftBounds.minX = isZPositive ? parentBounds.minX : corridorBounds.maxX + wallThickness;
            leftBounds.maxX = isZPositive ? corridorBounds.minX - wallThickness : parentBounds.maxX;

            if (isDeep)
            {
                endBounds.minZ = isZPositive ? maxZ + wallThickness : parentBounds.minZ;
                endBounds.maxZ = isZPositive ? parentBounds.maxZ : minZ - wallThickness;;
                endBounds.minX = parentBounds.minX;
                endBounds.maxX = parentBounds.maxX;
            }            
        }
        else if (!RoomDirectionVertical(section.roomDirection))
        {
            isDeep = ((float)parentBounds.GetSize().x / parentBounds.GetSize().z) > deepRatio;
            bool isXPositive = section.roomDirection == Direction.East;
            // Either start at the left wall or choose somewhere a room's distance away
            int minX, maxX;

            if (isXPositive)
            {
                // Room extends from left boundary
                minX = parentBounds.minX;
                if (isDeep)
                {
                    // Push the end of the corridor back to force subsections to be more square
                    int squaringDepth = parentBounds.minX + parentBounds.GetSize().x / 2;
                    maxX = Mathf.Clamp(squaringDepth, parentBounds.minX + minimums.roomSize + wallThickness, 
                        parentBounds.maxX - minimums.roomSize);
                }
                else
                {
                    maxX = parentBounds.maxX;   
                }
            }
            else
            {
                // Room extends from right boundary
                maxX = parentBounds.maxX;
                if (isDeep)
                {
                    // Push the end of the corridor back to force subsections to be more square
                    int squaringDepth = parentBounds.minX + parentBounds.GetSize().x / 2;
                    minX = Mathf.Clamp(squaringDepth, parentBounds.minX + minimums.roomSize, 
                        parentBounds.maxX - minimums.roomSize - wallThickness);
                }
                else
                {
                    minX = parentBounds.minX;
                }
            }

            corridorBounds.minX = minX;
            corridorBounds.maxX = maxX;
            corridorBounds.minZ = (parentBounds.minZ + parentBounds.maxZ) / 2 - minimums.corridorSize / 2; 
            corridorBounds.maxZ = corridorBounds.minZ + minimums.corridorSize;

            rightBounds.minX = minX;
            rightBounds.maxX = maxX;
            rightBounds.minZ = isXPositive ? corridorBounds.maxZ + wallThickness : parentBounds.minZ;
            rightBounds.maxZ = isXPositive ? parentBounds.maxZ : corridorBounds.minZ - wallThickness;

            leftBounds.minX = minX;
            leftBounds.maxX = maxX;
            leftBounds.minZ = isXPositive ? parentBounds.minZ : corridorBounds.maxZ + wallThickness;
            leftBounds.maxZ = isXPositive ? corridorBounds.minZ - wallThickness : parentBounds.maxZ;
            
            if (isDeep)
            {
                endBounds.minX = isXPositive ? maxX + wallThickness : parentBounds.minX;
                endBounds.maxX = isXPositive ? parentBounds.maxX : minX - wallThickness;
                endBounds.minZ = parentBounds.minZ;
                endBounds.maxZ = parentBounds.maxZ;
            }
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
        
        Space rightSpace = new Space(rightBounds, rightSection.roomDirection);
        if(rightSpace.width > 2 * rightSpace.depth + minimums.wallThickness && rightSpace.depth > minimums.roomSize)
        {
            rightSection = BufferDivide(rightSection);
            section.children.AddRange(rightSection.children);   
        }
        else
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
        
        Space leftSpace = new Space(leftBounds, leftSection.roomDirection);
        if(leftSpace.width > 2 * leftSpace.depth + minimums.wallThickness && leftSpace.depth > minimums.roomSize)
        {
            leftSection = BufferDivide(leftSection);
            section.children.AddRange(leftSection.children);   
        }
        else
            section.children.Add(leftSection);
        
        if(isDeep)
        {
            Section endSection = new Section();
            endSection.parent = section;
            endSection.position = endBounds.GetPosition();
            endSection.size = endBounds.GetSize();
            endSection.startFloor = floor;
            endSection.endFloor = floor;
            endSection.roomDirection = section.roomDirection;
            
            Space endSpace = new Space(endBounds, endSection.roomDirection);
            if(endSpace.width > 2 * endSpace.depth + minimums.wallThickness && endSpace.depth > 
               minimums.corridorSize + minimums.wallThickness + minimums.roomSize)
            {
                endSection = BroomDivide(endSection);
                section.children.AddRange(endSection.children);
            }
            else
                section.children.Add(endSection);
        }

        for (int i = 0; i < section.children.Count; i++)
            if(!section.children[i].isCorridor)
                section.children[i] = SubdivideSection(section.children[i], floor);
        
        return section;
    }
    
}
