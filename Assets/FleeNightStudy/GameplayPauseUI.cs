using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace FleeNightStudy
{
    /// <summary>玩法中按 Esc 打开暂停菜单：冻结游戏时间，继续 / 返回主菜单。</summary>
    public class GameplayPauseUI : MonoBehaviour
    {
        const string PanelName = "GameplayPausePanel";

        public static bool IsPaused { get; private set; }

        [SerializeField] GameObject panel;
        [SerializeField] Button continueButton;
        [SerializeField] Button mainMenuButton;

        bool _paused;
        float _timeScaleBeforePause = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoEnsureOnManagers()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name.Contains("MainMenu"))
                return;

            if (Object.FindObjectOfType<GameplayPauseUI>() != null)
                return;

            var host = GameObject.Find("Managers") ?? Object.FindObjectOfType<GameStateManager>()?.gameObject;
            if (host == null)
                return;

            host.AddComponent<GameplayPauseUI>();
        }

        void Awake()
        {
            EnsurePanelReady();
            SetPauseVisible(false);
        }

        void OnDestroy()
        {
            if (_paused)
                ResumeTime();
        }

        void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                if (_paused)
                    SetPauseVisible(false);
                return;
            }

            if (IsManualOpen())
                return;

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            EnsurePanelReady();
            if (panel == null)
                return;

            SetPauseVisible(!_paused);
        }

        static bool IsManualOpen()
        {
            var manual = GameObject.Find("ControlsManualPanel");
            return manual != null && manual.activeSelf;
        }

        void EnsurePanelReady()
        {
            if (panel == null)
            {
                var existing = GameObject.Find(PanelName);
                if (existing != null && existing.GetComponent<GraphicRaycaster>() == null)
                {
                    Object.Destroy(existing);
                    existing = null;
                }

                panel = existing != null ? existing : CreatePanel();
            }

            ResolveButtons();
            WireButtons();
        }

        void ResolveButtons()
        {
            if (panel == null)
                return;

            continueButton = panel.transform.Find("Box/ContinueButton")?.GetComponent<Button>()
                             ?? continueButton;
            mainMenuButton = panel.transform.Find("Box/MainMenuButton")?.GetComponent<Button>()
                             ?? mainMenuButton;
        }

        GameObject CreatePanel()
        {
            var rootCanvas = Object.FindObjectOfType<Canvas>();
            if (rootCanvas == null)
                return null;

            EnsureEventSystem();

            var panelGo = new GameObject(PanelName, typeof(RectTransform));
            panelGo.transform.SetParent(rootCanvas.transform, false);

            var pauseCanvas = panelGo.AddComponent<Canvas>();
            pauseCanvas.overrideSorting = true;
            pauseCanvas.sortingOrder = 500;
            panelGo.AddComponent<GraphicRaycaster>();

            var dim = panelGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
            Stretch(panelGo);

            var box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(panelGo.transform, false);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(520f, 320f);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.1f, 0.12f, 0.18f, 0.96f);
            boxImg.raycastTarget = true;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(box.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            titleRect.sizeDelta = new Vector2(-48f, 56f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            TmpFontHelper.SetUiText(title, GameUiCopy.PauseMenuTitle);
            title.fontSize = 36f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;

            continueButton = CreateMenuButton(box.transform, "ContinueButton", GameUiCopy.PauseContinueLabel,
                new Vector2(0f, -100f));
            mainMenuButton = CreateMenuButton(box.transform, "MainMenuButton", GameUiCopy.PauseMainMenuLabel,
                new Vector2(0f, -170f));

            panel = panelGo;
            panel.SetActive(false);
            TmpFontHelper.ApplyDefaultFontRecursive(panelGo);
            return panelGo;
        }

        static Button CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredY)
        {
            var btnGo = new GameObject(name, typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredY;
            rect.sizeDelta = new Vector2(400f, 52f);

            var image = btnGo.AddComponent<Image>();
            image.color = new Color(0.14f, 0.18f, 0.26f, 0.96f);
            image.raycastTarget = true;

            var button = btnGo.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = true;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.92f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            button.colors = colors;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);
            Stretch(textGo);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            TmpFontHelper.SetUiText(tmp, label);
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return button;
        }

        void WireButtons()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnClickContinue);
                continueButton.onClick.AddListener(OnClickContinue);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnClickMainMenu);
                mainMenuButton.onClick.AddListener(OnClickMainMenu);
            }
        }

        public void OnClickContinue()
        {
            SetPauseVisible(false);
        }

        public void OnClickMainMenu()
        {
            ResumeTime();
            if (panel != null)
                panel.SetActive(false);
            _paused = false;
            IsPaused = false;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.LoadMainMenu();
            else
                SceneManager.LoadScene(GameStateManager.DefaultMainMenuSceneName);
        }

        void SetPauseVisible(bool show)
        {
            EnsurePanelReady();
            if (panel == null)
                return;

            if (show == _paused)
                return;

            _paused = show;
            IsPaused = show;
            panel.SetActive(show);

            if (show)
            {
                panel.transform.SetAsLastSibling();
                PauseTime();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ResolveButtons();
                WireButtons();
                TmpFontHelper.ApplyDefaultFontRecursive(panel);
            }
            else
            {
                ResumeTime();
                if (!IsManualOpen())
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        void PauseTime()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;

            _timeScaleBeforePause = Time.timeScale > 0.001f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        void ResumeTime()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = _timeScaleBeforePause > 0.001f ? _timeScaleBeforePause : 1f;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        static void Stretch(GameObject go)
        {
            var rect = UiRectUtil.GetRectTransform(go);
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
