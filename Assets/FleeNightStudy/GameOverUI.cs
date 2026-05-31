using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace FleeNightStudy
{
    /// <summary>
    /// 胜负结果 UI：分开的胜利 / 失败面板、背景插画、淡入与可选 Animator。
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("面板")]
        [Tooltip("旧版单一面板；若已指定 victoryPanel / defeatPanel 则可留空")]
        [SerializeField] GameObject panelRoot;

        [SerializeField] GameObject victoryPanel;
        [SerializeField] GameObject defeatPanel;

        [Header("背景插画")]
        [SerializeField] Sprite victoryBackgroundSprite;
        [SerializeField] Sprite defeatBackgroundSprite;
        [SerializeField] Image victoryBackgroundImage;
        [SerializeField] Image defeatBackgroundImage;

        [Header("文案")]
        [SerializeField] Text messageText;
        [SerializeField] TMP_Text messageTextMeshPro;
        [SerializeField] TMP_Text victoryTextMeshPro;
        [SerializeField] TMP_Text defeatTextMeshPro;

        [SerializeField] string victoryMessage = "胜利：逃离晚自习！";
        [SerializeField] string gameOverMessage = "失败：被老师抓住。";

        [Header("入场动画（可选）")]
        [SerializeField] Animator victoryAnimator;
        [SerializeField] Animator defeatAnimator;
        [SerializeField] string showTriggerName = "Show";
        [SerializeField] bool animateFadeIn = true;
        [SerializeField] float fadeInDuration = 0.4f;

        Coroutine _fadeRoutine;

        void OnEnable()
        {
            if (FindObjectOfType<GameResultController>() != null)
                return;
            if (GameStateManager.Instance == null)
                return;
            GameStateManager.Instance.OnVictory += HandleVictory;
            GameStateManager.Instance.OnGameOver += HandleGameOver;
        }

        void OnDisable()
        {
            if (GameStateManager.Instance == null)
                return;
            GameStateManager.Instance.OnVictory -= HandleVictory;
            GameStateManager.Instance.OnGameOver -= HandleGameOver;
        }

        void HandleVictory() => ShowVictory();

        void HandleGameOver() => ShowDefeat();

        public void ShowVictory() => ShowResult(true);

        public void ShowDefeat() => ShowResult(false);

        void ShowResult(bool victory)
        {
            GameResultUiBootstrap.Ensure();

            var ui = GameResultUiBootstrap.GetPrimaryUi();
            if (ui == null)
                ui = this;
            if (ui == null)
                return;

            if (ui != this)
            {
                ui.ShowResultInternal(victory);
                return;
            }

            ShowResultInternal(victory);
        }

        void ShowResultInternal(bool victory)
        {
            HideAllPanels();
            GameResultUiBootstrap.NormalizeHierarchy(this);
            GameResultUiBootstrap.RemoveRedundantCenterWidgets(this);
            GameResultUiBootstrap.WireAllResultButtons(this);

            GameObject panel = victory ? GetVictoryPanel() : GetDefeatPanel();
            if (panel == null)
                return;

            gameObject.SetActive(true);
            SetAncestorsActive(panel.transform, true);
            StretchFull(panel.GetComponent<RectTransform>());
            if (!victory && panel.transform.parent != null)
                StretchFull(panel.transform.parent.GetComponent<RectTransform>());

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();

            ApplyBackground(victory, panel);
            ApplyMessage(victory, panel);
            TmpFontHelper.ApplyDefaultFontRecursive(panel);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var animator = victory ? victoryAnimator : defeatAnimator;
            if (animator != null && !string.IsNullOrEmpty(showTriggerName))
                animator.SetTrigger(showTriggerName);

            if (animateFadeIn)
            {
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = panel.AddComponent<CanvasGroup>();
                if (_fadeRoutine != null)
                    StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeInPanel(cg));
            }
        }

        static void SetAncestorsActive(Transform node, bool active)
        {
            while (node != null)
            {
                if (node.gameObject.activeSelf != active)
                    node.gameObject.SetActive(active);
                node = node.parent;
            }
        }

        void ApplyBackground(bool victory, GameObject panel)
        {
            var sprite = victory ? victoryBackgroundSprite : defeatBackgroundSprite;
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(victory
                    ? "FleeNightStudy/VictoryBackground"
                    : "FleeNightStudy/DefeatBackground");
            }

            var image = GetBackgroundImageTarget(victory, panel, out bool isRootPanelImage);
            if (image == null)
                return;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }
            else if (!isRootPanelImage)
            {
                image.sprite = null;
                image.color = victory
                    ? new Color(0.05f, 0.22f, 0.12f, 0.98f)
                    : new Color(0.22f, 0.05f, 0.05f, 0.98f);
            }

            var dim = panel.transform.Find("TextDimOverlay");
            if (dim != null)
                dim.gameObject.SetActive(true);
        }

        static Image GetBackgroundImageTarget(bool victory, GameObject panel, out bool isRootPanelImage)
        {
            isRootPanelImage = false;
            var bgTr = panel.transform.Find("BackgroundImage");
            if (bgTr != null)
                return bgTr.GetComponent<Image>();

            if (panel.GetComponent<Image>() != null)
                return CreateBackgroundImageChild(panel);

            return null;
        }

        static Image CreateBackgroundImageChild(GameObject panel)
        {
            var go = new GameObject("BackgroundImage", typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);
            go.transform.SetAsFirstSibling();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<Image>();
        }

        void ApplyMessage(bool victory, GameObject panel)
        {
            string message = victory ? victoryMessage : gameOverMessage;
            string stats = BuildStatsBlock(victory);

            var titleTmp = victory ? victoryTextMeshPro : defeatTextMeshPro;
            if (titleTmp == null)
                titleTmp = panel.transform.Find("TitleText")?.GetComponent<TMP_Text>();

            var statsTmp = panel.transform.Find("StatsText")?.GetComponent<TMP_Text>();

            if (titleTmp != null && statsTmp != null)
            {
                TmpFontHelper.SetUiText(titleTmp, message);
                TmpFontHelper.SetUiText(statsTmp, stats);
                return;
            }

            string body = message + "\n\n" + stats;
            var panelTmp = titleTmp;
            if (panelTmp == null)
                panelTmp = messageTextMeshPro;
            if (panelTmp == null)
                panelTmp = panel.transform.Find("ResultText")?.GetComponent<TMP_Text>();

            if (panelTmp != null)
            {
                TmpFontHelper.SetUiText(panelTmp, body);
                EnsureLegacyTextLayout(panelTmp.rectTransform);
            }
            else if (messageText != null)
            {
                messageText.text = body;
            }
            else
            {
                var tmp = panel.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                    TmpFontHelper.SetUiText(tmp, body);
            }
        }

        static void StretchFull(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void EnsureLegacyTextLayout(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0.5f, 0.55f);
            rect.anchorMax = new Vector2(0.5f, 0.55f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 380f);
            rect.anchoredPosition = Vector2.zero;
        }

        static string BuildStatsBlock(bool victory)
        {
            int min = (int)(GameResultStats.ElapsedSeconds / 60f);
            int sec = (int)(GameResultStats.ElapsedSeconds % 60f);
            string diff = GameResultStats.Difficulty == GameDifficulty.Easy ? "简单" : "普通";
            string playerName = ResolvePlayerDisplayName();

            string block =
                $"玩家：{playerName}\n" +
                $"难度：{diff}\n" +
                $"收集课本：{GameResultStats.TextbooksCollected}\n" +
                $"用时：{min:00}:{sec:00}\n" +
                $"评分：{GameResultStats.Score}";

            if (victory)
                block += "\n\n" + LeaderboardManager.FormatLeaderboardText(GameResultStats.Difficulty);

            return block;
        }

        static string ResolvePlayerDisplayName()
        {
            string name = GameSessionData.PlayerName;
            if (string.IsNullOrWhiteSpace(name))
                name = GameResultStats.PlayerName;
            if (string.IsNullOrWhiteSpace(name) && GameStateManager.Instance != null)
                name = GameStateManager.Instance.PlayerName;
            return string.IsNullOrWhiteSpace(name) ? "玩家" : name.Trim();
        }

        GameObject GetVictoryPanel()
        {
            if (victoryPanel != null)
                return victoryPanel;
            return panelRoot != null ? panelRoot : gameObject;
        }

        GameObject GetDefeatPanel()
        {
            if (defeatPanel != null)
                return defeatPanel;

            var child = transform.Find("DefeatPanel");
            if (child != null)
                return child.gameObject;

            if (panelRoot != null)
                return panelRoot;

            return gameObject;
        }

        void HideAllPanels()
        {
            if (victoryPanel != null)
                victoryPanel.SetActive(false);

            var builtDefeat = transform.Find("DefeatPanel");
            if (builtDefeat != null)
                builtDefeat.gameObject.SetActive(false);
            else if (defeatPanel != null)
                defeatPanel.SetActive(false);

            if (victoryPanel == null && builtDefeat == null && defeatPanel == null)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                else
                    gameObject.SetActive(false);
            }
        }

        IEnumerator FadeInPanel(CanvasGroup cg)
        {
            cg.alpha = 0f;
            float t = 0f;
            float dur = Mathf.Max(0.01f, fadeInDuration);
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / dur);
                yield return null;
            }

            cg.alpha = 1f;
            _fadeRoutine = null;
        }

        public void OnClickRestart()
        {
            Time.timeScale = 1f;
            MenuCursorPolicy.Unlock();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ReloadScene();
                return;
            }

            SceneManager.LoadScene(GameStateManager.DefaultGameplaySceneName);
        }

        public void OnClickExitToMainMenu()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.LoadMainMenu();
            else
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(GameStateManager.DefaultMainMenuSceneName);
            }
        }
    }
}
