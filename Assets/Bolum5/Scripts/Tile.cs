using UnityEngine;

namespace DepremOyunu
{
    [RequireComponent(typeof(Renderer))]
    public class Tile : MonoBehaviour
    {
        public int row;
        public int col;
        public TileType type;
        public string hazardKey;
        public string decisionKey;

        [Header("Materyaller (GridManager tarafından atanır, boş bırakabilirsiniz)")]
        public Material openMat;
        public Material startMat;
        public Material goalMat;
        public Material hazardMat;
        public Material decisionMat;
        public Material visitedMat;
        public Material hoverMat;

        private Renderer _renderer;
        private Material _baseMat;
        public bool visited;

        [Header("Toplanma alanı tabelası gibi üstüne konan opsiyonel dekor")]
        public GameObject decorPrefabInstance;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void ApplyBaseVisual()
        {
            switch (type)
            {
                case TileType.Start: _baseMat = startMat != null ? startMat : openMat; break;
                case TileType.Goal: _baseMat = goalMat != null ? goalMat : openMat; break;
                case TileType.Hazard: _baseMat = hazardMat; break;
                case TileType.Decision: _baseMat = decisionMat != null ? decisionMat : openMat; break;
                default: _baseMat = openMat; break;
            }
            if (_baseMat != null && _renderer != null) _renderer.material = _baseMat;
        }

        public void SetVisited(bool value)
        {
            visited = value;
            if (_renderer == null) return;
            if (value && visitedMat != null) _renderer.material = visitedMat;
            else ApplyBaseVisual();
        }

        public void SetHover(bool on)
        {
            if (_renderer == null || type == TileType.Hazard) return; // hazard rengi sabit kalsın
            if (on && hoverMat != null) _renderer.material = hoverMat;
            else if (!on) _renderer.material = visited && visitedMat != null ? visitedMat : _baseMat;
        }

        public GridCoord Coord => new GridCoord(row, col);
    }
}
