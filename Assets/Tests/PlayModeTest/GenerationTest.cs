using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static DungeonGenerator;
using static Sections;
using static Directions;
using static Walls;
using static Doors;
using static Helpers;

public class GenerationTest
{

    [UnityTest]
    public IEnumerator DoorTestPositive()
    {
        yield return DefaultLoad();

        TestDoorsToFarForward();
    }
    
    [UnityTest]
    public IEnumerator DoorTestNegative()
    {
        yield return DefaultLoad();

        TestDoorsToFarBackward();
    }


    private void TestDoorsToFarForward()
    {
        Specifications specifications = Main.instance.GetSpecs();
        Vector3Int wallOffset = new Vector3Int(specifications.wallThickness, 0, specifications.wallThickness);
        HashSet<string> anomalies = new HashSet<string>();
        
        Section mainSection = Main.instance.GetDungeonGenerator().floors[0];
        List<Section> rooms = Main.instance.GetDungeonGenerator().rooms;
        
        for (int i =0; i < rooms.Count; i++)
        {
            string parentDivisionType = "null";
            string grandParentDivisionType = "null";
            if (rooms[i].parent == null && i != 0)
                anomalies.Add($"Section {i} has no parent.\n");
            else if(i != 0)
            {
                parentDivisionType = rooms[i].parent.divisionType.ToString();
                if (rooms[i].parent != mainSection)
                {
                    if (rooms[i].parent.parent == null)
                        anomalies.Add($"Section {i} has no grandparent and parent is not main section.\n");
                    else
                        grandParentDivisionType = rooms[i].parent.parent.divisionType.ToString();
                }
            }
            
            List<DoorOffset> eastDoors = rooms[i].eastDoors;
            for (int j = 0; j < eastDoors.Count; j++)
            {
                int doorPosInRoom = eastDoors[j].offset - rooms[i].position.z;
                
                if (doorPosInRoom > rooms[i].size.z)
                    anomalies.Add($"Room {i} east door {j} starts outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.z}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
                else if (doorPosInRoom > rooms[i].size.z - specifications.doorWidth)
                    anomalies.Add($"Room {i} east door {j} ends outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.z}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
            
            List<DoorOffset> westDoors = rooms[i].westDoors;
            for (int j = 0; j < westDoors.Count; j++)
            {
                int doorPosInRoom = westDoors[j].offset - rooms[i].position.z;
                
                if (doorPosInRoom > rooms[i].size.z)
                    anomalies.Add($"Room {i} west door {j} starts outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.z}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
                else if (doorPosInRoom > rooms[i].size.z - specifications.doorWidth)
                    anomalies.Add($"Room {i} west door {j} ends outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.z}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
            
            List<DoorOffset> northDoors = rooms[i].northDoors;
            for (int j = 0; j < northDoors.Count; j++)
            {
                int doorPosInRoom = northDoors[j].offset - rooms[i].position.x;
                
                if (doorPosInRoom > rooms[i].size.x)
                    anomalies.Add($"Room {i} north door {j} starts outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.x}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
                else if (doorPosInRoom > rooms[i].size.x - specifications.doorWidth)
                    anomalies.Add($"Room {i} north door {j} ends outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.x}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
            
            List<DoorOffset> southDoors = rooms[i].southDoors;
            for (int j = 0; j < southDoors.Count; j++)
            {
                int doorPosInRoom = southDoors[j].offset - rooms[i].position.x;
                
                if (doorPosInRoom > rooms[i].size.x)
                    anomalies.Add($"Room {i} south door {j} starts outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.x}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
                else if (doorPosInRoom > rooms[i].size.x - specifications.doorWidth)
                    anomalies.Add($"Room {i} south door {j} ends outside of room bounds. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.x}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
        }
        
        string p = "------------------------------------------------------------------------------------\n";
        Assert.IsTrue(anomalies.Count == 0,
            p + $"\t\tDoor Positive Anomalies Found in Seed: {Main.instance.GetSeed()}\n" + p + '\n' +
            string.Join("", anomalies));
    }
    
    private void TestDoorsToFarBackward()
    {
        Specifications specifications = Main.instance.GetSpecs();
        Vector3Int wallOffset = new Vector3Int(specifications.wallThickness, 0, specifications.wallThickness);
        HashSet<string> anomalies = new HashSet<string>();
        
        List<Section> rooms = Main.instance.GetDungeonGenerator().rooms;
        Section mainSection = Main.instance.GetDungeonGenerator().floors[0];
        
        for (int i =0; i < rooms.Count; i++)
        {
            string parentDivisionType = "null";
            string grandParentDivisionType = "null";
            if (rooms[i].parent == null && i != 0)
                anomalies.Add($"Section {i} has no parent.\n");
            else if(i != 0)
            {
                parentDivisionType = rooms[i].parent.divisionType.ToString();
                if (rooms[i].parent != mainSection)
                {
                    if (rooms[i].parent.parent == null)
                        anomalies.Add($"Section {i} has no grandparent and parent is not main section.\n");
                    else
                        grandParentDivisionType = rooms[i].parent.parent.divisionType.ToString();
                }
            }
            
            List<DoorOffset> eastDoors = rooms[i].eastDoors;
            for (int j = 0; j < eastDoors.Count; j++)
            {
                int doorPosInRoom = eastDoors[j].offset - rooms[i].position.z;
                if (doorPosInRoom < 0)
                    anomalies.Add($"Room {i} east door {j} has a negative position. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.z}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
            
            List<DoorOffset> westDoors = rooms[i].westDoors;
            for (int j = 0; j < westDoors.Count; j++)
            {
                int doorPosInRoom = westDoors[j].offset - rooms[i].position.z;
                if (doorPosInRoom < 0)
                    anomalies.Add($"Room {i} west door {j} has a negative position. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.z}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
            
            List<DoorOffset> northDoors = rooms[i].northDoors;
            for (int j = 0; j < northDoors.Count; j++)
            {
                int doorPosInRoom = northDoors[j].offset - rooms[i].position.x;
                if (doorPosInRoom < 0)
                    anomalies.Add($"Room {i} north door {j} has a negative position. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.x}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
            
            List<DoorOffset> southDoors = rooms[i].southDoors;
            for (int j = 0; j < southDoors.Count; j++)
            {
                int doorPosInRoom = southDoors[j].offset - rooms[i].position.x;
                if (doorPosInRoom < 0)
                    anomalies.Add($"Room {i} south door {j} has a negative position. Door offset: {doorPosInRoom} Wall length: {rooms[i].size.x}\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
            }
        }
        
        string p = "------------------------------------------------------------------------------------\n";
        Assert.IsTrue(anomalies.Count == 0,
            p + $"\t\tDoor Negative Anomalies Found in Seed: {Main.instance.GetSeed()}\n" + p + '\n' +
            string.Join("", anomalies));
    }
        
    [UnityTest]
    public IEnumerator WallTestZeroOrNegativeLength()
    {
        yield return DefaultLoad();

        TestWallsZeroLengthOrNegativeSize();
    }
    
    [UnityTest]
    public IEnumerator WallTestOverlap()
    {
        yield return DefaultLoad();
        
        TestWallsOverlap();
    }

    private void TestWallsZeroLengthOrNegativeSize()
    {
        Specifications specifications = Main.instance.GetSpecs();
        HashSet<string> anomalies = new HashSet<string>();
        
        List<Section> rooms = Main.instance.GetDungeonGenerator().rooms;
        Section mainSection = Main.instance.GetDungeonGenerator().floors[0];

        for (int i = 0; i < Main.instance.GetDungeonGenerator().rooms.Count; i++)
        {
            
            Section section = Main.instance.GetDungeonGenerator().rooms[i];
            Wall[] iSectionWalls = section.GetWalls(specifications.doorHeight, specifications.doorWidth);
            
            string parentDivisionType = "null";
            string grandParentDivisionType = "null";
            if (rooms[i].parent == null && i != 0)
                anomalies.Add($"Section {i} has no parent.\n");
            else if(i != 0)
            {
                parentDivisionType = rooms[i].parent.divisionType.ToString();
                if (rooms[i].parent != mainSection)
                {
                    if (rooms[i].parent.parent == null)
                        anomalies.Add($"Section {i} has no grandparent and parent is not main section.\n");
                    else
                        grandParentDivisionType = rooms[i].parent.parent.divisionType.ToString();
                }
            }

            for (int k = 0; k < iSectionWalls.Length; k++)
            {
                Vector3Int wall1Start = iSectionWalls[k].position;
                Vector3Int wall1End = wall1Start + iSectionWalls[k].size;

                if (wall1Start == wall1End)
                {
                    anomalies.Add($"Room: {i}, Wall: {k} has a length of 0. {wall1Start}. \n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
                }
                else if (iSectionWalls[k].size.x < 0 || iSectionWalls[k].size.z < 0)
                {
                    anomalies.Add($"Room: {i}, Wall: {k} has a negative size. {wall1End - wall1Start}.\n" +
                                  $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n\n");
                }
            }
        }

        string p = "------------------------------------------------------------------------------------\n";
        Assert.IsTrue(anomalies.Count == 0,
            p + $"\t\tWall Zero/Negative Anomalies Found in Seed: {Main.instance.GetSeed()}\n" + p + '\n' +
            string.Join("", anomalies));
    }
    
    private void TestWallsOverlap()
    {
        Specifications specifications = Main.instance.GetSpecs();
        HashSet<string> anomalies = new HashSet<string>();

        for (int i = 0; i < Main.instance.GetDungeonGenerator().rooms.Count; i++)
        {
            Wall[] iSectionWalls = Main.instance.GetDungeonGenerator().rooms[i]
                .GetWalls(specifications.doorHeight, specifications.doorWidth);

            for (int j = i + 1; j < Main.instance.GetDungeonGenerator().rooms.Count; j++)
            {
                Wall[] jSectionWalls = Main.instance.GetDungeonGenerator().rooms[j]
                    .GetWalls(specifications.doorHeight, specifications.doorWidth);

                for (int k = 0; k < iSectionWalls.Length; k++)
                {
                    for (int l = 0; l < jSectionWalls.Length; l++)
                    {
                        Vector3Int wall1Start = iSectionWalls[k].position;
                        Vector3Int wall1End = wall1Start + iSectionWalls[k].size;
                        Vector3Int wall2Start = jSectionWalls[l].position;
                        Vector3Int wall2End = wall2Start + jSectionWalls[l].size;

                        if (wall1Start == wall2Start || wall1Start == wall2End ||
                            wall1End == wall2Start || wall1End == wall2End) continue;

                        bool orthogonal;
                        if (AxisAlignedLinesOverlap(wall1Start, wall1End, wall2Start, wall2End, out orthogonal))
                        {
                            anomalies.Add( orthogonal ? "Orthogonal" : "Parallel" + " " +
                                $"Wall {k} in room {i} overlaps with wall {l} in room {j}.\n" +
                                $"Wall 1 start: {wall1Start}, end: {wall1End}. Wall 2 start: {wall2Start}, end: {wall2End} \n" +
                                $"Room 1 pos: {Main.instance.GetDungeonGenerator().rooms[i].position}, size: {Main.instance.GetDungeonGenerator().rooms[i].size} \n" +
                                $"Room 2 pos: {Main.instance.GetDungeonGenerator().rooms[j].position}, size: {Main.instance.GetDungeonGenerator().rooms[j].size} \n\n");
                        }
                    }
                }
            }
        }

        string p = "------------------------------------------------------------------------------------\n";
        Assert.IsTrue(anomalies.Count == 0,
            p + $"\t\tWall Overlap Anomalies Found in Seed: {Main.instance.GetSeed()}\n" + p + '\n' +
            string.Join("", anomalies));
    }

    private bool AxisAlignedLinesOverlap(Vector3Int line1Start, Vector3Int line1End, Vector3Int line2Start,
        Vector3Int line2End, out bool orthogonal)
    {
        bool wall1Vertical = line1Start.x == line1End.x;
        bool wall2Vertical = line2Start.x == line2End.x;
        
        if (wall1Vertical == wall2Vertical)
        {
            orthogonal = false;
            if (wall1Vertical)
            {
                if (line1Start.x == line2Start.x)
                    return RangeContainsIntExclusive(line1Start.z, line2Start.z, line2End.z) || RangeContainsIntExclusive(line1End.z, line2Start.z, line2End.z);
            }
            else
            {
                if (line1Start.z == line2Start.z)
                    return RangeContainsIntExclusive(line1Start.x, line2Start.x, line2End.x) || RangeContainsIntExclusive(line1End.x, line2Start.x, line2End.x);
            }
            return false;
        }
        
        
        orthogonal = true;
        if (wall1Vertical)
        {
            return RangeContainsIntExclusive(line1Start.x, line2Start.x, line2End.x) && RangeContainsIntExclusive(line2Start.z, line1Start.z, line1End.z);
        }
        else
        {
            return RangeContainsIntExclusive(line2Start.x, line1Start.x, line1End.x) && RangeContainsIntExclusive(line1Start.z, line2Start.z, line2End.z);
        }
    }

    [UnityTest]
    public IEnumerator MacroDivideTest()
    {
        yield return DefaultLoad();
        
        TestMacroDivide();
    }

    private void TestMacroDivide()
    {
        Specifications specifications = Main.instance.GetSpecs();
        HashSet<string> anomalies = new HashSet<string>();
        
        List<Section> rooms = Main.instance.GetDungeonGenerator().rooms;
        Section mainSection = Main.instance.GetDungeonGenerator().floors[0];

        for (int i = 0; i < Main.instance.GetDungeonGenerator().rooms.Count; i++)
        {
            
            Section section = Main.instance.GetDungeonGenerator().rooms[i];
            Wall[] iSectionWalls = section.GetWalls(specifications.doorHeight, specifications.doorWidth);
            
            string parentDivisionType = "null";
            string grandParentDivisionType = "null";
            if (rooms[i].parent == null && i != 0)
                anomalies.Add($"Section {i} has no parent.\n");
            else if(i != 0)
            {
                parentDivisionType = rooms[i].parent.divisionType.ToString();
                if (rooms[i].parent != mainSection)
                {
                    if (rooms[i].parent.parent == null)
                        anomalies.Add($"Section {i} has no grandparent and parent is not main section.\n");
                    else
                        grandParentDivisionType = rooms[i].parent.parent.divisionType.ToString();
                }
            }

            for (int k = 0; k < iSectionWalls.Length; k++)
            {
                Vector3Int wall1Start = iSectionWalls[k].position;
                Vector3Int wall1End = wall1Start + iSectionWalls[k].size;

                if (wall1Start == wall1End)
                {
                    if (section.isCorridor)
                        anomalies.Add($"|||||||||||| Room {i} is a corridor ||||||||||||\n");
                    
                    anomalies.Add($"\tRoom: {i}, Wall: {k} has a length of 0. {wall1Start}. \n" +
                                  $"\tParent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n");
                }
                else if (iSectionWalls[k].size.x < 0 || iSectionWalls[k].size.z < 0)
                {
                    if (section.isCorridor)
                        anomalies.Add($"|||||||||||| Room {i} is a corridor ||||||||||||\n");
                    
                    anomalies.Add($"\tRoom: {i}, Wall: {k} has a negative size. {wall1End - wall1Start}.\n" +
                                  $"\tParent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n");
                }
            }
        }

        string p = "------------------------------------------------------------------------------------\n";        
        Assert.IsTrue(anomalies.Count == 0,
            p + $"\t\tMacro Divide Anomalies Found in Seed: {Main.instance.GetSeed()}\n" + p + '\n' +
            string.Join("", anomalies));
    }

    [UnityTest]
    public IEnumerator CorridorSizeTest()
    {
        yield return DefaultLoad();
        
        TestCorridorSize();
    }

    private void TestCorridorSize()
    {
        Specifications specifications = Main.instance.GetSpecs();
        HashSet<string> anomalies = new HashSet<string>();
        
        List<Section> rooms = Main.instance.GetDungeonGenerator().rooms;
        Section mainSection = Main.instance.GetDungeonGenerator().floors[0];

        for (int i = 0; i < rooms.Count; i++)
        {
            string parentDivisionType = "null";
            string grandParentDivisionType = "null";
            if (rooms[i].parent == null && i != 0)
                anomalies.Add($"Section {i} has no parent.\n");
            else if(i != 0)
            {
                parentDivisionType = rooms[i].parent.divisionType.ToString();
                if (rooms[i].parent != mainSection)
                {
                    if (rooms[i].parent.parent == null)
                        anomalies.Add($"Section {i} has no grandparent and parent is not main section.\n");
                    else
                        grandParentDivisionType = rooms[i].parent.parent.divisionType.ToString();
                }
            }
            
            if (rooms[i].isCorridor && (rooms[i].size.x < 0 || rooms[i].size.z < 0))
                anomalies.Add($"Corridor {i} has a negative size. Size: {rooms[i].size}.\n" +
                              $"Parent division type: {parentDivisionType}, Grandparent division type: {grandParentDivisionType}.\n");
        }
        
        string p = "------------------------------------------------------------------------------------\n";        
        Assert.IsTrue(anomalies.Count == 0,
            p + $"\t\tCorridor Size Anomalies Found in Seed: {Main.instance.GetSeed()}\n" + p + '\n' +
            string.Join("", anomalies));
    }
    
    private Door? GetOffendingDoor(Vector3Int pos)
    {
        foreach (Door door in Main.instance.GetDungeonGenerator().doors)
            if (door.position == pos) return door;
        return null;
    }

    private IEnumerator DefaultLoad()
    {
        SceneManager.LoadScene(0);
        yield return null;

        float timeoutDuration = 30f;
        float startTime = Time.time;

        while (!Main.instance || !Main.instance.GetGenerationCompleted())
        {
            if (Time.time - startTime > timeoutDuration)
            {
                Assert.Fail("Generation test timed out after " + timeoutDuration + " seconds");
            }

            yield return null;
        }
    }
    
}
