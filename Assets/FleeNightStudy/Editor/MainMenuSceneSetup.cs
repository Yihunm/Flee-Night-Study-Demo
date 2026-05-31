#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using FleeNightStudy;
using FleeNightStudy.Editor;

namespace FleeNightStudy.EditorTools
{
    public static class MainMenuSceneSetup
    {
        const string MainMenuScenePath = "Assets/FleeNightStudy/Scenes/MainMenu.unity";
        const string GameplayScenePath = "Assets/AssetStoreOriginals/School/Scenes/MergeV2.unity";
        const string BackgroundPath = "Assets/FleeNightStudy/UI/MainMenuBackground.png";
        const string ResourcesBackgroundPath = "Assets/FleeNightStudy/Resources/FleeNightStudy/MainMenuBackground.png";

        static readonly Color TitleGreen = new Color(0.55f, 0.92f, 0.18f, 1f);
        static readonly Color MenuButtonText = new Color(0.96f, 0.97f, 1f, 1f);
        static readonly Color ModalBodyText = new Color(0.08f, 0.09f, 0.12f, 1f);
        static readonly Color ModalTitleText = new Color(0.06f, 0.07f, 0.10f, 1f);

        [MenuItem("FleeNightStudy/重建主菜单")]
        public static void RebuildMainMenuSceneMenu()
        {
            if (BlockIfPlaying())
                return;
            RebuildMainMenuScene(true);
        }

        [MenuItem("FleeNightStudy/重建主菜单", true)]
        static bool RebuildMainMenuSceneMenuValidate() => !EditorApplication.isPlaying;

        [MenuItem("FleeNightStudy/修复主菜单显示")]
        public static void FixMainMenuDisplayMenu()
        {
            if (BlockIfPlaying())
                return;

            EnsureBuildSettings();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name.Contains("MainMenu"))
            {
                var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
                if (menu != null)
                    TryApplyMainMenuFonts(menu.gameObject);
                MainMenuUI.FixMainMenuCanvas();
                SaveMainMenuScene(scene);
            }

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "已设置 Build：0=MainMenu，1=MergeV2，并尝试修复主菜单字体。\n" +
                "若仍缺按钮，请运行「重建主菜单」。",
                "确定");
        }

        [MenuItem("FleeNightStudy/修复主菜单显示", true)]
        static bool FixMainMenuDisplayMenuValidate() => !EditorApplication.isPlaying;

        public static void RebuildMainMenuScene(bool showDialog)
        {
            if (EditorApplication.isPlaying)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "FleeNightStudy",
                        "请先停止 Play 模式，再运行「重建主菜单」。",
                        "确定");
                return;
            }

            EnsureFolder("Assets/FleeNightStudy/Scenes");
            MirrorBackgroundToResources();
            ChineseTmpFontSetup.SanitizeDefaultProjectFonts();
            if (ChineseTmpFontSetup.GetUiFontAsset() == null)
                ChineseTmpFontSetup.EmergencyRepairTmpFonts(showDialog: false, applyAllScenes: false);

            var scene = OpenOrCreateMainMenuScene();
            ClearSceneRoots(scene);

            GameObject menuRoot;
            try
            {
                menuRoot = BuildMainMenuHierarchy();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FleeNightStudy] 主菜单构建失败：{ex}");
                if (showDialog)
                    EditorUtility.DisplayDialog("FleeNightStudy", $"主菜单构建失败，请查看 Console：\n{ex.Message}", "确定");
                return;
            }

            TryApplyMainMenuFonts(menuRoot);
            MainMenuUI.FixMainMenuCanvas();

            if (!SaveMainMenuScene(scene))
            {
                Debug.LogError("[FleeNightStudy] 主菜单保存失败。");
                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "FleeNightStudy",
                        "主菜单保存失败。\n请确认 MainMenu 场景未在其他窗口锁定，然后重试。",
                        "确定");
                return;
            }

            EnsureBuildSettings();

            if (showDialog)
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    "主菜单已重建并保存。\n当前场景：MainMenu\nBuild：0=MainMenu，1=MergeV2",
                    "确定");
        }

        static bool BlockIfPlaying()
        {
            if (!EditorApplication.isPlaying)
                return false;

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "请先停止 Play 模式，再运行此菜单项。",
                "确定");
            return true;
        }

        static Scene OpenOrCreateMainMenuScene()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("不能在 Play 模式下打开/重建主菜单场景。");

            bool exists = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(MainMenuScenePath));
            if (exists)
                return EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, MainMenuScenePath))
                throw new InvalidOperationException($"无法创建场景文件：{MainMenuScenePath}");

            AssetDatabase.Refresh();
            return EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        static void ClearSceneRoots(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                UnityEngine.Object.DestroyImmediate(root);
        }

        static bool SaveMainMenuScene(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);

            bool saved = !string.IsNullOrEmpty(scene.path)
                ? EditorSceneManager.SaveScene(scene)
                : EditorSceneManager.SaveScene(scene, MainMenuScenePath);

            if (!saved)
                saved = EditorSceneManager.SaveScene(scene, MainMenuScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!EditorApplication.isPlaying &&
                !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(MainMenuScenePath)))
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var active = EditorSceneManager.GetActiveScene();
            Debug.Log($"[FleeNightStudy] 主菜单保存结果={saved}，当前场景={active.name}，路径={active.path}");
            return saved;
        }

        static void TryApplyMainMenuFonts(GameObject menuRoot)
        {
            if (menuRoot == null)
                return;

            try
            {
                ChineseTmpFontSetup.ApplyMainMenuFontsSilent(menuRoot);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FleeNightStudy] 主菜单字体修复已跳过：{ex.Message}");
            }
        }

        static GameObject BuildMainMenuHierarchy()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            MainMenuCamera.EnsureExists();

            var canvasGo = new GameObject("MainMenuCanvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.GetComponent<RectTransform>().localScale = Vector3.one;

            var root = new GameObject("MainMenuUI");
            root.transform.SetParent(canvasGo.transform, false);
            StretchFull(root.AddComponent<RectTransform>());
            var menu = root.AddComponent<MainMenuUI>();

            var bgImage = CreateClassroomBackground(root.transform);
            CreateVignetteOverlay(root.transform);
            var menuStack = CreateMenuStack(root.transform, out _, out _, out _);
            var leaderboardPanel = CreateLeaderboardPanel(root.transform);
            var difficultyPanel = CreateDifficultySelectPanel(root.transform);
            var instructionsPanel = CreateInstructionsPanel(root.transform, out _);

            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("gameplaySceneName").stringValue = GameStateManager.DefaultGameplaySceneName;
            menuSo.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            menuSo.FindProperty("menuStack").objectReferenceValue = menuStack;
            menuSo.FindProperty("leaderboardPanel").objectReferenceValue = leaderboardPanel;
            menuSo.FindProperty("difficultySelectPanel").objectReferenceValue = difficultyPanel;
            menuSo.FindProperty("instructionsPanel").objectReferenceValue = instructionsPanel;
            menuSo.FindProperty("playerNameInput").objectReferenceValue =
                menuStack.transform.Find("PlayerNameInput")?.GetComponent<TMP_InputField>();
            menuSo.FindProperty("nameHintText").objectReferenceValue =
                menuStack.transform.Find("NameHint")?.GetComponent<TMP_Text>();
            menuSo.FindProperty("leaderboardText").objectReferenceValue =
                LeaderboardScrollHelper.EnsureScrollBody(leaderboardPanel.transform.Find("Box"), out _);
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        public static void EnsureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
        }

        static Image CreateClassroomBackground(Transform parent)
        {
            var bgGo = new GameObject("BackgroundImage");
            bgGo.transform.SetParent(parent, false);
            bgGo.transform.SetAsFirstSibling();
            StretchFull(bgGo.AddComponent<RectTransform>());
            var img = bgGo.AddComponent<Image>();
            img.raycastTarget = false;
            ConfigureBackgroundImporter(BackgroundPath);
            var sprite = LoadSprite(BackgroundPath);
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
            else img.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            return img;
        }

        static void CreateVignetteOverlay(Transform parent)
        {
            var dimGo = new GameObject("VignetteOverlay");
            dimGo.transform.SetParent(parent, false);
            StretchFull(dimGo.AddComponent<RectTransform>());
            var vignette = dimGo.AddComponent<Image>();
            vignette.color = new Color(0f, 0f, 0f, 0.22f);
            vignette.raycastTarget = false;
        }

        static GameObject CreateMenuStack(Transform parent, out Button startBtn, out Button instructionsBtn, out Button quitBtn)
        {
            var stackGo = new GameObject("MenuStack");
            stackGo.transform.SetParent(parent, false);
            var stackRect = stackGo.AddComponent<RectTransform>();
            stackRect.anchorMin = stackRect.anchorMax = new Vector2(0.5f, 0.5f);
            stackRect.pivot = new Vector2(0.5f, 0.5f);
            stackRect.anchoredPosition = new Vector2(0f, -20f);
            stackRect.sizeDelta = new Vector2(520f, 480f);

            var layout = stackGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            CreateTitleText(stackGo.transform, "《逃离晚自习》", 68f, TitleGreen, 0.34f);

            var nameGo = new GameObject("PlayerNameInput");
            nameGo.transform.SetParent(stackGo.transform, false);
            nameGo.AddComponent<LayoutElement>().preferredHeight = 44f;
            nameGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            var nameInput = nameGo.AddComponent<TMP_InputField>();
            var nameTextGo = new GameObject("Text");
            nameTextGo.transform.SetParent(nameGo.transform, false);
            StretchFull(nameTextGo.AddComponent<RectTransform>());
            var nameTmp = nameTextGo.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 22f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = MenuButtonText;
            GameUiBuilder.ApplyMenuItemStyle(nameTmp, MenuButtonText, 22f);
            nameInput.textComponent = nameTmp;
            nameInput.text = "";

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(nameGo.transform, false);
            StretchFull(placeholderGo.AddComponent<RectTransform>());
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "请输入玩家名字";
            placeholderTmp.fontSize = 20f;
            placeholderTmp.fontStyle = FontStyles.Italic;
            placeholderTmp.alignment = TextAlignmentOptions.Center;
            placeholderTmp.color = new Color(1f, 1f, 1f, 0.45f);
            GameUiBuilder.ApplyMenuItemStyle(placeholderTmp, placeholderTmp.color, 20f);
            nameInput.placeholder = placeholderTmp;

            var hintGo = new GameObject("NameHint");
            hintGo.transform.SetParent(stackGo.transform, false);
            hintGo.AddComponent<LayoutElement>().preferredHeight = 28f;
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.fontSize = 16f;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(1f, 0.45f, 0.35f, 1f);
            hintTmp.text = "";

            startBtn = GameUiBuilder.CreateCenterTextButton(stackGo.transform, "开始游戏", MenuButtonText, 54f, 48f);
            GameUiBuilder.CreateCenterTextButton(stackGo.transform, "排行榜", MenuButtonText, 44f, 38f);
            instructionsBtn = GameUiBuilder.CreateCenterTextButton(stackGo.transform, "操作说明", MenuButtonText, 44f, 38f);
            quitBtn = GameUiBuilder.CreateCenterTextButton(stackGo.transform, "退出", MenuButtonText, 44f, 38f);
            return stackGo;
        }

        static GameObject CreateLeaderboardPanel(Transform parent)
        {
            var panel = new GameObject("LeaderboardPanel");
            panel.transform.SetParent(parent, false);
            StretchFull(panel.AddComponent<RectTransform>());
            panel.SetActive(false);

            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(panel.transform, false);
            StretchFull(blocker.AddComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var box = new GameObject("Box");
            box.transform.SetParent(panel.transform, false);
            var boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(620f, 820f);
            box.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);

            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 28, 32);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            CreateModalTitleText(box.transform, "排行榜", 36f);

            var scrollBody = LeaderboardScrollHelper.EnsureScrollBody(box.transform, out _);
            if (scrollBody != null)
            {
                scrollBody.text = "请选择要查看的难度";
                GameUiBuilder.ApplyModalBodyStyle(scrollBody, 20f);
            }

            GameUiBuilder.CreateModalButton(box.transform, "简单难度", 32f, 44f);
            GameUiBuilder.CreateModalButton(box.transform, "普通难度", 32f, 44f);
            GameUiBuilder.CreateModalButton(box.transform, "返回", 30f, 42f);

            box.transform.SetAsLastSibling();
            return panel;
        }

        static GameObject CreateDifficultySelectPanel(Transform parent)
        {
            var panel = new GameObject("DifficultySelectPanel");
            panel.transform.SetParent(parent, false);
            StretchFull(panel.AddComponent<RectTransform>());
            panel.SetActive(false);

            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(panel.transform, false);
            StretchFull(blocker.AddComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var box = new GameObject("Box");
            box.transform.SetParent(panel.transform, false);
            var boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(500f, 340f);
            box.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);

            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 36, 36);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            GameUiBuilder.CreateModalButton(box.transform, "简单难度", 36f, 52f);
            GameUiBuilder.CreateModalButton(box.transform, "普通难度", 36f, 52f);
            GameUiBuilder.CreateModalButton(box.transform, "返回", 32f, 46f);

            box.transform.SetAsLastSibling();
            return panel;
        }

        static GameObject CreateInstructionsPanel(Transform parent, out Button backBtn)
        {
            var panel = new GameObject("InstructionsPanel");
            panel.transform.SetParent(parent, false);
            StretchFull(panel.AddComponent<RectTransform>());
            panel.SetActive(false);

            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(panel.transform, false);
            StretchFull(blocker.AddComponent<RectTransform>());
            blocker.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var box = new GameObject("Box");
            box.transform.SetParent(panel.transform, false);
            var boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(760f, 640f);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(1f, 1f, 1f, 0.96f);
            boxImg.raycastTarget = true;

            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 32, 40);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            CreateModalTitleText(box.transform, "操作说明", 40f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(box.transform, false);
            bodyGo.AddComponent<LayoutElement>().preferredHeight = 360f;
            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.text = GameUiCopy.InstructionsBody;
            body.alignment = TextAlignmentOptions.TopLeft;
            GameUiBuilder.ApplyModalBodyStyle(body, 24f);

            var spacerGo = new GameObject("BottomSpacer");
            spacerGo.transform.SetParent(box.transform, false);
            spacerGo.AddComponent<LayoutElement>().preferredHeight = 8f;

            backBtn = GameUiBuilder.CreateModalButton(box.transform, "返回", 34f, 48f);
            backBtn.gameObject.name = "InstructionsBackButton";

            box.transform.SetAsLastSibling();
            return panel;
        }

        static void CreateModalTitleText(Transform parent, string text, float fontSize)
        {
            var go = new GameObject("TitleText");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            GameUiBuilder.ApplyTitleStyle(tmp, ModalTitleText, fontSize, 0.08f);
        }

        static void CreateTitleText(Transform parent, string text, float fontSize, Color color, float outlineWidth)
        {
            var go = new GameObject("TitleText");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 110f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            GameUiBuilder.ApplyTitleStyle(tmp, color, fontSize, outlineWidth);
        }

        static void MirrorBackgroundToResources()
        {
            var src = ToAbsoluteAssetPath(BackgroundPath);
            var dst = ToAbsoluteAssetPath(ResourcesBackgroundPath);
            if (!File.Exists(src))
                return;

            EnsureFolder("Assets/FleeNightStudy/Resources/FleeNightStudy");
            File.Copy(src, dst, true);
            ConfigureBackgroundImporter(ResourcesBackgroundPath);
        }

        static string ToAbsoluteAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return assetPath;
            return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        }

        static void ConfigureBackgroundImporter(string assetPath)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                return;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite sp) return sp;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
