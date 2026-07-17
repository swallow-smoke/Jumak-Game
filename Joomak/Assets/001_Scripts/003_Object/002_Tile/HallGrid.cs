using _001_Scripts._001_Manager;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _001_Scripts._003_Object._002_Tile
{
    // 홀 Grid/Tilemap 접근점. 셀 좌표 <-> 월드 좌표 변환과 바닥 타일의 보행 가능 여부를 제공한다.
    public sealed class HallGrid : SinManagerBase<HallGrid>
    {
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap floorTilemap;

        public override void Initialize()
        {
        }

        public Vector3 CellToWorld(Vector3Int cell) => grid.GetCellCenterWorld(cell);

        public Vector3Int WorldToCell(Vector3 world) => grid.WorldToCell(world);

        public bool TryGetTile(Vector3Int cell, out TileBase tile)
        {
            tile = floorTilemap != null ? floorTilemap.GetTile<TileBase>(cell) : null;
            return tile != null;
        }

        public bool IsWalkable(Vector3Int cell) => TryGetTile(cell, out TileBase tile) && tile.IsWalkable;
    }
}
