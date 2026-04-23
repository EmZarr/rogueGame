using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, HashSet<Vector2Int> walkable)
    {
        var open = new SortedSet<(int f, int id, Vector2Int pos)>(
            Comparer<(int f, int id, Vector2Int pos)>.Create((a, b) =>
                a.f != b.f ? a.f.CompareTo(b.f) : a.id.CompareTo(b.id))
        );

        var gCost = new Dictionary<Vector2Int, int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        int idCounter = 0;

        gCost[start] = 0;
        open.Add((Heuristic(start, end), idCounter++, start));

        while (open.Count > 0)
        {
            var (_, _, current) = open.Min;
            open.Remove(open.Min);

            if (current == end)
                return ReconstructPath(cameFrom, end);

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighbor = current + dir;
                if (!walkable.Contains(neighbor)) continue;

                int tentativeG = gCost[current] + 1;

                if (!gCost.TryGetValue(neighbor, out int existingG) || tentativeG < existingG)
                {
                    gCost[neighbor] = tentativeG;
                    cameFrom[neighbor] = current;
                    open.Add((tentativeG + Heuristic(neighbor, end), idCounter++, neighbor));
                }
            }
        }

        return new List<Vector2Int>();
    }

    static int Heuristic(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        var current = end;
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(current);
        path.Reverse();
        return path;
    }
}