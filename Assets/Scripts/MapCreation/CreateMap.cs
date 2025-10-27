using UnityEngine;
using UnityEngine.Tilemaps;


public class CreateMap : MonoBehaviour

{
    Tilemap tilemap;

    [SerializeField] TileBase groundTile;
    [SerializeField] TileBase wallTile;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*grid = FindFirstObjectByType<Grid>();
        for (int x = 0; x++; x < Grid.x) {
            for (int y = 0; y++; y < Grid.z) {
                tilemapWalk.xz = walk;
        }
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
