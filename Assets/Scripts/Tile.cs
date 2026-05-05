using UnityEngine;

namespace AutoChess
{
    public class Tile : MonoBehaviour
    {
        public Vector2Int gridPos;
        public TileZone zone;
        public Unit occupant;
    }
}
