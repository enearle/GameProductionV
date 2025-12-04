using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using static Sections;
using static Directions;
using static Walls;
using static Doors;
using static Helpers;


public class DungeonGenerator : MonoBehaviour
{
    [Serializable]
    public struct Specifications
    {
        public int roomSize;
        public int corridorSize;
        public int doorWidth;
        public int doorHeight;
        public int floorHeight;
        public float maxFloorUsage;
        public int wallThickness;
        public int floorThickness;
        public float entropyThreshold;
        public float macroThreshold;
        public List<Region> regions;
    }

    private static UInt64 calls = 0;
    public Specifications specs;
    int minimumSectionSize;
    public List<Section> rooms = new List<Section>();
    public List<Section> floors = new List<Section>(); // In case I want to retroactively make changes to the dungeon
    public List<Door> doors = new List<Door>();
    public float deepRatio = 0.9f;
    private int zeroPoint;
    private int maxPoint;
    private Direction startDirection;
    
    
    public void GenerateDungeon(Vector3Int size, Specifications specs, int seed, Direction startDirection)
    {
        SetSeed(seed);
        this.startDirection = startDirection;
        
        this.specs = specs;
        minimumSectionSize = specs.roomSize * 2 + specs.corridorSize + specs.wallThickness * 2;
        zeroPoint = specs.roomSize * 2 + specs.wallThickness;
        maxPoint = Mathf.RoundToInt(Mathf.Lerp(zeroPoint, Math.Min(size.x, size.z), specs.entropyThreshold));
        
        if (maxPoint < zeroPoint)
            Debug.LogError("Dungeon size is too small for minimum entropy");
        
        for (int i = 0; i < size.y / specs.floorHeight; i++)
        {
            Vector3Int floorSize = new Vector3Int(size.x, specs.floorHeight, size.z);
            GenerateFloor(i, floorSize);
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == 0)
                Debug.Log("Room 1: " + rooms[i].corridorOffset + " " + rooms[i].isCorridor);
            Door door = CreateDoor(rooms[i], specs);
            doors.Add(door);
            AddDoorToRoom(rooms[i], door);
        }
    }

    private void GenerateFloor(int floor, Vector3Int floorSize)
    {
        Section mainSection = new Section();
        mainSection.position = -floorSize / 2;
        mainSection.position.y = floor * specs.floorHeight;
        mainSection.size = floorSize;
        mainSection.size.y -= specs.floorThickness;
        mainSection.startFloor = floor;
        mainSection.endFloor = floor;
        mainSection.direction = startDirection;
        mainSection = SubdivideSection(mainSection);
        floors.Add(mainSection);
    }

    private Section SubdivideSection(Section section)
    {
        calls++;
        if (calls >= 5000)
        {
            Debug.Log("Calls: " + calls);
            return null;
        }
        
        float entropy = CalculateSectionEntropy(section, zeroPoint, maxPoint);
        bool finishEarly = LowEntropyRoll(entropy);
        bool canDivide = section.CheckSectionCanBeDivide(minimumSectionSize);
        if (section.regionIndex == -1 && specs.regions.Count > 0)
        {
            section.RollToSetRegion(entropy, specs.regions);
        }
        
        try
        {
            if (!finishEarly && canDivide)
            {
                if (CanMacroSubDivide(section))
                    return CrossMacroDivide(section);
                else
                    return ThreeSectionDivide(section, entropy);
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

    private Section BroomDivide(Section section, float entropy = 0)
    {
        section.divisionType = DivisionType.Broom;
        Section corridorSection = new Section(section, true);
        corridorSection.parent = section;
        corridorSection.isCorridor = true;
        corridorSection.leadingRoom = section.leadingRoom;
        Section broomSection = new Section(section);
        broomSection.parent = section;
        
        SectionBounds parentBounds = new SectionBounds(section.position, section.size);
        Sections.Space space = new Sections.Space(parentBounds, section.direction);
        int corridorAndWall = specs.corridorSize + specs.wallThickness;
        int broomAndWall = space.depth - specs.corridorSize;
        // TODO Fix this
        bool isVertical = DirectionIsVertical(section.direction);
        if (isVertical)
        {
            corridorSection.size -= new Vector3Int(0, 0, broomAndWall);
            broomSection.size -= new Vector3Int(0, 0, corridorAndWall);
            if (section.direction == Direction.North)
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
            if (section.direction == Direction.East)
            {
                broomSection.position += new Vector3Int(corridorAndWall, 0, 0);
            }
            else
            {
                corridorSection.position += new Vector3Int(broomAndWall, 0, 0);
            }
        }
        
        section.children.Add(corridorSection);
        rooms.Add(corridorSection);
        broomSection.leadingRoom = corridorSection;
        broomSection = BufferDivide(broomSection, entropy);
        section.children.AddRange(broomSection.children);
        return section;
    }
 
    private Section BufferDivide(Section section, float entropy = 0)
    {
        section.divisionType = DivisionType.Buffer;
        SectionBounds parentBounds = new SectionBounds(section.position, section.size);
        Sections.Space space = new Sections.Space(parentBounds, section.direction);
        int subDivisions = WideSubDiv(space, entropy);
        List<SectionBounds> subSections = new List<SectionBounds>();
        
        int totalWallSpace = (subDivisions - 1) * specs.wallThickness;
        int availableSpace = space.width - totalWallSpace;
        int subDivSize = availableSpace / subDivisions;
        
        bool isVertical = DirectionIsVertical(section.direction);
        for (int i = 0; i < subDivisions - 1; i++)
        {
            Vector3Int posOffset = new Vector3Int(i * (subDivSize + specs.wallThickness), 0, 0);
            Vector3Int sizeOffset = new Vector3Int(subDivSize, parentBounds.height, space.depth);
            Vector3Int position = parentBounds.GetPosition() + (isVertical ? posOffset : ZYXSwizzle(posOffset));
            Vector3Int size = isVertical ? sizeOffset : ZYXSwizzle(sizeOffset);
            SectionBounds bounds = new SectionBounds(position, size);
            subSections.Add(bounds);
        }

        int spaceUsed = (subDivisions - 1) * (subDivSize + specs.wallThickness);
        int spaceLeft = space.width - spaceUsed;
        Vector3Int lastDivPosOffset = new Vector3Int(spaceUsed, 0, 0);
        Vector3Int lastDivPosition = parentBounds.GetPosition() + (isVertical ? lastDivPosOffset : ZYXSwizzle(lastDivPosOffset));
        Vector3Int lastDivSizeOffset = new Vector3Int(spaceLeft, parentBounds.height, space.depth);
        Vector3Int lastDivSize = isVertical ? lastDivSizeOffset : ZYXSwizzle(lastDivSizeOffset);
        SectionBounds lastDivBounds = new SectionBounds(lastDivPosition, lastDivSize);
        subSections.Add(lastDivBounds);

        foreach (SectionBounds bounds in subSections)
        {
            Section subSection = new Section(section);
            subSection.parent = section;
            subSection.position = bounds.GetPosition();
            subSection.size = bounds.GetSize();
            subSection.leadingRoom = section.leadingRoom;
            section.children.Add(subSection);
        }
        
        return section;
    }

    private int WideSubDiv(Sections.Space space, float entropy = 0)
    {
        int maxSubDiv = (space.width + specs.wallThickness) / (specs.roomSize + specs.wallThickness);
        int subDivByDepth = Mathf.Min(maxSubDiv, space.width / (space.depth + specs.wallThickness));
        int subDivByMin = Mathf.Min(maxSubDiv, space.width / (specs.roomSize + specs.wallThickness));
    
        int randomSubDiv = Random.Range(subDivByMin, subDivByDepth);
        int entropicSubDiv = Mathf.FloorToInt(Mathf.Lerp(subDivByDepth, randomSubDiv, entropy));
        return Mathf.Max(1, entropicSubDiv);
    }

    static private Vector3Int ZYXSwizzle(Vector3Int vector)
    {
        return new Vector3Int(vector.z, vector.y, vector.x);
    }
    
    private Section ThreeSectionDivide(Section section, float entropy)
    {
        section.divisionType = DivisionType.ThreeSection;
        SectionBounds parentBounds = new SectionBounds(section.position, section.size);
        SectionBounds corridorBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        SectionBounds rightBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        SectionBounds leftBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        SectionBounds endBounds = new SectionBounds(parentBounds.YPos, parentBounds.height);
        
        int wallThickness = specs.wallThickness;
        bool isDeep = false;
        bool isVertical = DirectionIsVertical(section.direction);
        float depth = Random.Range(0.35f, 0.65f);
        depth = Mathf.Lerp(0.5f, depth, entropy);
        int corridorOffset;
        
        
        if (isVertical)
        {
            isDeep = ((float)parentBounds.GetSize().z / parentBounds.GetSize().x) > deepRatio;
            bool isZPositive = section.direction == Direction.North;
            // Either start at the bottom wall or choose somewhere a room's distance away
            int minZ, maxZ;
            if (isZPositive)
            {
                // Room extends from bottom boundary
                minZ = parentBounds.minZ;
                if (isDeep)
                {
                    // Push the end of the corridor back to force subsections to be more square
                    int squaringDepth = parentBounds.minZ + Mathf.RoundToInt(parentBounds.GetSize().z * depth);
                    maxZ = Mathf.Clamp(squaringDepth, parentBounds.minZ + specs.roomSize + wallThickness, 
                        parentBounds.maxZ - specs.roomSize);
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
                    int squaringDepth = parentBounds.minZ + Mathf.RoundToInt(parentBounds.GetSize().z * depth);
                    minZ = Mathf.Clamp(squaringDepth, parentBounds.minZ + specs.roomSize, 
                        parentBounds.maxZ - specs.roomSize - wallThickness);
                }
                else
                {
                    minZ = parentBounds.minZ;   
                }
            }

            corridorOffset = section.corridorOffset != 0 ? 
                section.corridorOffset : PlaceCorridorHorizontal(parentBounds.GetSize().x, entropy);

            corridorBounds.minZ = minZ;
            corridorBounds.maxZ = maxZ;
            corridorBounds.minX = (parentBounds.minX + parentBounds.maxX) / 2 - specs.corridorSize / 2;  
            corridorBounds.minX = parentBounds.minX + corridorOffset;  
            corridorBounds.maxX = corridorBounds.minX + specs.corridorSize;
            
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
        else
        {
            isDeep = ((float)parentBounds.GetSize().x / parentBounds.GetSize().z) > deepRatio;
            bool isXPositive = section.direction == Direction.East;
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
                    maxX = Mathf.Clamp(squaringDepth, parentBounds.minX + specs.roomSize + wallThickness, 
                        parentBounds.maxX - specs.roomSize);
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
                    minX = Mathf.Clamp(squaringDepth, parentBounds.minX + specs.roomSize, 
                        parentBounds.maxX - specs.roomSize - wallThickness);
                }
                else
                {
                    minX = parentBounds.minX;
                }
            }

            corridorOffset = section.corridorOffset != 0 ? 
                section.corridorOffset : PlaceCorridorHorizontal(parentBounds.GetSize().z, entropy);

            corridorBounds.minX = minX;
            corridorBounds.maxX = maxX;
            corridorBounds.minZ = parentBounds.minZ + corridorOffset;
            corridorBounds.maxZ = corridorBounds.minZ + specs.corridorSize;

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
        
        // TODO Replace this with entropic rng /////////////////////////////////////////////
        bool bufferSides = true;
        bool broomEnd = true;
        
        Section corridorSection = new Section();
        corridorSection.parent = section;
        corridorSection.position = corridorBounds.GetPosition();
        corridorSection.size = corridorBounds.GetSize();
        corridorSection.startFloor = section.startFloor;
        corridorSection.endFloor = section.startFloor;
        corridorSection.direction = section.direction;
        corridorSection.isCorridor = true;
        corridorSection.leadingRoom = section.leadingRoom;
        section.children.Add(corridorSection);
        rooms.Add(corridorSection);
        
        Section rightSection = new Section();
        rightSection.parent = section;
        rightSection.position = rightBounds.GetPosition();
        rightSection.size = rightBounds.GetSize();
        rightSection.startFloor = section.startFloor;
        rightSection.endFloor = section.startFloor; 
        rightSection.direction = isVertical ? ClockWise(section.direction) : CounterClockWise(section.direction);
        rightSection.leadingRoom = corridorSection;
        
        Sections.Space rightSpace = new Sections.Space(rightBounds, rightSection.direction);
        if(rightSpace.width > 2 * rightSpace.depth + specs.wallThickness && rightSpace.depth > specs.roomSize && bufferSides)
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
        leftSection.startFloor = section.startFloor;
        leftSection.endFloor = section.startFloor;
        leftSection.direction = isVertical ? CounterClockWise(section.direction) : ClockWise(section.direction);
        leftSection.leadingRoom = corridorSection;
        
        Sections.Space leftSpace = new Sections.Space(leftBounds, leftSection.direction);
        if(leftSpace.width > 2 * leftSpace.depth + specs.wallThickness && leftSpace.depth > specs.roomSize && bufferSides)
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
            endSection.startFloor = section.startFloor;
            endSection.endFloor = section.startFloor;
            endSection.direction = section.direction;
            endSection.corridorOffset = corridorOffset;
            endSection.leadingRoom = corridorSection;
            
            Sections.Space endSpace = new Sections.Space(endBounds, endSection.direction);
            if(endSpace.width > 2 * endSpace.depth + specs.wallThickness && endSpace.depth > 
               specs.corridorSize + specs.wallThickness + specs.roomSize && broomEnd)
            {
                endSection = BroomDivide(endSection);
                section.children.AddRange(endSection.children);
            }
            else
                section.children.Add(endSection);
        }

        for (int i = 0; i < section.children.Count; i++)
            if(!section.children[i].isCorridor)
                section.children[i] = SubdivideSection(section.children[i]);
        
        return section;
    }

    private Section CrossMacroDivide(Section section)
    {
        section.divisionType = DivisionType.Macro;
        bool isVertical = DirectionIsVertical(section.direction);
        bool isOffset = section.corridorOffset != 0;
        
        Section mainCorridor = new Section(section);
        mainCorridor.leadingRoom = section.leadingRoom;
        mainCorridor.isCorridor = true;
        mainCorridor.direction = section.direction;
        Section corridorA = new Section(section);
        Section corridorB = new Section(section);
        Section topLeftQuadrant = new Section(section);
        Section topRightQuadrant = new Section(section);
        Section bottomLeftQuadrant = new Section(section);
        Section bottomRightQuadrant = new Section(section);
        mainCorridor.parent = section;
        
        if(isVertical)
        {
            Section rightCorridor = new Section(section);
            rightCorridor.direction = section.direction == Direction.North ? ClockWise(section.direction) : CounterClockWise(section.direction);
            rightCorridor.isCorridor = true;
            rightCorridor.leadingRoom = mainCorridor;
            Section leftCorridor = new Section(section);
            leftCorridor.direction = section.direction == Direction.North ? CounterClockWise(section.direction) : ClockWise(section.direction);
            leftCorridor.isCorridor = true;
            leftCorridor.leadingRoom = mainCorridor;
            
            int mainXOffset = isOffset ? section.corridorOffset : PlaceCorridorHorizontal(section.size.x, 0.5f);
            int leftZOffset = PlaceCorridorHorizontal(section.size.z, 0.5f);
            int rightZOffset = PlaceCorridorHorizontal(section.size.z, 0.5f);
            if (Mathf.Abs(leftZOffset - rightZOffset) < specs.roomSize * 2)
                leftZOffset = rightZOffset;
            
            mainCorridor.position = new Vector3Int(
                mainCorridor.position.x + mainXOffset,
                mainCorridor.position.y,
                mainCorridor.position.z
            );
            mainCorridor.size = new Vector3Int(
                specs.corridorSize, 
                mainCorridor.size.y, 
                mainCorridor.size.z
            );
            
            rightCorridor.position = new Vector3Int(
                mainCorridor.position.x + specs.corridorSize + specs.wallThickness,
                rightCorridor.position.y,
                rightCorridor.position.z + rightZOffset
            );
            rightCorridor.size = new Vector3Int(
                rightCorridor.size.x - (mainXOffset + specs.corridorSize + specs.wallThickness), 
                rightCorridor.size.y, 
                specs.corridorSize
            );

            leftCorridor.position = new Vector3Int(
                leftCorridor.position.x,
                leftCorridor.position.y,
                leftCorridor.position.z + leftZOffset
            );
            leftCorridor.size = new Vector3Int(
                mainXOffset - specs.wallThickness, 
                leftCorridor.size.y, 
                specs.corridorSize
            );

            topLeftQuadrant.position = new Vector3Int(
                topLeftQuadrant.position.x,
                topLeftQuadrant.position.y,
                leftCorridor.position.z + specs.corridorSize + specs.wallThickness
            );
            topLeftQuadrant.size = new Vector3Int(
                mainXOffset - specs.wallThickness,
                topLeftQuadrant.size.y,
                topLeftQuadrant.size.z - (leftZOffset + specs.corridorSize + specs.wallThickness)
            );

            topRightQuadrant.position = new Vector3Int(
                rightCorridor.position.x,
                topRightQuadrant.position.y,
                rightCorridor.position.z + specs.corridorSize + specs.wallThickness
            );
            topRightQuadrant.size = new Vector3Int(
                rightCorridor.size.x,
                topRightQuadrant.size.y,
                topRightQuadrant.size.z - (rightZOffset + specs.corridorSize + specs.wallThickness)
            );
            
            bottomLeftQuadrant.position = section.position;
            bottomLeftQuadrant.size = new Vector3Int(
                mainXOffset - specs.wallThickness,
                bottomLeftQuadrant.size.y,
                leftZOffset - specs.wallThickness
            );

            bottomRightQuadrant.position = new Vector3Int(
                rightCorridor.position.x,
                bottomRightQuadrant.position.y,
                bottomRightQuadrant.position.z
            );
            bottomRightQuadrant.size = new Vector3Int(
               rightCorridor.size.x,
                bottomRightQuadrant.size.y,
                rightZOffset - specs.wallThickness
            );

            corridorA = leftCorridor;
            corridorB = rightCorridor;
        }
        else
        {
            Section topCorridor = new Section(section);
            topCorridor.direction = section.direction == Direction.East ? CounterClockWise(section.direction) : ClockWise(section.direction);
            topCorridor.isCorridor = true;
            topCorridor.leadingRoom = mainCorridor;
            Section bottomCorridor = new Section(section);
            bottomCorridor.direction = section.direction == Direction.East ? ClockWise(section.direction) : CounterClockWise(section.direction);
            bottomCorridor.isCorridor = true;
            bottomCorridor.leadingRoom = mainCorridor;
            
            int mainZOffset = isOffset ? section.corridorOffset : PlaceCorridorHorizontal(section.size.z, 0.5f);
            int topXOffset = PlaceCorridorHorizontal(section.size.x, 0.5f);
            int bottomXOffset = PlaceCorridorHorizontal(section.size.x, 0.5f);
            if (Mathf.Abs(topXOffset - bottomXOffset) < specs.roomSize * 2)
                topXOffset = bottomXOffset;
            
            mainCorridor.position = new Vector3Int(
                mainCorridor.position.x,
                mainCorridor.position.y,
                mainCorridor.position.z + mainZOffset
            );
            mainCorridor.size = new Vector3Int(
                mainCorridor.size.x,
                mainCorridor.size.y,
                specs.corridorSize
            );

            topCorridor.position = new Vector3Int(
                topCorridor.position.x + topXOffset,
                topCorridor.position.y,
                mainCorridor.position.z + specs.corridorSize + specs.wallThickness
            );
            topCorridor.size = new Vector3Int(
                specs.corridorSize,
                topCorridor.size.y,
                topCorridor.size.z - (mainZOffset + specs.corridorSize + specs.wallThickness)
            );

            bottomCorridor.position = new Vector3Int(
                bottomCorridor.position.x + bottomXOffset,
                bottomCorridor.position.y,
                bottomCorridor.position.z
            );
            bottomCorridor.size = new Vector3Int(
                specs.corridorSize,
                bottomCorridor.size.y,
                mainZOffset - specs.wallThickness
            );
            
            topLeftQuadrant.position = new Vector3Int(
                topLeftQuadrant.position.x,
                topLeftQuadrant.position.y,
                topLeftQuadrant.position.z + mainZOffset + specs.corridorSize + specs.wallThickness
            );
            topLeftQuadrant.size = new Vector3Int(
                topXOffset - specs.wallThickness,
                topLeftQuadrant.size.y,
                topLeftQuadrant.size.z - (mainZOffset + specs.corridorSize + specs.wallThickness)
            );
            
            topRightQuadrant.position = new Vector3Int(
                topRightQuadrant.position.x + topXOffset + specs.corridorSize + specs.wallThickness,
                topRightQuadrant.position.y,
                topRightQuadrant.position.z + mainZOffset + specs.corridorSize + specs.wallThickness
            );
            topRightQuadrant.size = new Vector3Int(
                topRightQuadrant.size.x - (topXOffset + specs.corridorSize + specs.wallThickness),
                topRightQuadrant.size.y,
                topRightQuadrant.size.z - (mainZOffset + specs.corridorSize + specs.wallThickness)
            );

            bottomLeftQuadrant.position = section.position;
            bottomLeftQuadrant.size = new Vector3Int(
                bottomXOffset - specs.wallThickness,
                bottomLeftQuadrant.size.y,
                mainZOffset - specs.wallThickness
            );

            bottomRightQuadrant.position = new Vector3Int(
                bottomRightQuadrant.position.x + bottomXOffset + specs.corridorSize + specs.wallThickness,
                bottomRightQuadrant.position.y,
                bottomRightQuadrant.position.z
            );
            bottomRightQuadrant.size = new Vector3Int(
                bottomRightQuadrant.size.x - (bottomXOffset + specs.corridorSize + specs.wallThickness),
                bottomRightQuadrant.size.y,
                mainZOffset - specs.wallThickness
            );
            
            corridorA = topCorridor;
            corridorB = bottomCorridor;
        }
        
        corridorA.parent = section;
        corridorB.parent = section;
        corridorA.isMacroSideCorridor = true;
        corridorB.isMacroSideCorridor = true;
        mainCorridor.isMacroMainCorridor = true;
        topLeftQuadrant.parent = section;
        topRightQuadrant.parent = section;
        bottomLeftQuadrant.parent = section;
        bottomRightQuadrant.parent = section;
        
        
        if (isVertical)
        {
            topLeftQuadrant = RandomBool()
                ? BufferQuadrant(topLeftQuadrant, Direction.North, Direction.West, corridorA, mainCorridor)
                : BufferQuadrant(topLeftQuadrant, Direction.West, Direction.North, mainCorridor, corridorA);

            topRightQuadrant = RandomBool()
                ? BufferQuadrant(topRightQuadrant, Direction.North, Direction.East, corridorB, mainCorridor)
                : BufferQuadrant(topRightQuadrant, Direction.East, Direction.North, mainCorridor, corridorB);

            bottomLeftQuadrant = RandomBool()
                ? BufferQuadrant(bottomLeftQuadrant, Direction.South, Direction.West, corridorA, mainCorridor)
                : BufferQuadrant(bottomLeftQuadrant, Direction.West, Direction.South, mainCorridor, corridorA);

            bottomRightQuadrant = RandomBool()
                ? BufferQuadrant(bottomRightQuadrant, Direction.South, Direction.East, corridorB, mainCorridor)
                : BufferQuadrant(bottomRightQuadrant, Direction.East, Direction.South, mainCorridor, corridorB);
        }
        else
        {
            topLeftQuadrant = RandomBool()
                ? BufferQuadrant(topLeftQuadrant, Direction.North, Direction.West, mainCorridor, corridorA)
                : BufferQuadrant(topLeftQuadrant, Direction.West, Direction.North, corridorA, mainCorridor);

            topRightQuadrant = RandomBool()
                ? BufferQuadrant(topRightQuadrant, Direction.North, Direction.East, mainCorridor, corridorA)
                : BufferQuadrant(topRightQuadrant, Direction.East, Direction.North, corridorA, mainCorridor);

            bottomLeftQuadrant = RandomBool()
                ? BufferQuadrant(bottomLeftQuadrant, Direction.South, Direction.West, mainCorridor, corridorB)
                : BufferQuadrant(bottomLeftQuadrant, Direction.West, Direction.South, corridorB, mainCorridor);

            bottomRightQuadrant = RandomBool()
                ? BufferQuadrant(bottomRightQuadrant, Direction.South, Direction.East, mainCorridor, corridorB)
                : BufferQuadrant(bottomRightQuadrant, Direction.East, Direction.South, corridorB, mainCorridor);
        }
        
        section.children.Add(mainCorridor);
        rooms.Add(mainCorridor);
        section.children.Add(corridorA);
        rooms.Add(corridorA);
        section.children.Add(corridorB);
        rooms.Add(corridorB);
        
        section.children.Add(topLeftQuadrant);
        section.children.Add(topRightQuadrant);
        section.children.Add(bottomLeftQuadrant);
        section.children.Add(bottomRightQuadrant);

        foreach (Section childSection in topLeftQuadrant.children)
            SubdivideSection(childSection);
        foreach (Section childSection in topRightQuadrant.children)
            SubdivideSection(childSection);
        foreach (Section childSection in bottomLeftQuadrant.children)
            SubdivideSection(childSection);
        foreach (Section childSection in bottomRightQuadrant.children)
            SubdivideSection(childSection);
        
        return section;
    }

    Section BufferQuadrant(Section section, Direction primaryDir, Direction secondaryDir, Section primaryLead, Section secondaryLead)
    {
        section.divisionType = DivisionType.BufferedQuad;
        Section mainSection = new Section(section);
        mainSection.direction = primaryDir;
        mainSection.parent = section;
        
        int xOffset = Mathf.Max(Mathf.FloorToInt(section.size.x * 0.2f), specs.roomSize + specs.wallThickness);
        int zOffset = Mathf.Max(Mathf.FloorToInt(section.size.z * 0.2f), specs.roomSize + specs.wallThickness);
        
        mainSection = ApplyDirectionalOffset(mainSection, primaryDir, xOffset, zOffset);
        mainSection = ApplyDirectionalOffset(mainSection, secondaryDir, xOffset, zOffset);
        
        Section secondaryBuffer = new Section(section);
        secondaryBuffer.direction = secondaryDir;
        secondaryBuffer.parent = section;
        secondaryBuffer.leadingRoom = secondaryLead;
        
        if (DirectionIsVertical(secondaryDir))
        {
            // Calculate z-position based on both primary and secondary directions
            int zPos = secondaryDir == Direction.North 
                ? secondaryBuffer.position.z
                : secondaryBuffer.position.z + mainSection.size.z + specs.wallThickness;
                
            // For vertical primary direction, adjust x-position
            if (DirectionIsVertical(primaryDir))
            {
                secondaryBuffer.position = new Vector3Int(
                    secondaryBuffer.position.x,
                    secondaryBuffer.position.y,
                    zPos
                );
            }
            // For horizontal primary direction, account for x offset
            else
            {
                secondaryBuffer.position = new Vector3Int(
                    primaryDir == Direction.East 
                        ? secondaryBuffer.position.x + xOffset 
                        : secondaryBuffer.position.x,
                    secondaryBuffer.position.y,
                    zPos
                );
            }
            
            secondaryBuffer.size = new Vector3Int(
                mainSection.size.x,
                secondaryBuffer.size.y,
                zOffset - specs.wallThickness
            );
        }
        else
        {
            // Calculate x-position based on both primary and secondary directions
            int xPos = secondaryDir == Direction.East
                ? secondaryBuffer.position.x
                : secondaryBuffer.position.x + mainSection.size.x + specs.wallThickness;
                
            // For horizontal primary direction, adjust z-position
            if (!DirectionIsVertical(primaryDir))
            {
                secondaryBuffer.position = new Vector3Int(
                    xPos,
                    secondaryBuffer.position.y,
                    secondaryBuffer.position.z
                );
            }
            // For vertical primary direction, account for z offset
            else
            {
                secondaryBuffer.position = new Vector3Int(
                    xPos,
                    secondaryBuffer.position.y,
                    primaryDir == Direction.North 
                        ? secondaryBuffer.position.z + zOffset 
                        : secondaryBuffer.position.z
                );
            }
            
            secondaryBuffer.size = new Vector3Int(
                xOffset - specs.wallThickness,
                secondaryBuffer.size.y,
                mainSection.size.z
            );
        }

        
        Section corridor = new Section(section);
        corridor.direction = primaryDir;
        corridor.isCorridor = true;
        corridor.parent = section;
        corridor.leadingRoom = primaryLead;
        Section leftBuffer = new Section(section);
        leftBuffer.direction = primaryDir;
        leftBuffer.parent = section;
        leftBuffer.leadingRoom = primaryLead;
        Section rightBuffer = new Section(section);
        rightBuffer.direction = primaryDir;
        rightBuffer.parent = section;
        rightBuffer.leadingRoom = primaryLead;
        

        if (DirectionIsVertical(primaryDir))
        {
            int eastOffset = secondaryDir == Direction.East ? xOffset : 0;
            int corridorXOffset = PlaceCorridorHorizontal(mainSection.size.x, 0.5f);
            corridor.position = new Vector3Int(
                corridor.position.x + corridorXOffset + eastOffset,
                corridor.position.y,
                primaryDir == Direction.North 
                    ? corridor.position.z 
                    : corridor.position.z + mainSection.size.z + specs.wallThickness
            );
            corridor.size = new Vector3Int(
                specs.corridorSize,
                corridor.size.y,
                zOffset - specs.wallThickness
            );

            leftBuffer.position = new Vector3Int(
                leftBuffer.position.x,
                leftBuffer.position.y,
                primaryDir == Direction.North
                    ? leftBuffer.position.z
                    : leftBuffer.position.z + mainSection.size.z + specs.wallThickness
            );
            leftBuffer.size = new Vector3Int(
                (corridorXOffset + eastOffset) - specs.wallThickness,
                leftBuffer.size.y,
                zOffset - specs.wallThickness
            );

            rightBuffer.position = new Vector3Int(
                rightBuffer.position.x + corridorXOffset + eastOffset + specs.corridorSize + specs.wallThickness,
                rightBuffer.position.y,
                primaryDir == Direction.North
                    ? rightBuffer.position.z
                    : rightBuffer.position.z + mainSection.size.z + specs.wallThickness
            );
            rightBuffer.size = new Vector3Int(
                rightBuffer.size.x - (corridorXOffset + eastOffset + specs.corridorSize + specs.wallThickness),
                rightBuffer.size.y,
                zOffset - specs.wallThickness
            );
            mainSection.corridorOffset = secondaryDir == Direction.East ? mainSection.size.x - corridorXOffset - specs.corridorSize : corridorXOffset;
        }
        else
        {
            int northOffset = secondaryDir == Direction.North ? zOffset : 0;
            int corridorZOffset = PlaceCorridorHorizontal(mainSection.size.z, 0.5f);
            corridor.position = new Vector3Int(
                primaryDir == Direction.East
                    ? corridor.position.x 
                    : corridor.position.x + mainSection.size.x + specs.wallThickness,
                corridor.position.y,
                corridor.position.z + (corridorZOffset + northOffset)
            );
            corridor.size = new Vector3Int(
                xOffset - specs.wallThickness,
                corridor.size.y,
                specs.corridorSize
            );

            leftBuffer.position = new Vector3Int(
                primaryDir == Direction.East
                    ? leftBuffer.position.x
                    : leftBuffer.position.x + mainSection.size.x + specs.wallThickness,
                leftBuffer.position.y,
                leftBuffer.position.z + corridorZOffset + northOffset + specs.corridorSize + specs.wallThickness
            );
            leftBuffer.size = new Vector3Int(
                xOffset - specs.wallThickness,
                leftBuffer.size.y,
                leftBuffer.size.z - (corridorZOffset + northOffset + specs.corridorSize + specs.wallThickness)
            );

            rightBuffer.position = new Vector3Int(
                primaryDir == Direction.East
                    ? rightBuffer.position.x
                    : rightBuffer.position.x + mainSection.size.x + specs.wallThickness,
                rightBuffer.position.y,
                rightBuffer.position.z
            );
            rightBuffer.size = new Vector3Int(
                xOffset - specs.wallThickness,
                rightBuffer.size.y,
                (corridorZOffset + northOffset) - specs.wallThickness
            );
            mainSection.corridorOffset = secondaryDir == Direction.North ? mainSection.size.z - corridorZOffset - specs.corridorSize : corridorZOffset;
        }
        
        if(!DirectionIsVertical(primaryDir) && secondaryDir == Direction.North)
            if(primaryDir == Direction.East)
                mainSection.corridorOffset = mainSection.size.z - (mainSection.corridorOffset + specs.corridorSize);
            else
                mainSection.corridorOffset = mainSection.size.z - (mainSection.corridorOffset + specs.corridorSize);
        else if(DirectionIsVertical(primaryDir) && secondaryDir == Direction.East)
            if(primaryDir == Direction.South)
                mainSection.corridorOffset = mainSection.size.x - (mainSection.corridorOffset + specs.corridorSize);
            else
                mainSection.corridorOffset = mainSection.size.x - (mainSection.corridorOffset + specs.corridorSize);
        
        secondaryBuffer = BufferDivide(secondaryBuffer, 0);
        rightBuffer = BufferDivide(rightBuffer, 0);
        leftBuffer = BufferDivide(leftBuffer, 0);
        
        section.children.Add(mainSection);
        section.children.AddRange(secondaryBuffer.children);;
        section.children.AddRange(rightBuffer.children);
        section.children.AddRange(leftBuffer.children);
        rooms.Add(corridor);
        
        return section;
    }

    private Section ApplyDirectionalOffset(Section section, Direction direction, int xOffset, int zOffset)
    {
        switch (direction)
        {
            case Direction.North:
                section.position = new Vector3Int(
                    section.position.x,
                    section.position.y,
                    section.position.z + zOffset
                );
                section.size = new Vector3Int(
                    section.size.x,
                    section.size.y,
                    section.size.z - zOffset
                );
                break;
            case Direction.South:
                section.position = new Vector3Int(
                    section.position.x,
                    section.position.y,
                    section.position.z
                );
                section.size = new Vector3Int(
                    section.size.x,
                    section.size.y,
                    section.size.z - zOffset
                );
                break;
            case Direction.East:
                section.position = new Vector3Int(
                    section.position.x + xOffset,
                    section.position.y,
                    section.position.z
                );
                section.size = new Vector3Int(
                    section.size.x - xOffset,
                    section.size.y,
                    section.size.z
                );
                break;
            case Direction.West:
                section.position = new Vector3Int(
                    section.position.x,
                    section.position.y,
                    section.position.z
                );
                section.size = new Vector3Int(
                    section.size.x - xOffset,
                    section.size.y,
                    section.size.z
                );
                break;
        }
        return section;   
    }    
    
    private void SetSeed(int seed)
    {
        if (seed == 0)
        {
            seed = System.DateTime.Now.Millisecond;
            Debug.Log(seed);
        }
        Random.InitState(seed);
    }
    
    private int PlaceCorridorHorizontal(int width, float entropy)
    {
        int min = specs.roomSize + specs.wallThickness;
        int max = width - specs.roomSize - specs.wallThickness - specs.corridorSize;
        int randPos = Random.Range(min, max);
        int setPos = width / 2 - specs.corridorSize / 2;
        
        return Mathf.RoundToInt(Mathf.Lerp(setPos, randPos, entropy));
    }
    
    private bool CanMacroSubDivide(Section section)
    {
        int min = Mathf.Min(section.size.x, section.size.z);
        return min > specs.macroThreshold;
    }

    
}
