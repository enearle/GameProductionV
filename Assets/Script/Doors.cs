using UnityEngine;
using static Sections;
using static Directions;


public class Doors
{
    public struct Door
    {
        public Vector3Int position;
        public Vector3Int size;
    }
    
    public static Door CreateDoor(Section section, DungeonGenerator.MinimumMutators minimums)
    {
        int wallThickness = minimums.wallThickness;
        int doorOffset = CorridorOffsetToDoorOffset(section.corridorOffset, minimums);

        Door door = new Door();
        switch (section.direction)
        {
            case Direction.South:
                door = new Door()
                {
                    position =  new Vector3Int(section.corridorOffset == 0 ? 
                            section.position.x + section.size.x / 2 - minimums.doorWidth / 2 : 
                            section.position.x + doorOffset, 
                            section.position.y, section.position.z + section.size.z),
                    size = new Vector3Int(minimums.doorWidth, minimums.floorHeight, wallThickness)
                };
                break;
            case Direction.North:
                door = new Door()
                {
                    position = new Vector3Int(section.corridorOffset == 0 ?
                        section.position.x + section.size.x / 2 - minimums.doorWidth / 2 :
                        section.position.x + doorOffset,
                        section.position.y, section.position.z - wallThickness),
                    size = new Vector3Int(minimums.doorWidth, minimums.floorHeight, wallThickness)
                };
                break;
            case Direction.East:
                door = new Door()
                {
                    position = new Vector3Int(section.position.x - wallThickness, section.position.y, 
                        section.corridorOffset == 0 ? section.position.z + section.size.z / 2 - minimums.doorWidth / 2 :
                            section.position.z + doorOffset),
                    size = new Vector3Int(wallThickness, minimums.floorHeight, minimums.doorWidth)
                };
                break;
            case Direction.West:
                door = new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x, section.position.y,
                        section.corridorOffset == 0 ? section.position.z + section.size.z / 2 - minimums.doorWidth / 2 :
                            section.position.z + doorOffset),
                    size = new Vector3Int(wallThickness, minimums.floorHeight, minimums.doorWidth)
                };
                break;
            default:
                door = new Door();
                break;
        }

        return door;
    }
    
    public static void AddDoorToRoom(Section room, Door door)
    {
        if (DirectionIsVertical(room.direction))
        {
            if (room.direction == Direction.North)
            {
                room.southDoors.Add(door.position.x);;
                if (room.leadingRoom != null)
                    room.leadingRoom.northDoors.Add(door.position.x);
            }
            else
            {
                room.northDoors.Add(door.position.x);
                if (room.leadingRoom != null)
                    room.leadingRoom.southDoors.Add(door.position.x);
            }
        }
        else
        {
            if (room.direction == Direction.East)
            {
                room.westDoors.Add(door.position.z);
                if (room.leadingRoom != null)
                    room.leadingRoom.eastDoors.Add(door.position.z);
            }
            else
            {
                room.eastDoors.Add(door.position.z);
                if (room.leadingRoom != null)
                    room.leadingRoom.westDoors.Add(door.position.z);
            }
        }
    }
    
    public static int DoorOffsetToCorridorOffset(int doorOffset, DungeonGenerator.MinimumMutators minimums)
    {
        int difference = (minimums.corridorSize - minimums.doorWidth) / 2;
        return doorOffset - difference;
    }

    public static int CorridorOffsetToDoorOffset(int corridorOffset, DungeonGenerator.MinimumMutators minimums)
    {
        int difference = (minimums.corridorSize - minimums.doorWidth) / 2;
        return corridorOffset + difference;
    }
}
