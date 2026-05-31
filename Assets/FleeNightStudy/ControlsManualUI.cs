using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace FleeNightStudy
{
    public class ControlsManualUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button backButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoEnsureOnManagers()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name.Contains("MainMenu"))
                return;

            if (Object.FindObjectOfType<ControlsManualUI>() != null)
                return;

            var host = GameObject.Find("Managers") ?? Object.FindObjectOfType<GameStateManager>()?.gameObject;
            if (host == null)
                return;

            host.AddComponent<ControlsManualUI>();
        }

        void Awake()
        {
            ResolvePanel();
            EnsureBackButton();
            ApplyBodyText();
            WireBackButton();
            if (panel != null)
                panel.SetActive(false);
        }

        void Start()
        {
            ResolvePanel();
            if (panel != null)
                ControlsManualLayout.Ensure(panel);
            EnsureBackButton();
            ApplyBodyText();
            WireBackButton();
            ApplyFonts();
        }

        public void Bind(GameObject panelRoot, TMP_Text body)
        {
            panel = panelRoot;
            bodyText = body;
            if (panel != null)
                ControlsManualLayout.Ensure(panel);
            ResolveBodyReference();
            EnsureBackButton();
            ApplyBodyText();
            WireBackButton();
            ApplyFonts();
        }

        void ResolveBodyReference()
        {
            if (panel == null)
                return;

            bodyText = panel.transform.Find("Box/BodyScroll/Viewport/Content/Body")?.GetComponent<TMP_Text>()
                       ?? panel.transform.Find("Box/Body")?.GetComponent<TMP_Text>()
                       ?? bodyText;
        }

        void Update()
        {
            if (GameplayPauseUI.IsPaused)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                if (panel != null && panel.activeSelf)
                    SetVisible(false);
                return;
            }

            if (panel != null && panel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H))
                {
                    SetVisible(false);
                    RestoreGameplayCursor();
                    return;
                }
            }

            if (!Input.GetKeyDown(KeyCode.H))
                return;

            ResolvePanel();
            if (panel == null)
            {
                Debug.LogWarning("[ControlsManualUI] 未找到 ControlsManualPanel，请确认场景已运行 GameplayBootstrap。");
                return;
            }

            SetVisible(!panel.activeSelf);
        }

        void ResolvePanel()
        {
            if (panel != null && bodyText != null)
                return;

            var existingPanel = GameObject.Find("ControlsManualPanel");
            if (existingPanel == null)
                return;

            panel = existingPanel;
            bodyText = existingPanel.transform.Find("Box/BodyScroll/Viewport/Content/Body")?.GetComponent<TMP_Text>()
                       ?? existingPanel.transform.Find("Box/Body")?.GetComponent<TMP_Text>();
            backButton = existingPanel.transform.Find("Box/ManualBackButton")?.GetComponent<Button>();
        }

        void EnsureBackButton()
        {
            if (panel == null)
                return;

            var box = panel.transform.Find("Box");
            if (box == null)
                return;

            var existing = box.Find("ManualBackButton");
            if (existing != null)
            {
                backButton = existing.GetComponent<Button>();
                return;
            }

            backButton = CreateBackButton(box);
        }

        static Button CreateBackButton(Transform box)
        {
            var btnGo = new GameObject("ManualBackButton");
            btnGo.transform.SetParent(box, false);
            btnGo.AddComponent<RectTransform>();

            var layout = btnGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 46f;
            layout.minHeight = 42f;

            var image = btnGo.AddComponent<Image>();
            image.color = new Color(0.14f, 0.18f, 0.26f, 0.96f);

            var button = btnGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.92f, 1f);
            button.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            textGo.AddComponent<RectTransform>();
            Stretch(textGo);

            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 28f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.97f, 1f, 1f);
            label.raycastTarget = false;
            TmpFontHelper.SetUiText(label, "返回");

            btnGo.transform.SetAsLastSibling();
            return button;
        }

        static void Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void WireBackButton()
        {
            if (backButton == null)
                return;

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnClickBack);
        }

        public void OnClickBack() => SetVisible(false);

        void ApplyBodyText()
        {
            ResolveBodyReference();
            if (bodyText != null)
                TmpFontHelper.SetUiText(bodyText, GameUiCopy.InstructionsBody);
        }

        void ApplyFonts()
        {
            if (panel != null)
                TmpFontHelper.ApplyDefaultFontRecursive(panel);
        }

        void SetVisible(bool show)
        {
            panel.SetActive(show);
            panel.transform.SetAsLastSibling();

            var canvas = panel.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);

            if (show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
                RestoreGameplayCursor();

            if (show)
            {
                EnsureBackButton();
                WireBackButton();
                ApplyFonts();
            }
        }

        static void RestoreGameplayCursor()
        {
            var pause = GameObject.Find("GameplayPausePanel");
            if (pause != null && pause.activeSelf)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
