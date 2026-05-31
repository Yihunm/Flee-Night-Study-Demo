using UnityEngine;
using UnityEngine.UI;

namespace FleeNightStudy
{
    /// <summary>右上角小地图：玩家、课本、老师。</summary>
    public class MinimapHUD : MonoBehaviour
    {
        [SerializeField] RectTransform mapRect;
        [SerializeField] RawImage mapImage;
        [SerializeField] float worldRadius = 35f;
        [SerializeField] float mapSize = 180f;

        Texture2D _tex;
        Transform _player;

        void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            _tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            if (mapImage != null) mapImage.texture = _tex;
        }

        public void Bind(RectTransform rect, RawImage image)
        {
            mapRect = rect;
            mapImage = image;
            if (mapRect != null)
            {
                mapRect.anchorMin = mapRect.anchorMax = new Vector2(1f, 1f);
                mapRect.pivot = new Vector2(1f, 1f);
                mapRect.anchoredPosition = new Vector2(-16f, -16f);
                mapRect.sizeDelta = new Vector2(mapSize, mapSize);
            }
        }

        void LateUpdate()
        {
            if (_tex == null || _player == null) return;
            ClearTex(new Color(0.08f, 0.1f, 0.14f, 0.85f));

            DrawBlips<TextbookPickup>(new Color(1f, 0.85f, 0.2f, 1f), 3);
            DrawBlips<TeacherController>(new Color(1f, 0.25f, 0.2f, 1f), 4);
            DrawPoint(_player.position, new Color(0.2f, 1f, 0.45f, 1f), 5);

            _tex.Apply();
        }

        void DrawBlips<T>(Color c, int size) where T : Component
        {
            foreach (var o in FindObjectsOfType<T>())
            {
                if (o == null) continue;
                DrawPoint(o.transform.position, c, size);
            }
        }

        void DrawPoint(Vector3 world, Color c, int size)
        {
            if (!WorldToMap(world, out int x, out int y)) return;
            for (int dy = -size; dy <= size; dy++)
            for (int dx = -size; dx <= size; dx++)
            {
                int px = x + dx, py = y + dy;
                if (px < 0 || py < 0 || px >= _tex.width || py >= _tex.height) continue;
                if (dx * dx + dy * dy <= size * size)
                    _tex.SetPixel(px, py, c);
            }
        }

        bool WorldToMap(Vector3 world, out int x, out int y)
        {
            x = y = 0;
            Vector3 rel = world - _player.position;
            float u = rel.x / worldRadius;
            float v = rel.z / worldRadius;
            if (Mathf.Abs(u) > 1f || Mathf.Abs(v) > 1f) return false;
            x = Mathf.RoundToInt((u * 0.5f + 0.5f) * (_tex.width - 1));
            y = Mathf.RoundToInt((v * 0.5f + 0.5f) * (_tex.height - 1));
            return true;
        }

        void ClearTex(Color c)
        {
            var arr = _tex.GetPixels();
            for (int i = 0; i < arr.Length; i++) arr[i] = c;
            _tex.SetPixels(arr);
        }
    }
}
