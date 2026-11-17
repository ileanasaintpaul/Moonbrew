using System;
using UnityEngine;

namespace Moonbrew.Farming
{
    // The farm's square cells on the ground plane. Cell (0, 0) spans x and z from 0 to cellSize.
    public static class FarmGrid
    {
        public static Vector2Int CellOf(Vector3 position, float cellSize)
        {
            CheckSize(cellSize);
            return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
        }

        public static Vector3 CenterOf(Vector2Int cell, float cellSize, float height)
        {
            CheckSize(cellSize);
            return new Vector3((cell.x + 0.5f) * cellSize, height, (cell.y + 0.5f) * cellSize);
        }

        // Within reach when at most `reach` cells away in both directions, diagonals included.
        public static bool InReach(Vector2Int from, Vector2Int to, int reach)
        {
            return Math.Abs(from.x - to.x) <= reach && Math.Abs(from.y - to.y) <= reach;
        }

        // A sickle swing: within the radius on the ground plane, and within half the arc on each side of facing.
        public static bool InSwing(Vector3 origin, Vector3 facing, Vector3 target, float radius, float arcDegrees)
        {
            var toTarget = new Vector2(target.x - origin.x, target.z - origin.z);
            if (toTarget.sqrMagnitude > radius * radius)
            {
                return false;
            }

            var direction = new Vector2(facing.x, facing.z);
            if (toTarget.sqrMagnitude < 1e-6f || direction.sqrMagnitude < 1e-6f)
            {
                return true;
            }

            return Vector2.Angle(direction, toTarget) <= arcDegrees / 2f;
        }

        private static void CheckSize(float cellSize)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "A cell needs a positive size.");
            }
        }
    }
}
