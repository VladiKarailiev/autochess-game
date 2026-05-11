using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoChess
{
    public class DragController : MonoBehaviour
    {
        public BoardGrid grid;
        public PlayerEconomy economy;
        public RoundManager rounds;
        public Inspector inspector;
        public Camera worldCamera;

        Unit dragging;
        Tile originTile;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        void OnEnable()
        {
            if (inspector == null)
                Debug.LogWarning("DragController.inspector is not assigned; right-click will do nothing.");
        }

        void Update()
        {
            if (grid == null || worldCamera == null) return;
            bool inPrep = rounds == null || rounds.Phase == GamePhase.Prep;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            bool pointerOverHud = GameHUD.IsPointerOverHud(screenPos);
            Vector3 worldPos = ScreenToWorld(screenPos);

            if (mouse.rightButton.wasPressedThisFrame && dragging == null && !pointerOverHud)
                TryInspect(worldPos);

            if (!inPrep)
            {
                if (dragging != null)
                {
                    dragging.PlaceOnTile(originTile);
                    dragging = null;
                    originTile = null;
                }
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && !pointerOverHud)
                TryStartDrag(worldPos);

            if (dragging != null)
                dragging.transform.position = worldPos;

            if (mouse.leftButton.wasReleasedThisFrame && dragging != null)
                FinishDrag(worldPos);
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            float depth = -worldCamera.transform.position.z;
            Vector3 w = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            w.z = 0f;
            return w;
        }

        void TryStartDrag(Vector3 worldPos)
        {
            Tile tile = grid.WorldToTile(worldPos);
            if (tile == null || tile.occupant == null) return;
            if (tile.occupant.team != Team.Player) return;
            dragging = tile.occupant;
            originTile = tile;
        }

        void FinishDrag(Vector3 worldPos)
        {
            Tile target = grid.WorldToTile(worldPos);

            if (target == null || target.zone == TileZone.EnemyCombat)
            {
                dragging.PlaceOnTile(originTile);
            }
            else if (target.occupant == null || target.occupant == dragging)
            {
                if (CanPlaceOnEmpty(originTile, target))
                    dragging.PlaceOnTile(target);
                else
                    dragging.PlaceOnTile(originTile);
            }
            else
            {
                Unit other = target.occupant;
                other.PlaceOnTile(originTile);
                dragging.PlaceOnTile(target);
            }

            dragging = null;
            originTile = null;
        }

        bool CanPlaceOnEmpty(Tile from, Tile to)
        {
            if (to.zone == TileZone.EnemyCombat) return false;
            if (from.zone == TileZone.PlayerCombat) return true;
            if (to.zone == TileZone.Bench) return true;
            if (economy == null) return true;
            return grid.CountPlayerCombatUnits() < economy.level;
        }

        void TryInspect(Vector3 worldPos)
        {
            if (inspector == null) return;
            Tile tile = grid.WorldToTile(worldPos);
            if (tile == null || tile.occupant == null)
            {
                inspector.Clear();
                return;
            }
            inspector.Toggle(tile.occupant);
        }
    }
}
