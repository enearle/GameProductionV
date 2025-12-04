using UnityEngine;

public static class Directions
{
    public enum Direction
    {
        North,
        South,
        West,
        East
    }
    
    public static bool DirectionIsVertical(Direction direction)
    {
        return direction == Direction.North || direction == Direction.South;
    }
    
    public static Direction ClockWise(Direction direction)
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

    public static Direction CounterClockWise(Direction direction)
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

    public static Vector3 DirectionToVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.North:
                return Vector3.forward;
            case Direction.South:
                return Vector3.back;
            case Direction.West:
                return Vector3.left;
            case Direction.East:
                return Vector3.right;
            default:
                return Vector3.forward;
        }
    }
    
}
