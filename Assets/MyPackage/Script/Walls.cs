using UnityEngine;
using static Directions;

public class Walls
{
    public struct Wall
    {
        public Vector3Int position;
        public Vector3Int size;
        public Direction direction;

        public Wall(Vector3Int position, Vector3Int size, Direction direction)
        {
            this.position = position;
            this.size = size;
            this.direction = direction;
        }
    }
}
