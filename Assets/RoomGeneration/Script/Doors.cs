using System;
using UnityEngine;
using UnityEngine.Assertions;
using static Sections;
using static Directions;


public class Doors
{
    public struct Door
    {
        public Vector3Int position;
        public Vector3Int size;
    }
    
    public struct DoorOffset : IComparable
    {
        public Door door;
        public int offset;
    
        public DoorOffset(Door door, int offset)
        {
            this.door = door;
            this.offset = offset;
        }
        
        public int CompareTo(object obj)
        {
            return offset.CompareTo(((DoorOffset)obj).offset);
        }
    }
    
    public static Door CreateDoor(Section section, DungeonGenerator.Specifications specs)
    {
        int wallThickness = specs.wallThickness;
        int doorOffset = CorridorOffsetToDoorOffset(section.corridorOffset, specs);

        Door door = new Door();
        switch (section.direction)
        {
            case Direction.South:
                door = new Door()
                {
                    position =  new Vector3Int(section.corridorOffset == 0 ? 
                            section.position.x + section.size.x / 2 - specs.doorWidth / 2 : 
                            section.position.x + doorOffset, 
                            section.position.y, section.position.z + section.size.z),
                    size = new Vector3Int(specs.doorWidth, specs.floorHeight, wallThickness)
                };
                break;
            case Direction.North:
                door = new Door()
                {
                    position = new Vector3Int(section.corridorOffset == 0 ?
                        section.position.x + section.size.x / 2 - specs.doorWidth / 2 :
                        section.position.x + doorOffset,
                        section.position.y, section.position.z - wallThickness),
                    size = new Vector3Int(specs.doorWidth, specs.floorHeight, wallThickness)
                };
                break;
            case Direction.East:
                door = new Door()
                {
                    position = new Vector3Int(section.position.x - wallThickness, section.position.y, 
                        section.corridorOffset == 0 ? section.position.z + section.size.z / 2 - specs.doorWidth / 2 :
                            section.position.z + doorOffset),
                    size = new Vector3Int(wallThickness, specs.floorHeight, specs.doorWidth)
                };
                break;
            case Direction.West:
                door = new Door()
                {
                    position = new Vector3Int(section.position.x + section.size.x, section.position.y,
                        section.corridorOffset == 0 ? section.position.z + section.size.z / 2 - specs.doorWidth / 2 :
                            section.position.z + doorOffset),
                    size = new Vector3Int(wallThickness, specs.floorHeight, specs.doorWidth)
                };
                break;
            default:
                door = new Door();
                break;
        }
        
        section.leadingDoor = door;

        return door;
    }
    
    public static void AddDoorToRoom(Section room, Door door)
    {
        if (DirectionIsVertical(room.direction))
        {
            if (room.direction == Direction.North)
            {
                int doorX = door.position.x;
                if (doorX >= room.position.x && doorX + door.size.x <= room.position.x + room.size.x)
                {
                    room.southDoors.Add(new DoorOffset(door, doorX));
                    if (room.leadingRoom != null)
                        room.leadingRoom.northDoors.Add(new DoorOffset(door, doorX));
                }
            }
            else
            {
                int doorX = door.position.x;
                if (doorX >= room.position.x && doorX + door.size.x <= room.position.x + room.size.x)
                {
                    room.northDoors.Add(new DoorOffset(door, doorX));
                    if (room.leadingRoom != null)
                        room.leadingRoom.southDoors.Add(new DoorOffset(door, doorX));
                }
            }
        }
        else
        {
            if (room.direction == Direction.East)
            {
                int doorZ = door.position.z;
                if (doorZ >= room.position.z && doorZ + door.size.z <= room.position.z + room.size.z)
                {
                    room.westDoors.Add(new DoorOffset(door, doorZ));
                    if (room.leadingRoom != null)
                        room.leadingRoom.eastDoors.Add(new DoorOffset(door, doorZ));
                }
            }
            else
            {
                int doorZ = door.position.z;
                if (doorZ >= room.position.z && doorZ + door.size.z <= room.position.z + room.size.z)
                {
                    room.eastDoors.Add(new DoorOffset(door, doorZ));
                    if (room.leadingRoom != null)
                        room.leadingRoom.westDoors.Add(new DoorOffset(door, doorZ));
                }
            }
        }
    }
    
    public static int DoorOffsetToCorridorOffset(int doorOffset, DungeonGenerator.Specifications specs)
    {
        int difference = (specs.corridorSize - specs.doorWidth) / 2;
        return doorOffset - difference;
    }

    public static int CorridorOffsetToDoorOffset(int corridorOffset, DungeonGenerator.Specifications specs)
    {
        int difference = (specs.corridorSize - specs.doorWidth) / 2;
        return corridorOffset + difference;
    }
}
