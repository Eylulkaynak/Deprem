using UnityEngine;

namespace DepremOyunu
{
    /// <summary>
    /// Sahnede TEK SEFERLİK olarak kamerayı klasik izometrik açıya (45° / 35.264°)
    /// yerleştirir ve tüm tahtayı kapsayacak şekilde orthographic size'ı ayarlar.
    /// Kamera sabittir; oyuncuyu takip etmez.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraRig : MonoBehaviour
    {
        [Header("Referanslar")]
        public GridManager grid;

        [Header("İzometrik Açı")]
        public float yaw = 45f;
        public float pitch = 35.264f; // klasik "true isometric" açısı

        [Header("Mesafe / Kadraj")]
        [Tooltip("Kameranın tahtaya olan mesafesi")]
        public float distance = 14f;
        [Tooltip("Orthographic size'a eklenecek boşluk payı")]
        public float padding = 1.5f;

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
        }

        private void Start()
        {
            Frame();
        }

        [ContextMenu("Kamerayı Tahtaya Göre Ayarla")]
        public void Frame()
        {
            if (grid == null || grid.level == null) return;

            Vector3 center = grid.BoardCenter();
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rot;
            transform.position = center - rot * Vector3.forward * distance;

            // Tahtanın en geniş kenarına göre orthographic size hesapla.
            float boardWidth = grid.level.cols * grid.level.tileSize;
            float boardDepth = grid.level.rows * grid.level.tileSize;
            float diag = Mathf.Sqrt(boardWidth * boardWidth + boardDepth * boardDepth);
            _cam.orthographicSize = diag * 0.5f + padding;
        }
    }
}
