using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FleeNightStudy
{
    public enum ItemType
    {
        SpeedSnack = 1,
        InvisibilityUniform = 2,
        TextbookMagnet = 3,
        AlarmClock = 4,
        ChalkBomb = 5
    }

    /// <summary>道具栏：1-5 切换，Z 使用。</summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] Transform throwOrigin;
        [SerializeField] GameObject alarmClockPrefab;
        [SerializeField] float alarmThrowSize = 0.22f;
        [SerializeField] float speedSnackDuration = 5f;
        [SerializeField] float speedSnackMultiplier = 1.8f;
        [SerializeField] float invisibilityDuration = 5f;
        [SerializeField] float magnetDuration = 3f;
        [SerializeField] float magnetRadius = 18f;
        [SerializeField] float throwForce = 8f;

        readonly Dictionary<ItemType, int> _counts = new Dictionary<ItemType, int>();
        ItemType _selected = ItemType.SpeedSnack;
        FirstPersonWalker _walker;
        PlayerStealth _stealth;

        public ItemType SelectedItem => _selected;
        public event System.Action OnInventoryChanged;

        void Awake()
        {
            _walker = GetComponent<FirstPersonWalker>();
            _stealth = GetComponent<PlayerStealth>();
            if (_stealth == null)
                _stealth = gameObject.AddComponent<PlayerStealth>();
            if (throwOrigin == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) throwOrigin = cam.transform;
            }
        }

        void Start()
        {
            InitForDifficulty(GameSessionData.Difficulty);
        }

        public void InitForDifficulty(GameDifficulty diff)
        {
            _counts.Clear();
            if (diff == GameDifficulty.Easy)
            {
                foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
                    _counts[t] = 1;
                _selected = ItemType.SpeedSnack;
            }
            else
            {
                var all = new List<ItemType>((ItemType[])System.Enum.GetValues(typeof(ItemType)));
                while (all.Count > 2)
                    all.RemoveAt(Random.Range(0, all.Count));
                foreach (var t in all)
                    _counts[t] = 1;
                _selected = all.Count > 0 ? all[0] : ItemType.SpeedSnack;
            }
            OnInventoryChanged?.Invoke();
        }

        void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(ItemType.SpeedSnack);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(ItemType.InvisibilityUniform);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select(ItemType.TextbookMagnet);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Select(ItemType.AlarmClock);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Select(ItemType.ChalkBomb);

            if (Input.GetKeyDown(KeyCode.Z))
                UseSelected();
        }

        public int GetCount(ItemType type) => _counts.TryGetValue(type, out int c) ? c : 0;

        public bool HasItem(ItemType type) => GetCount(type) > 0;

        void Select(ItemType type)
        {
            if (!HasItem(type)) return;
            _selected = type;
            OnInventoryChanged?.Invoke();
        }

        void UseSelected()
        {
            if (!HasItem(_selected)) return;
            switch (_selected)
            {
                case ItemType.SpeedSnack: UseSpeedSnack(); break;
                case ItemType.InvisibilityUniform: UseInvisibility(); break;
                case ItemType.TextbookMagnet: StartCoroutine(UseMagnet()); break;
                case ItemType.AlarmClock: UseAlarm(); break;
                case ItemType.ChalkBomb: UseChalk(); break;
            }
            Consume(_selected);
        }

        void Consume(ItemType type)
        {
            if (!_counts.ContainsKey(type)) return;
            _counts[type] = Mathf.Max(0, _counts[type] - 1);
            if (_counts[type] <= 0 && _selected == type)
            {
                foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
                {
                    if (HasItem(t)) { _selected = t; break; }
                }
            }
            OnInventoryChanged?.Invoke();
        }

        void UseSpeedSnack()
        {
            if (_walker != null)
                StartCoroutine(SpeedBoostRoutine());
        }

        IEnumerator SpeedBoostRoutine()
        {
            _walker.SpeedMultiplier = speedSnackMultiplier;
            yield return new WaitForSeconds(speedSnackDuration);
            if (_walker != null) _walker.SpeedMultiplier = 1f;
        }

        void UseInvisibility() => _stealth.ActivateStealth(invisibilityDuration);

        IEnumerator UseMagnet()
        {
            float end = Time.time + magnetDuration;
            while (Time.time < end)
            {
                PullTextbooks();
                yield return null;
            }
        }

        void PullTextbooks()
        {
            var books = FindObjectsOfType<TextbookPickup>();
            foreach (var b in books)
            {
                if (b == null) continue;
                float d = Vector3.Distance(transform.position, b.transform.position);
                if (d > magnetRadius) continue;
                b.transform.position = Vector3.MoveTowards(b.transform.position, transform.position + Vector3.up, 12f * Time.deltaTime);
            }
        }

        void UseAlarm()
        {
            var forward = throwOrigin != null ? throwOrigin.forward : transform.forward;
            var spawnPos = throwOrigin != null
                ? throwOrigin.position + forward * 0.5f
                : transform.position + forward;

            var prefab = ResolveAlarmPrefab();
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab);
                go.name = "AlarmClock_Thrown";
                go.transform.position = spawnPos;
                go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                FitThrowScale(go, alarmThrowSize);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "AlarmClock_Thrown";
                go.transform.localScale = Vector3.one * 0.35f;
                go.transform.position = spawnPos;
            }

            PrepareThrownBody(go, forward);
            go.AddComponent<AlarmClockProjectile>().Launch(forward);
        }

        GameObject ResolveAlarmPrefab()
        {
            if (alarmClockPrefab != null)
                return alarmClockPrefab;

            alarmClockPrefab = Resources.Load<GameObject>("FleeNightStudy/AlarmClock");
            return alarmClockPrefab;
        }

        static void FitThrowScale(GameObject go, float targetMaxExtent)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                go.transform.localScale = Vector3.one * targetMaxExtent;
                return;
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max <= 0.0001f)
            {
                go.transform.localScale = Vector3.one * targetMaxExtent;
                return;
            }

            float factor = targetMaxExtent / max;
            go.transform.localScale *= factor;
        }

        void PrepareThrownBody(GameObject go, Vector3 forward)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.isTrigger = false;

            rb.AddForce(forward * throwForce + Vector3.up * 2f, ForceMode.VelocityChange);
        }

        void UseChalk()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ChalkBomb_Thrown";
            go.transform.localScale = Vector3.one * 0.25f;
            go.transform.position = throwOrigin != null ? throwOrigin.position + throwOrigin.forward * 0.5f : transform.position + transform.forward;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            var forward = throwOrigin != null ? throwOrigin.forward : transform.forward;
            rb.AddForce(forward * throwForce + Vector3.up * 1.5f, ForceMode.VelocityChange);
            go.AddComponent<ChalkBombProjectile>();
        }
    }
}
