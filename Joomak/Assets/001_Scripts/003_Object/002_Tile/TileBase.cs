using UnityEngine;
using UnityEngine.Tilemaps;

namespace _001_Scripts._003_Object._002_Tile
{
    public enum HallTileType
    {
        Floor,
        Wall
    }

    [CreateAssetMenu(fileName = "New Hall Tile", menuName = "Tile/Hall Tile")]
    public class TileBase : Tile
    {
        [SerializeField] private HallTileType tileType = HallTileType.Floor;
        [SerializeField] private bool isWalkable = true;

        public HallTileType TileType => tileType;
        public bool IsWalkable => isWalkable;
    }
}
