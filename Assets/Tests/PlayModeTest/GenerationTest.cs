using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static DungeonGenerator;

public class GenerationTest
{
    // A Test behaves as an ordinary method
    [Test]
    public void GenerationTestSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator GenerationTestWithEnumeratorPasses()
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

        TestWalls();
    }

    private void TestWalls()
    {
        MinimumMutators minimumMutators = Main.instance.GetMinimumMutators();
        List<Wall> allWalls = new List<Wall>();
        List<Wall> northWalls = new List<Wall>();
        List<Wall> southWalls = new List<Wall>();
        List<Wall> eastWalls = new List<Wall>();
        List<Wall> westWalls = new List<Wall>();
        
        foreach (Section section in Main.instance.GetDungeonGenerator().rooms)
            allWalls.AddRange(section.GetWalls(minimumMutators.doorHeight, minimumMutators.doorWidth));
        
        Assert.IsTrue(allWalls.Count > 0, "No walls were generated.");

        foreach (Wall wall in allWalls)
        {
            switch (wall.direction)
            {
                case Direction.North:
                    northWalls.Add(wall);
                    break;
                case Direction.South:
                    southWalls.Add(wall);
                    break;
                case Direction.East:
                    eastWalls.Add(wall);
                    break;
                case Direction.West:
                    westWalls.Add(wall);
                    break;
            }
        }
        
        CheckForOverlap(northWalls, true);
        CheckForOverlap(southWalls, true);
        CheckForOverlap(eastWalls, false);
        CheckForOverlap(westWalls, false);
        
        northWalls.AddRange(southWalls);
        eastWalls.AddRange(westWalls);
        
        CheckForOverlap(northWalls, true);
        CheckForOverlap(eastWalls, false);
    }

    private void CheckForOverlap(List<Wall> walls, bool isXAxis)
    {
        for (int i = 0; i < walls.Count; i++)
        {
            for (int j = 0; j < walls.Count; j++)
            {
                if (i == j) continue;
                
                int pos1 = isXAxis ? walls[i].position.z : walls[i].position.x;
                int pos2 = isXAxis ? walls[j].position.z : walls[j].position.x;
                
                if (pos1 != pos2) continue;
                
                int wall1Min = isXAxis ? walls[i].position.x : walls[i].position.z;
                int wall1Max = isXAxis ? walls[i].position.x + walls[i].size.x : walls[i].position.z + walls[i].size.z;
                int wall2Min = isXAxis ? walls[j].position.x : walls[j].position.z;
                int wall2Max = isXAxis ? walls[j].position.x + walls[j].size.x : walls[j].position.z + walls[j].size.z;

                
                
                Assert.IsTrue(wall1Min < wall2Max && wall2Min < wall1Max,
                    $"Wall overlap detected between walls at position mins {wall1Min} and {wall2Min}"
                    + $" with position maxes {wall1Max} and {wall2Max}.");
            }
        }
    }
}
