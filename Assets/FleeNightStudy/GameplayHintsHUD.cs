using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FleeNightStudy
{
    /// <summary>玩法左下角三行提示：H 操作手册 / 课本进度 / 当前道具说明。</summary>
    public class GameplayHintsHUD : MonoBehaviour
    {
        const string ContentRootName = "HintsContent";

        [FormerlySerializedAs("interactLine")]
        [SerializeField] TMP_Text helpLine;
        [SerializeField] TMP_Text textbookLine;
        [FormerlySerializedAs("movementLine")]
        [SerializeField] TMP_Text itemLine;

        [SerializeField] string helpText = GameUiCopy.GameplayHelpHint;
        [SerializeField] string textbookLockedFormat = GameUiCopy.TextbookLockedFormat;
        [SerializeField] string textbookUnlockedText = GameUiCopy.TextbookUnlockedText;

        PlayerInventory _inventory;
        bool _linesBuilt;
        RectTransform _contentRoot;

        void Awake()
        {
            EnsurePanelStructure();
        }

        void Start()
        {
            SubscribeGameState(true);
            _inventory = FindObjectOfType<PlayerInventory>();
            if (_inventory != null)
                _inventory.OnInventoryChanged += RefreshItemLine;

            EnsurePanelStructure();
            RefreshAll();
        }

        void OnDisable()
        {
            SubscribeGameState(false);
            if (_inventory != null)
                _inventory.OnInventoryChanged -= RefreshItemLine;
        }

        /// <summary>编辑器一键设置时调用：只保证结构，不依赖 Play 模式。</summary>
        public void EnsurePanelStructure()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas != null && transform.parent != canvas.transform)
                transform.SetParent(canvas.transform, false);

            if (UiRectUtil.GetRectTransform(gameObject) == null)
            {
                Debug.LogError(
                    "[FleeNightStudy] GameplayHintsPanel 不是 UI 物体（无 RectTransform）。请在菜单执行「FleeNightStudy → 一键设置当前场景」后保存。",
                    gameObject);
                return;
            }

            PurgeLegacyDirectHintChildren();

            _contentRoot = GetContentRoot();
            if (_contentRoot == null)
                return;

            _contentRoot.anchorMin = Vector2.zero;
            _contentRoot.anchorMax = Vector2.zero;
            _contentRoot.pivot = Vector2.zero;
            _contentRoot.anchoredPosition = Vector2.zero;
            _contentRoot.sizeDelta = new Vector2(760f, 120f);

            var layout = _contentRoot.GetComponent<VerticalLayoutGroup>()
                         ?? _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
        }

        void PurgeLegacyDirectHintChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == ContentRootName)
                    continue;

                if (child.GetComponent<TMP_Text>() != null || child.name.Contains("Hint"))
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }
        }

        RectTransform GetContentRoot()
        {
            var existing = transform.Find(ContentRootName)?.GetComponent<RectTransform>();
            if (existing != null)
                return existing;

            return UiRectUtil.CreateUiChild(transform, ContentRootName);
        }

        void SubscribeGameState(bool subscribe)
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null)
                return;

            if (subscribe)
            {
                gsm.OnTextbookCountChanged += HandleTextbookCountChanged;
                gsm.OnDoorUnlocked += HandleDoorUnlocked;
            }
            else
            {
                gsm.OnTextbookCountChanged -= HandleTextbookCountChanged;
                gsm.OnDoorUnlocked -= HandleDoorUnlocked;
            }
        }

        void HandleTextbookCountChanged(int collected, int required) => RefreshTextbookLine();
        void HandleDoorUnlocked() => RefreshTextbookLine();

        public void RefreshAll()
        {
            EnsurePanelStructure();
            if (_contentRoot == null)
                _contentRoot = GetContentRoot();
            if (_contentRoot == null)
                return;

            if (!_linesBuilt)
                RebuildHintLines();

            SetLineText(helpLine, helpText);
            RefreshTextbookLine();
            RefreshItemLine();
            SetVisible(true);

            TmpFontHelper.ApplyDefaultFontRecursive(_contentRoot.gameObject);
        }

        public void Bind(TMP_Text help, TMP_Text textbook, TMP_Text item)
        {
            RebuildHintLines();
            RefreshAll();
        }

        public void RebuildHintLines()
        {
            helpLine = null;
            textbookLine = null;
            itemLine = null;

            EnsurePanelStructure();
            if (_contentRoot == null)
                _contentRoot = GetContentRoot();
            if (_contentRoot == null)
                return;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = _contentRoot.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            helpLine = CreateLine("HelpHint");
            textbookLine = CreateLine("TextbookHint");
            itemLine = CreateLine("ItemHint");

            _linesBuilt = true;
        }

        TMP_Text CreateLine(string lineName)
        {
            var rect = UiRectUtil.CreateUiChild(_contentRoot, lineName);
            if (rect == null)
                return null;

            var le = rect.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 30f;
            le.preferredHeight = 34f;
            le.flexibleWidth = 1f;

            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Normal;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            tmp.richText = false;
            tmp.enableAutoSizing = false;
            tmp.outlineWidth = 0f;

            var font = TmpFontHelper.ResolveUiFont();
            if (font != null)
                TmpFontHelper.ApplyUiFont(tmp, font);

            return tmp;
        }

        public void RefreshTextbookLine()
        {
            if (textbookLine == null)
                return;

            var gsm = GameStateManager.Instance;
            string text;
            if (gsm == null)
                text = string.Format(textbookLockedFormat, 0);
            else if (gsm.HasCollectedEnoughTextbooks || gsm.DoorUnlocked)
                text = textbookUnlockedText;
            else
                text = string.Format(textbookLockedFormat, gsm.TextbooksRemaining);

            SetLineText(textbookLine, text);
        }

        public void RefreshItemLine()
        {
            if (itemLine == null)
                return;

            if (_inventory == null)
                _inventory = FindObjectOfType<PlayerInventory>();

            if (_inventory == null)
            {
                SetLineText(itemLine, GameUiCopy.NoItemText);
                return;
            }

            int slot = (int)_inventory.SelectedItem;
            int count = _inventory.GetCount(_inventory.SelectedItem);
            SetLineText(itemLine, GameUiCopy.FormatItemHint(_inventory.SelectedItem, slot, count));
        }

        static void SetLineText(TMP_Text line, string text)
        {
            if (line == null)
                return;

            TmpFontHelper.SetUiText(line, text ?? string.Empty);
            line.fontSize = 22f;
            line.alignment = TextAlignmentOptions.MidlineLeft;
            line.color = Color.white;
            line.fontStyle = FontStyles.Normal;
            line.outlineWidth = 0f;
        }

        public void SetVisible(bool visible)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                visible = false;
            gameObject.SetActive(visible);
        }

        void LateUpdate()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded && gameObject.activeSelf)
                SetVisible(false);
        }
    }
}
