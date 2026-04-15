using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapSurroundingsGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap tilemapBase;
    [SerializeField] private Tilemap tilemapDecour;

    [Header("Forest Tiles")]
    [SerializeField] private TileBase[] tilesForestBase;
    [SerializeField] private TileBase[] tilesForestDecour;

    [Header("Tree Prefabs")]
    [SerializeField] private List<GameObject> treePrefabs = new List<GameObject>();

    [Header("Settings")]
    [SerializeField] private int padding = 8;
    [SerializeField, Range(0, 100)] private int decorChance = 15;
    [SerializeField, Range(0, 100)] private int treeChance = 10;
    [SerializeField] private int minTreeDistanceFromMap = 5;

    private readonly List<GameObject> spawnedTrees = new List<GameObject>();


    public void ClearSurroundings()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
                Destroy(tree);
        }

        spawnedTrees.Clear();
    }

    public void GenerateSurroundings(Map map)
    {
        if (map == null || map.rooms == null || map.rooms.Count == 0)
            return;

        if (tilemapBase == null || tilemapDecour == null)
            return;

        if (tilesForestBase == null || tilesForestBase.Length == 0)
            return;

        ClearSurroundings();

        var floorTiles = new HashSet<Vector2Int>(
            map.rooms.SelectMany(r => r.tiles.Select(t => t.pos))
        );

        if (!TryGetUsedBounds(map, out int minX, out int maxX, out int minY, out int maxY))
            return;

        for (int x = minX - padding; x <= maxX + padding; x++)
        {
            for (int y = minY - padding; y <= maxY + padding; y++)
            {
                if (floorTiles.Contains(new Vector2Int(x, y)))
                    continue;

                Vector3Int cell = new Vector3Int(x, y, 0);

                if (tilemapBase.GetTile(cell) != null)
                    continue;

                tilemapBase.SetTile(cell, tilesForestBase[Random.Range(0, tilesForestBase.Length)]);

                if (tilesForestDecour != null && tilesForestDecour.Length > 0 && Random.Range(0, 100) < decorChance)
                    tilemapDecour.SetTile(cell, tilesForestDecour[Random.Range(0, tilesForestDecour.Length)]);

                int distance = GetDistanceFromUsedArea(x, y, minX, maxX, minY, maxY);

                if (distance >= minTreeDistanceFromMap && treePrefabs != null && treePrefabs.Count > 0 && Random.Range(0, 100) < treeChance)
                {
                    Vector3 worldPos = tilemapBase.GetCellCenterWorld(cell);
                    GameObject tree = Instantiate(treePrefabs[Random.Range(0, treePrefabs.Count)], worldPos, Quaternion.identity, transform);
                    spawnedTrees.Add(tree);
                }
            }
        }
    }

    private bool TryGetUsedBounds(Map map, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue; maxX = int.MinValue;
        minY = int.MaxValue; maxY = int.MinValue;

        bool foundAny = false;

        foreach (var room in map.rooms)
        {
            foreach (var gridEntry in room.tiles)
            {
                foundAny = true;
                if (gridEntry.pos.x < minX) minX = gridEntry.pos.x;
                if (gridEntry.pos.x > maxX) maxX = gridEntry.pos.x;
                if (gridEntry.pos.y < minY) minY = gridEntry.pos.y;
                if (gridEntry.pos.y > maxY) maxY = gridEntry.pos.y;
            }
        }

        return foundAny;
    }

    private int GetDistanceFromUsedArea(int x, int y, int minX, int maxX, int minY, int maxY)
    {
        int dx = 0;
        if (x < minX) dx = minX - x;
        else if (x > maxX) dx = x - maxX;

        int dy = 0;
        if (y < minY) dy = minY - y;
        else if (y > maxY) dy = y - maxY;

        return Mathf.Max(dx, dy);
    }
}