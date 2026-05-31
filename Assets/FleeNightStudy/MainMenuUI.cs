using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace FleeNightStudy
{
    /// <summary>
    /// 主菜单：输入名字 → 开始游戏 / 排行榜 / 操作说明 / 退出。
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        const string BackgroundResourcePath = "FleeNightStudy/MainMenuBackground";

        [SerializeField] string gameplaySceneName = GameStateManager.DefaultGameplaySceneName;
        [SerializeField] Image backgroundImage;
        [SerializeField] GameObject menuStack;
        [SerializeField] GameObject leaderboardPanel;
        [SerializeField] GameObject difficultySelectPanel;
        [SerializeField] GameObject instructionsPanel;
        [SerializeField] TMP_InputField playerNameInput;
        [SerializeField] TMP_Text nameHintText;
        [SerializeField] TMP_Text leaderboardText;

        void Awake()
        {
            FixMainMenuCanvas();
            ResolveReferences();
            DisableOverlayRaycasts();
            WireAllButtons();
        }

        void OnEnable() => MenuCursorPolicy.Unlock();

        void Start()
        {
            Time.timeScale = 1f;
            MenuCursorPolicy.Unlock();
            MainMenuCamera.EnsureExists();
            TmpFontHelper.ApplyDefaultFontRecursive(gameObject);
            ApplyBackgroundIfNeeded();
            EnsureModalPanelReadable(leaderboardPanel);
            EnsureModalPanelReadable(difficultySelectPanel);
            EnsureModalPanelReadable(instructionsPanel);
            ShowMainMenu();
        }

        void ResolveReferences()
        {
            if (menuStack == null)
                menuStack = transform.Find("MenuStack")?.gameObject;
            if (leaderboardPanel == null)
                leaderboardPanel = transform.Find("LeaderboardPanel")?.gameObject;
            if (difficultySelectPanel == null)
                difficultySelectPanel = transform.Find("DifficultySelectPanel")?.gameObject;
            if (instructionsPanel == null)
                instructionsPanel = transform.Find("InstructionsPanel")?.gameObject;
            if (playerNameInput == null)
                playerNameInput = transform.Find("MenuStack/PlayerNameInput")?.GetComponent<TMP_InputField>();
            if (nameHintText == null)
                nameHintText = transform.Find("MenuStack/NameHint")?.GetComponent<TMP_Text>();
            if (leaderboardText == null)
                leaderboardText = transform.Find("LeaderboardPanel/Box/LeaderboardScroll/Viewport/Content/Body")?.GetComponent<TMP_Text>()
                                  ?? transform.Find("LeaderboardPanel/Box/BodyViewport/Body")?.GetComponent<TMP_Text>()
                                  ?? transform.Find("LeaderboardPanel/Box/Body")?.GetComponent<TMP_Text>();
            if (backgroundImage == null)
                backgroundImage = transform.Find("BackgroundImage")?.GetComponent<Image>();
        }

        void ShowMainMenu()
        {
            if (menuStack != null) menuStack.SetActive(true);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
            if (difficultySelectPanel != null) difficultySelectPanel.SetActive(false);
            if (instructionsPanel != null) instructionsPanel.SetActive(false);
            ClearNameHint();
        }

        void HideMenuStack()
        {
            if (menuStack != null)
                menuStack.SetActive(false);
        }

        void DisableOverlayRaycasts()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.GetComponent<Button>() != null)
                    continue;
                if (graphic.GetComponentInParent<Button>() != null && graphic.gameObject.name == "Text")
                {
                    graphic.raycastTarget = false;
                    continue;
                }

                if (graphic.name is "BackgroundImage" or "VignetteOverlay" or "AccentGlow")
                    graphic.raycastTarget = false;
            }
        }

        void WireAllButtons()
        {
            WireByPath("MenuStack/开始游戏Button", OnClickStartGame);
            WireByPath("MenuStack/排行榜Button", OnClickShowLeaderboard);
            WireByPath("MenuStack/操作说明Button", OnClickShowInstructions);
            WireByPath("MenuStack/退出Button", OnClickQuit);

            WireByPath("DifficultySelectPanel/Box/简单难度Button", () => StartWithDifficulty(GameDifficulty.Easy));
            WireByPath("DifficultySelectPanel/Box/普通难度Button", () => StartWithDifficulty(GameDifficulty.Normal));
            WireByPath("DifficultySelectPanel/Box/返回Button", OnClickCancelDifficulty);

            WireByPath("LeaderboardPanel/Box/简单难度Button", () => ShowLeaderboardForMode(GameDifficulty.Easy));
            WireByPath("LeaderboardPanel/Box/普通难度Button", () => ShowLeaderboardForMode(GameDifficulty.Normal));
            WireByPath("LeaderboardPanel/Box/返回Button", OnClickCloseLeaderboard);

            WireByPath("InstructionsPanel/Box/InstructionsBackButton", OnClickCloseInstructions);
            WireByPath("InstructionsPanel/Box/返回Button", OnClickCloseInstructions);
        }

        void WireByPath(string path, UnityAction action)
        {
            var tr = transform.Find(path);
            if (tr == null)
                return;
            var btn = tr.GetComponent<Button>();
            if (btn != null)
                ReplaceClick(btn, action);
        }

        static void ReplaceClick(Button button, UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.raycastTarget = false;
            var img = button.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = true;
        }

        void ApplyBackgroundIfNeeded()
        {
            if (backgroundImage == null || backgroundImage.sprite != null)
                return;

            var sprite = Resources.Load<Sprite>(BackgroundResourcePath);
            if (sprite == null)
                return;

            backgroundImage.sprite = sprite;
            backgroundImage.color = Color.white;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;
        }

        public static void FixMainMenuCanvas()
        {
            foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;

                var tr = canvas.transform;
                if (tr.parent != null)
                    tr.SetParent(null);

                if (tr.localScale != Vector3.one)
                    tr.localScale = Vector3.one;

                canvas.gameObject.SetActive(true);

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        void ClearNameHint()
        {
            if (nameHintText != null)
                nameHintText.text = "";
        }

        void ShowNameHint(string message)
        {
            if (nameHintText != null)
                nameHintText.text = message;
            else
                Debug.LogWarning("[MainMenuUI] " + message);
        }

        bool TryGetPlayerName(out string playerName)
        {
            playerName = playerNameInput != null ? playerNameInput.text : null;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                ShowNameHint("请先输入玩家名字");
                return false;
            }

            playerName = playerName.Trim();
            ClearNameHint();
            return true;
        }

        public void OnClickStartGame()
        {
            if (!TryGetPlayerName(out _))
                return;

            HideMenuStack();
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
            if (instructionsPanel != null)
                instructionsPanel.SetActive(false);

            if (difficultySelectPanel != null)
            {
                EnsureModalPanelReadable(difficultySelectPanel);
                difficultySelectPanel.SetActive(true);
                return;
            }

            Debug.LogWarning("[MainMenuUI] 未找到 DifficultySelectPanel，默认简单难度。请运行「重建主菜单」。");
            StartWithDifficulty(GameDifficulty.Easy);
        }

        public void OnClickCancelDifficulty()
        {
            ShowMainMenu();
        }

        void StartWithDifficulty(GameDifficulty difficulty)
        {
            if (!TryGetPlayerName(out string playerName))
            {
                ShowMainMenu();
                return;
            }

            Time.timeScale = 1f;
            GameSessionData.Difficulty = difficulty;
            GameSessionData.PlayerName = playerName;
            Debug.Log($"[MainMenuUI] 开始游戏 → {difficulty} / {playerName}");

            if (TryLoadScene(gameplaySceneName))
                return;

            if (TryLoadScene(GameStateManager.DefaultGameplaySceneName))
                return;

            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                SceneManager.LoadScene(1);
                return;
            }

            Debug.LogError("[MainMenuUI] 无法加载玩法场景。请在 Build Settings 中加入 MergeV2。");
            ShowMainMenu();
        }

        static bool TryLoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return true;
            }

            int buildIndex = FindSceneBuildIndex(sceneName);
            if (buildIndex >= 0)
            {
                SceneManager.LoadScene(buildIndex);
                return true;
            }

            return false;
        }

        static int FindSceneBuildIndex(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName == sceneName || path.EndsWith(sceneName + ".unity"))
                    return i;
            }
            return -1;
        }

        public void OnClickShowLeaderboard()
        {
            ResolveReferences();
            WireByPath("LeaderboardPanel/Box/简单难度Button", () => ShowLeaderboardForMode(GameDifficulty.Easy));
            WireByPath("LeaderboardPanel/Box/普通难度Button", () => ShowLeaderboardForMode(GameDifficulty.Normal));
            WireByPath("LeaderboardPanel/Box/返回Button", OnClickCloseLeaderboard);

            if (leaderboardPanel == null)
            {
                Debug.LogWarning("[MainMenuUI] 未找到 LeaderboardPanel，请运行「重建主菜单」。");
                return;
            }

            HideMenuStack();
            if (difficultySelectPanel != null)
                difficultySelectPanel.SetActive(false);
            if (instructionsPanel != null)
                instructionsPanel.SetActive(false);

            EnsureModalPanelReadable(leaderboardPanel);
            EnsureLeaderboardLayout(leaderboardPanel);

            var box = leaderboardPanel.transform.Find("Box");
            if (box != null)
                leaderboardText = LeaderboardScrollHelper.EnsureScrollBody(box, out _);

            if (leaderboardText != null)
                leaderboardText.text = "请选择要查看的难度";

            leaderboardPanel.SetActive(true);
        }

        static void EnsureLeaderboardLayout(GameObject panel)
        {
            if (panel == null)
                return;

            var box = panel.transform.Find("Box");
            if (box == null)
                return;

            var boxRect = box.GetComponent<RectTransform>();
            if (boxRect != null)
                boxRect.sizeDelta = new Vector2(620f, 820f);

            var body = LeaderboardScrollHelper.EnsureScrollBody(box, out _);
            if (body != null)
                TmpFontHelper.ApplyDefaultFontRecursive(body.gameObject);
        }

        void ShowLeaderboardForMode(GameDifficulty difficulty)
        {
            var box = leaderboardPanel?.transform.Find("Box");
            if (box != null)
                leaderboardText = LeaderboardScrollHelper.EnsureScrollBody(box, out _);

            if (leaderboardText != null)
            {
                leaderboardText.text = LeaderboardManager.FormatLeaderboardText(difficulty);
                Canvas.ForceUpdateCanvases();
                LeaderboardScrollHelper.RefreshScrollContent(leaderboardText);
                StartCoroutine(RefreshLeaderboardScrollNextFrame(leaderboardText));
            }
        }

        static System.Collections.IEnumerator RefreshLeaderboardScrollNextFrame(TMP_Text body)
        {
            yield return null;
            if (body != null)
                LeaderboardScrollHelper.RefreshScrollContent(body);
        }

        public void OnClickCloseLeaderboard()
        {
            ShowMainMenu();
        }

        public void OnClickShowInstructions()
        {
            ResolveReferences();
            WireByPath("InstructionsPanel/Box/InstructionsBackButton", OnClickCloseInstructions);
            WireByPath("InstructionsPanel/Box/返回Button", OnClickCloseInstructions);

            if (instructionsPanel == null)
            {
                Debug.LogWarning("[MainMenuUI] 未找到 InstructionsPanel，请运行「重建主菜单」。");
                return;
            }

            HideMenuStack();
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
            if (difficultySelectPanel != null)
                difficultySelectPanel.SetActive(false);
            EnsureModalPanelReadable(instructionsPanel);
            instructionsPanel.SetActive(true);
        }

        public void OnClickCloseInstructions()
        {
            ShowMainMenu();
        }

        public void OnClickQuit()
        {
            GameStateManager.QuitApplication();
        }

        static void EnsureModalPanelReadable(GameObject panel)
        {
            if (panel == null)
                return;

            var box = panel.transform.Find("Box");
            if (box == null)
                return;

            var boxRect = box.GetComponent<RectTransform>();
            if (boxRect != null)
            {
                if (panel.name == "LeaderboardPanel")
                {
                    boxRect.sizeDelta = new Vector2(620f, 820f);
                    EnsureLeaderboardLayout(panel);
                }
                else if (panel.name == "InstructionsPanel")
                    boxRect.sizeDelta = new Vector2(760f, 640f);
                else if (boxRect.sizeDelta.y < 560f)
                    boxRect.sizeDelta = new Vector2(Mathf.Max(boxRect.sizeDelta.x, 580f), 600f);
            }

            var bodyElement = box.Find("BodyViewport")?.GetComponent<LayoutElement>()
                              ?? box.Find("Body")?.GetComponent<LayoutElement>();
            if (bodyElement != null && panel.name == "LeaderboardPanel")
            {
                bodyElement.preferredHeight = LeaderboardScrollHelper.ScrollAreaPreferredHeight;
                bodyElement.minHeight = 220f;
            }
            else if (bodyElement != null && panel.name == "InstructionsPanel")
            {
                bodyElement.preferredHeight = 360f;
            }

            foreach (var tmp in box.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject.name == "Body")
                {
                    tmp.color = new Color(0.08f, 0.09f, 0.12f, 1f);
                    if (tmp.fontSize < 20f)
                        tmp.fontSize = 22f;
                    continue;
                }

                if (tmp.gameObject.name == "TitleText")
                {
                    tmp.color = new Color(0.06f, 0.07f, 0.10f, 1f);
                }
            }

            foreach (var button in box.GetComponentsInChildren<Button>(true))
                ApplyModalButtonStyle(button);
        }

        static void ApplyModalButtonStyle(Button button)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.14f, 0.18f, 0.26f, 0.96f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.92f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;

            label.color = new Color(0.96f, 0.97f, 1f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
        }
    }

    /// <summary>主菜单必须显示鼠标；从玩法场景返回时恢复。</summary>
    public static class MenuCursorPolicy
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryUnlockForActiveScene();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryUnlockForScene(scene);

        public static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static void TryUnlockForActiveScene() => TryUnlockForScene(SceneManager.GetActiveScene());

        static void TryUnlockForScene(Scene scene)
        {
            if (scene.name.Contains("MainMenu") || scene.name.Contains("Menu"))
                Unlock();
        }
    }

    /// <summary>主菜单场景需有 Camera，否则 Game 视图会显示 Display 1 提示。</summary>
    public static class MainMenuCamera
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnSceneLoaded()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name == "MainMenu" || scene.name.Contains("MainMenu"))
            {
                MenuCursorPolicy.Unlock();
                EnsureExists();
            }
        }

        public static void EnsureExists()
        {
            MainMenuUI.FixMainMenuCanvas();

            if (Camera.main != null)
                return;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            cam.cullingMask = 0;
            cam.depth = -10;
            cam.orthographic = true;
            cam.enabled = true;

            var hdrp = System.Type.GetType(
                "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdrp != null && camGo.GetComponent(hdrp) == null)
                camGo.AddComponent(hdrp);
        }
    }
}
