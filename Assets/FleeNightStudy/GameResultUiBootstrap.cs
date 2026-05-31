using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FleeNightStudy
{
    /// <summary>
    /// MergeV2 等场景缺少 GameResultUI，且 GameUI_Canvas 可能 scale=0。
    /// 进入玩法场景后自动修复 Canvas，并创建与编辑器「一键设置」一致的胜负 UI。
    /// </summary>
    public static class GameResultUiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            if (!ShouldBootstrap())
                return;

            FixOverlayCanvases();
            Ensure();
        }

        static bool ShouldBootstrap()
        {
            return GameObject.Find("Managers") != null
                   && Object.FindObjectOfType<GameStateManager>(true) != null;
        }

        public static void FixOverlayCanvases()
        {
            var overlays = new System.Collections.Generic.List<Canvas>();
            foreach (var c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;

                var tr = c.transform;
                if (tr.parent != null)
                    tr.SetParent(null);

                if (tr.localScale.sqrMagnitude < 0.01f)
                    tr.localScale = Vector3.one;

                overlays.Add(c);
            }

            Canvas primary = null;
            foreach (var c in overlays)
            {
                if (c.name != "GameUI_Canvas")
                    continue;
                if (primary == null || c.transform.childCount > primary.transform.childCount)
                    primary = c;
            }

            if (primary == null && overlays.Count > 0)
                primary = overlays[0];

            if (primary == null)
                return;

            foreach (var c in overlays)
            {
                if (c == primary)
                    continue;

                while (c.transform.childCount > 0)
                {
                    var child = c.transform.GetChild(0);
                    child.SetParent(primary.transform, false);
                }

                Object.Destroy(c.gameObject);
            }

            primary.gameObject.SetActive(true);
            var scaler = primary.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        public static void Ensure()
        {
            if (!ShouldBootstrap())
                return;

            FixOverlayCanvases();
            DisableLegacyGameOverPanels();

            var ui = GetPrimaryUi();
            if (ui != null && HasValidPanels(ui))
            {
                NormalizeHierarchy(ui);
                RemoveRedundantCenterWidgets(ui);
                WireAllResultButtons(ui);
                return;
            }

            var canvas = FindPrimaryCanvasTransform();
            if (canvas == null)
                return;

            if (ui != null)
                SafeDestroy(ui.gameObject);

            CreateGameResultUi(canvas);
        }

        public static GameOverUI GetPrimaryUi()
        {
            var root = GameObject.Find("GameResultUI");
            if (root != null)
            {
                var onRoot = root.GetComponent<GameOverUI>();
                if (onRoot != null && HasValidPanels(onRoot))
                    return onRoot;
            }

            GameOverUI best = null;
            foreach (var ui in Object.FindObjectsOfType<GameOverUI>(true))
            {
                if (ui == null || ui.gameObject.name == "GameOverPanel")
                    continue;
                if (!HasValidPanels(ui))
                    continue;
                return ui;
            }

            return best;
        }

        public static bool HasValidPanels(GameOverUI ui)
        {
            if (ui == null)
                return false;

            var defeat = ui.transform.Find("DefeatPanel");
            if (defeat == null)
                return false;

            return defeat.Find("StatsText") != null
                   && defeat.Find("BottomActionBar") != null
                   && defeat.Find("TitleText") != null;
        }

        static void DisableLegacyGameOverPanels()
        {
            foreach (var ui in Object.FindObjectsOfType<GameOverUI>(true))
            {
                if (ui == null || ui.gameObject.name != "GameOverPanel")
                    continue;
                ui.gameObject.SetActive(false);
            }
        }

        static Transform FindPrimaryCanvasTransform()
        {
            foreach (var c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;
                if (c.name == "GameUI_Canvas")
                    return c.transform;
            }

            var any = Object.FindObjectOfType<Canvas>();
            return any != null ? any.transform : null;
        }

        static void CreateGameResultUi(Transform canvas)
        {
            var controller = new GameObject("GameResultUI", typeof(RectTransform));
            controller.transform.SetParent(canvas, false);
            StretchFull(controller.GetComponent<RectTransform>());

            var ui = controller.AddComponent<GameOverUI>();
            controller.AddComponent<GameResultAudio>();

            var victoryPanel = CreateResultPanel(
                controller.transform,
                "VictoryPanel",
                "胜利：逃离晚自习！",
                out TMP_Text victoryTmp,
                out Image victoryBg,
                out Button victoryExit);

            var defeatPanel = CreateResultPanel(
                controller.transform,
                "DefeatPanel",
                "失败：被老师抓住。",
                out TMP_Text defeatTmp,
                out Image defeatBg,
                out Button defeatExit);

            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            controller.SetActive(true);

            WireAllResultButtons(ui);

            BindUiFields(ui, victoryPanel, defeatPanel, victoryTmp, defeatTmp, victoryBg, defeatBg);

            var spriteVictory = Resources.Load<Sprite>("FleeNightStudy/VictoryBackground");
            var spriteDefeat = Resources.Load<Sprite>("FleeNightStudy/DefeatBackground");
            if (spriteVictory != null && victoryBg != null)
            {
                victoryBg.sprite = spriteVictory;
                victoryBg.color = Color.white;
            }

            if (spriteDefeat != null && defeatBg != null)
            {
                defeatBg.sprite = spriteDefeat;
                defeatBg.color = Color.white;
            }

            NormalizeHierarchy(ui);
            TmpFontHelper.ApplyDefaultFontRecursive(controller);
        }

        /// <summary>把 GameResultUI 挂到 GameUI_Canvas 并铺满屏幕（修复一键设置后根节点默认 100×100 的问题）。</summary>
        public static void NormalizeHierarchy(GameOverUI ui)
        {
            if (!IsAlive(ui))
                return;

            FixOverlayCanvases();

            if (!IsAlive(ui))
                return;

            var canvas = FindPrimaryCanvasTransform();
            if (canvas == null || !IsAlive(canvas))
                return;

            var tr = ui.transform;
            if (!IsAlive(tr))
                return;

            if (tr.parent != canvas)
                tr.SetParent(canvas, false);

            tr.localScale = Vector3.one;
            tr.localRotation = Quaternion.identity;
            tr.localPosition = Vector3.zero;

            var rootRect = tr as RectTransform ?? ui.gameObject.AddComponent<RectTransform>();
            StretchFull(rootRect);
            rootRect.SetAsLastSibling();

            foreach (var panelName in new[] { "VictoryPanel", "DefeatPanel" })
            {
                var panel = SafeFind(tr, panelName);
                if (!IsAlive(panel))
                    continue;

                var panelRect = panel as RectTransform;
                if (panelRect != null)
                    StretchFull(panelRect);

                RemoveRedundantCenterWidgetsOnPanel(panel);
                FixExistingTextLayout(panel);
            }
        }

        static bool IsAlive(Object obj)
        {
            return obj != null;
        }

        static bool IsAlive(Transform tr)
        {
            return tr != null;
        }

        static Transform SafeFind(Transform parent, string childName)
        {
            if (!IsAlive(parent))
                return null;

            try
            {
                return parent.Find(childName);
            }
            catch (MissingReferenceException)
            {
                return null;
            }
        }

        /// <summary>去掉中间重复的副标题与「重新开始」按钮（保留底部操作栏）。</summary>
        public static void RemoveRedundantCenterWidgets(GameOverUI ui)
        {
            if (!IsAlive(ui))
                return;

            foreach (var panelName in new[] { "VictoryPanel", "DefeatPanel" })
            {
                var panel = SafeFind(ui.transform, panelName);
                if (IsAlive(panel))
                    RemoveRedundantCenterWidgetsOnPanel(panel);
            }
        }

        static void RemoveRedundantCenterWidgetsOnPanel(Transform panel)
        {
            if (!IsAlive(panel))
                return;

            DestroyIfExists(panel, "SubtitleText");
            DestroyIfExists(panel, "CenterButtons");
            DestroyIfExists(panel, "RestartButton");
        }

        static void SafeDestroy(GameObject go)
        {
            if (!IsAlive(go))
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(go);
            else
#endif
                Object.Destroy(go);
        }

        static void DestroyIfExists(Transform parent, string childName)
        {
            var child = SafeFind(parent, childName);
            if (!IsAlive(child))
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(child.gameObject);
            else
#endif
                Object.Destroy(child.gameObject);
        }

        static void FixExistingTextLayout(Transform panel)
        {
            if (!IsAlive(panel))
                return;

            FixTextRect(SafeFind(panel, "TitleText") as RectTransform, new Vector2(0.5f, 0.78f), new Vector2(680f, 100f), 0.5f);
            FixTextRect(SafeFind(panel, "StatsText") as RectTransform, new Vector2(0.5f, 0.58f), new Vector2(680f, 200f), 1f);

            var bottom = SafeFind(panel, "BottomActionBar") as RectTransform;
            if (bottom != null)
            {
                bottom.anchorMin = new Vector2(0f, 0f);
                bottom.anchorMax = new Vector2(1f, 0f);
                bottom.pivot = new Vector2(0.5f, 0f);
                bottom.anchoredPosition = Vector2.zero;
                bottom.sizeDelta = new Vector2(0f, 120f);
            }
        }

        static void FixTextRect(RectTransform rect, Vector2 anchor, Vector2 size, float pivotY)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, pivotY);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        public static void WireAllResultButtons(GameOverUI ui)
        {
            if (!IsAlive(ui))
                return;

            foreach (var panelName in new[] { "VictoryPanel", "DefeatPanel" })
            {
                var panel = SafeFind(ui.transform, panelName);
                if (!IsAlive(panel))
                    continue;

                var restart = panel.Find("BottomActionBar/BottomRestartButton")?.GetComponent<Button>();
                if (restart != null)
                {
                    restart.onClick.RemoveListener(ui.OnClickRestart);
                    restart.onClick.AddListener(ui.OnClickRestart);
                }

                var mainMenu = panel.Find("BottomActionBar/MainMenuButton")?.GetComponent<Button>();
                if (mainMenu != null)
                {
                    mainMenu.onClick.RemoveListener(ui.OnClickExitToMainMenu);
                    mainMenu.onClick.AddListener(ui.OnClickExitToMainMenu);
                }
            }
        }

        static void BindUiFields(GameOverUI ui, GameObject victoryPanel, GameObject defeatPanel,
            TMP_Text victoryTmp, TMP_Text defeatTmp, Image victoryBg, Image defeatBg)
        {
            var type = typeof(GameOverUI);
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

            type.GetField("victoryPanel", flags)?.SetValue(ui, victoryPanel);
            type.GetField("defeatPanel", flags)?.SetValue(ui, defeatPanel);
            type.GetField("victoryTextMeshPro", flags)?.SetValue(ui, victoryTmp);
            type.GetField("defeatTextMeshPro", flags)?.SetValue(ui, defeatTmp);
            type.GetField("victoryBackgroundImage", flags)?.SetValue(ui, victoryBg);
            type.GetField("defeatBackgroundImage", flags)?.SetValue(ui, defeatBg);
        }

        static GameObject CreateResultPanel(
            Transform parent,
            string panelName,
            string title,
            out TMP_Text titleTmp,
            out Image backgroundImage,
            out Button exitButton)
        {
            var panel = new GameObject(panelName, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());
            panel.AddComponent<CanvasGroup>();

            var bgGo = new GameObject("BackgroundImage", typeof(RectTransform));
            bgGo.transform.SetParent(panel.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            backgroundImage = bgGo.AddComponent<Image>();
            backgroundImage.color = panelName.StartsWith("Victory")
                ? new Color(0.05f, 0.22f, 0.12f, 1f)
                : new Color(0.22f, 0.05f, 0.05f, 0.98f);

            var dimGo = new GameObject("TextDimOverlay", typeof(RectTransform));
            dimGo.transform.SetParent(panel.transform, false);
            StretchFull(dimGo.GetComponent<RectTransform>());
            dimGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.42f);

            titleTmp = CreateAnchoredText(panel.transform, "TitleText", title, 40f,
                new Vector2(0.5f, 0.78f), new Vector2(680f, 100f), FontStyles.Bold, TextAlignmentOptions.Center, 0.5f);

            CreateAnchoredText(panel.transform, "StatsText", "", 28f,
                new Vector2(0.5f, 0.58f), new Vector2(680f, 200f), FontStyles.Normal, TextAlignmentOptions.Center, 1f);

            var bottomGo = new GameObject("BottomActionBar", typeof(RectTransform));
            bottomGo.transform.SetParent(panel.transform, false);
            var bottomRect = bottomGo.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.anchoredPosition = Vector2.zero;
            bottomRect.sizeDelta = new Vector2(0f, 120f);
            bottomGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var bottomLayout = bottomGo.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.spacing = 28f;
            bottomLayout.padding = new RectOffset(40, 40, 18, 18);
            bottomLayout.childControlWidth = false;
            bottomLayout.childControlHeight = true;

            CreateBarButton(bottomGo.transform, "BottomRestartButton", "重新开始",
                new Color(0.25f, 0.75f, 0.45f, 1f), 260f);
            exitButton = CreateBarButton(bottomGo.transform, "MainMenuButton", "返回主菜单",
                new Color(0.38f, 0.38f, 0.44f, 1f), 260f);

            return panel;
        }

        static TMP_Text CreateAnchoredText(Transform parent, string name, string text, float size,
            Vector2 anchor, Vector2 sizeDelta, FontStyles style, TextAlignmentOptions align, float pivotY)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, pivotY);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            TmpFontHelper.SetUiText(tmp, text);
            return tmp;
        }

        static Button CreateBarButton(Transform parent, string objectName, string label, Color color, float width)
        {
            var btnGo = new GameObject(objectName, typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);
            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minHeight = 56f;
            le.preferredHeight = 56f;

            btnGo.AddComponent<Image>().color = color;
            var button = btnGo.AddComponent<Button>();

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            StretchFull(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 26f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            TmpFontHelper.SetUiText(tmp, label);

            return button;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
