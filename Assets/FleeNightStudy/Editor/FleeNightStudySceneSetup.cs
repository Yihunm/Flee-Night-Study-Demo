#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using FleeNightStudy;

namespace FleeNightStudy.EditorTools
{
    /// <summary>
    /// 一键在当前场景补全《逃离晚自习》玩法物体与组件绑定（MergeV2 等）。
    /// 菜单：FleeNightStudy → 设置当前场景玩法
    /// </summary>
    public static class FleeNightStudySceneSetup
    {
        const string GameplayRootName = "FleeNightStudy_Gameplay";
        const int DefaultTextbookCount = 8;

        [MenuItem("FleeNightStudy/修复场景 UI Canvas")]
        public static void FixSceneUiCanvasMenu()
        {
            FixAllOverlayCanvases();
            ConsolidateDuplicateGameUiCanvases();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "已将 GameUI_Canvas 移到场景根并修复缩放。\n请保存场景 (Ctrl+S)。",
                "确定");
        }

        [MenuItem("FleeNightStudy/一键设置当前场景")]
        public static void SetupCurrentScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name.Contains("MainMenu"))
            {
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    "「一键设置当前场景」只用于玩法场景（如 MergeV2），不要对主菜单场景使用。\n\n" +
                    "主菜单请用：FleeNightStudy → 重建主菜单",
                    "确定");
                return;
            }

            EnsureTags();
            bool alreadyConfigured = IsSceneAlreadyConfigured();

            if (alreadyConfigured)
                ApplyLightweightUpgrade();
            else
                ApplyFullSetup();

            FixAllOverlayCanvases();
            ConsolidateDuplicateGameUiCanvases();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                alreadyConfigured
                    ? "当前场景已做过玩法设置，本次仅补全 Managers 组件并修复 UI Canvas。\n请保存场景 (Ctrl+S) 后 Play。"
                    : "场景玩法首次设置完成。\n请保存场景 (Ctrl+S) 后 Play。",
                "确定");

            MainMenuSceneSetup.EnsureBuildSettings();
        }

        static bool IsSceneAlreadyConfigured()
        {
            return GameObject.Find(GameplayRootName) != null
                   && GameObject.Find("Managers") != null
                   && GameObject.FindGameObjectWithTag("Player") != null;
        }

        static void ApplyLightweightUpgrade()
        {
            var root = GameObject.Find(GameplayRootName);
            var managers = EnsureManagers(root != null ? root.transform : null, reparent: false);
            EnsureTeacherSpawnManager(root != null ? root.transform : null);
            EnsurePlayer(lightweight: true);
            EnsureGameResultController();
            if (!HasValidGameResultUi() && !HasLegacyGameResultUi())
                RepairGameResultUi(null);
            else
                TryBindAllResultAssets();
            EnsureGameplayHintsHud(null);
            EnsureSchoolExitVictoryGate();
            FixTextbookDoorPassage(silent: true);
            ApplyTeacherPatrolRoutes();
            Debug.Log("[FleeNightStudy] 轻量升级：已补全 Managers / UI，未重建课本与老师。");
        }

        static void ApplyFullSetup()
        {
            var root = GetOrCreateGameplayRoot();

            var managers = EnsureManagers(root.transform, reparent: true);
            var player = EnsurePlayer(lightweight: false);
            EnsureDemoBootstrap();
            DisableSceneFlyCameras();

            int required = GetTextbooksRequired(managers);
            EnsureTextbooks(root.transform, player != null ? player.transform.position : Vector3.zero, required);
            EnsureExitDoor(root.transform, player != null ? player.transform.position : Vector3.zero);
            EnsureGameOverUi(null);
            EnsureGameplayHintsHud(null);
            EnsureSchoolExitVictoryGate();
            EnsureTeacherSpawnManager(root.transform);
            EnsureTeacherCharacterVisuals();
            EnsurePatrolTeachers(root.transform, player != null ? player.transform : null);

            EditorUtility.SetDirty(root);
            if (managers != null) EditorUtility.SetDirty(managers);
            if (player != null) EditorUtility.SetDirty(player.gameObject);

            Debug.Log("[FleeNightStudy] 首次完整设置完成（未自动烘焙 NavMesh，请手动 Window → AI → Navigation → Bake）。");
        }

        /// <summary>Screen Space Overlay Canvas 必须在场景根且 scale=1，否则 UI 不可见。</summary>
        static void FixAllOverlayCanvases()
        {
            foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                var tr = canvas.transform;
                if (tr.parent != null)
                {
                    Undo.SetTransformParent(tr, null, "Fix overlay canvas parent");
                    Debug.Log($"[FleeNightStudy] 已将 {canvas.name} 移到场景根（修复 UI 缩放）。");
                }

                if (tr.localScale != Vector3.one)
                {
                    Undo.RecordObject(tr, "Fix overlay canvas scale");
                    tr.localScale = Vector3.one;
                }

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    Undo.RecordObject(scaler, "Fix canvas scaler");
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                EditorUtility.SetDirty(canvas.gameObject);
            }
        }

        static void ConsolidateDuplicateGameUiCanvases()
        {
            var canvases = Object.FindObjectsOfType<Canvas>(true)
                .Where(c => c != null && c.name == "GameUI_Canvas")
                .OrderByDescending(c => c.GetComponentsInChildren<GameplayHintsHUD>(true).Length)
                .ThenByDescending(c => c.transform.childCount)
                .ToList();

            if (canvases.Count <= 1) return;

            var primary = canvases[0].gameObject;
            for (int i = 1; i < canvases.Count; i++)
            {
                var dup = canvases[i];
                MoveCanvasChildrenToPrimary(dup.transform, primary.transform);
                Undo.DestroyObjectImmediate(dup.gameObject);
                Debug.Log("[FleeNightStudy] 已合并重复的 GameUI_Canvas。");
            }
        }

        static void MoveCanvasChildrenToPrimary(Transform from, Transform to)
        {
            while (from.childCount > 0)
            {
                var child = from.GetChild(0);
                Undo.SetTransformParent(child, to, "Merge UI canvas children");
            }
        }

        static GameObject GetPrimaryGameUiCanvas(Transform fallbackParent)
        {
            FixAllOverlayCanvases();
            ConsolidateDuplicateGameUiCanvases();

            var existing = Object.FindObjectsOfType<Canvas>(true)
                .FirstOrDefault(c => c != null && c.name == "GameUI_Canvas");
            if (existing != null) return existing.gameObject;

            return CreateOverlayCanvas(fallbackParent);
        }

        static GameObject GetOrCreateGameplayRoot()
        {
            var existing = GameObject.Find(GameplayRootName);
            if (existing != null) return existing;

            var go = new GameObject(GameplayRootName);
            Undo.RegisterCreatedObjectUndo(go, "Create FleeNightStudy Gameplay Root");
            return go;
        }

        static void EnsureTags()
        {
            // Unity 内置 Player；Teacher 需在 TagManager 中存在
            if (!TagExists("Teacher"))
                Debug.LogWarning("[FleeNightStudy] 请在 Project Settings → Tags 中添加 Teacher，或运行后手动给老师设 Tag。");
        }

        static bool TagExists(string tag)
        {
            try
            {
                var obj = GameObject.FindWithTag(tag);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static GameObject EnsureManagers(Transform parent, bool reparent)
        {
            var go = GameObject.Find("Managers") ?? new GameObject("Managers");
            if (reparent && go.transform.parent == null && parent != null)
                go.transform.SetParent(parent, false);

            Undo.RecordObject(go, "Setup Managers");

            RemoveMisplacedComponent<TextbookPickup>(go);

            if (go.GetComponent<GameStateManager>() == null)
                Undo.AddComponent<GameStateManager>(go);

            if (go.GetComponent<GameFlowDisableSimpleFp>() == null)
                Undo.AddComponent<GameFlowDisableSimpleFp>(go);

            if (go.GetComponent<GameplayBootstrap>() == null)
                Undo.AddComponent<GameplayBootstrap>(go);

            var gsm = go.GetComponent<GameStateManager>();
            if (gsm != null)
            {
                var so = new SerializedObject(gsm);
                if (so.FindProperty("textbooksRequired").intValue < 1)
                    so.FindProperty("textbooksRequired").intValue = DefaultTextbookCount;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureGameResultController();

            return go;
        }

        static void RemoveMisplacedComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null)
            {
                Undo.DestroyObjectImmediate(c);
                Debug.Log($"[FleeNightStudy] 已从 Managers 移除误挂的 {typeof(T).Name}。");
            }
        }

        static int GetTextbooksRequired(GameObject managers)
        {
            if (managers == null) return DefaultTextbookCount;
            var gsm = managers.GetComponent<GameStateManager>();
            if (gsm == null) return DefaultTextbookCount;
            var so = new SerializedObject(gsm);
            return so.FindProperty("textbooksRequired").intValue;
        }

        static GameObject EnsurePlayer(bool lightweight)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                Undo.RegisterCreatedObjectUndo(player, "Create Player");
                player.transform.position = new Vector3(2f, 2f, -5f);
                lightweight = false;
            }

            Undo.RecordObject(player, "Setup Player");
            player.tag = "Player";

            if (!lightweight)
            {
                var mr = player.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            if (player.GetComponent<CharacterController>() == null)
                Undo.AddComponent<CharacterController>(player);

            if (!lightweight)
            {
                var cc = player.GetComponent<CharacterController>();
                cc.height = 1.6f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, 0.9f, 0f);
            }

            var camTr = player.transform.Find("PlayerCamera");
            Camera cam;
            if (camTr == null)
            {
                var camGo = new GameObject("PlayerCamera");
                Undo.RegisterCreatedObjectUndo(camGo, "Create PlayerCamera");
                camGo.transform.SetParent(player.transform, false);
                camGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                cam = Undo.AddComponent<Camera>(camGo);
                Undo.AddComponent<AudioListener>(camGo);
            }
            else
            {
                cam = camTr.GetComponent<Camera>();
                if (cam == null) cam = Undo.AddComponent<Camera>(camTr.gameObject);
                if (camTr.GetComponent<AudioListener>() == null)
                    Undo.AddComponent<AudioListener>(camTr.gameObject);
            }

            DisableOtherAudioListeners(cam != null ? cam.GetComponent<AudioListener>() : null);

            if (player.GetComponent<FirstPersonWalker>() == null)
                Undo.AddComponent<FirstPersonWalker>(player);

            var walker = player.GetComponent<FirstPersonWalker>();
            WireWalkerCamera(walker, player.transform.Find("PlayerCamera"));

            if (player.GetComponent<DoorInteractor>() == null)
                Undo.AddComponent<DoorInteractor>(player);

            var interactor = player.GetComponent<DoorInteractor>();
            WireDoorInteractor(interactor, player.transform.Find("PlayerCamera"));

            if (player.GetComponent<PlayerCaught>() == null)
                Undo.AddComponent<PlayerCaught>(player);

            return player;
        }

        static void WireWalkerCamera(FirstPersonWalker walker, Transform camTr)
        {
            if (walker == null || camTr == null) return;
            var so = new SerializedObject(walker);
            so.FindProperty("cameraTransform").objectReferenceValue = camTr;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireDoorInteractor(DoorInteractor interactor, Transform camTr)
        {
            if (interactor == null || camTr == null) return;
            var so = new SerializedObject(interactor);
            so.FindProperty("rayOrigin").objectReferenceValue = camTr;
            so.ApplyModifiedPropertiesWithoutUndo();

            var oldHint = GameObject.Find("DoorHintPanel");
            if (oldHint != null && oldHint.activeSelf)
            {
                oldHint.SetActive(false);
                Debug.Log("[FleeNightStudy] 已禁用旧 DoorHintPanel，改用左下角 GameplayHintsPanel。");
            }
        }

        static void DisableOtherAudioListeners(AudioListener keep)
        {
            foreach (var al in Object.FindObjectsOfType<AudioListener>(true))
            {
                if (keep != null && al == keep) continue;
                al.enabled = false;
            }
        }

        static void EnsureDemoBootstrap()
        {
            var go = GameObject.Find("DemoBootstrap");
            if (go == null)
            {
                go = new GameObject("DemoBootstrap");
                Undo.RegisterCreatedObjectUndo(go, "Create DemoBootstrap");
            }

            if (go.GetComponent<SchoolFlyCameraDisabler>() == null)
                Undo.AddComponent<SchoolFlyCameraDisabler>(go);
            if (go.GetComponent<NightLightingApplier>() == null)
                Undo.AddComponent<NightLightingApplier>(go);
        }

        static void DisableSceneFlyCameras()
        {
            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (mb.GetType().Name == "SimpleCameraController")
                    mb.enabled = false;
            }

            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
            {
                var listener = mainCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }

        static void EnsureTextbooks(Transform parent, Vector3 nearPlayer, int count)
        {
            var folder = GameObject.Find("Textbooks");
            if (folder == null)
            {
                folder = new GameObject("Textbooks");
                Undo.RegisterCreatedObjectUndo(folder, "Create Textbooks folder");
                folder.transform.SetParent(parent, false);
            }

            var existing = folder.GetComponentsInChildren<TextbookPickup>(true).Length;
            var need = Mathf.Max(0, count - existing);
            var start = existing;

            for (int i = 0; i < need; i++)
            {
                int idx = start + i + 1;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Textbook_{idx:D2}";
                Undo.RegisterCreatedObjectUndo(go, "Create Textbook");
                go.transform.SetParent(folder.transform, false);
                go.transform.localScale = new Vector3(0.25f, 0.05f, 0.35f);
                go.transform.position = nearPlayer + new Vector3((idx % 4) * 1.2f - 1.8f, 1f, 2f + (idx / 4) * 1.2f);

                var col = go.GetComponent<BoxCollider>();
                col.isTrigger = true;

                if (go.GetComponent<TextbookPickup>() == null)
                    Undo.AddComponent<TextbookPickup>(go);
            }

            Debug.Log($"[FleeNightStudy] 课本：已有 {existing}，新建 {need}，目标 {count}。");
        }

        static void EnsureExitDoor(Transform parent, Vector3 nearPlayer)
        {
            if (Object.FindObjectOfType<ExitDoor>() != null)
            {
                Debug.Log("[FleeNightStudy] 场景中已有 ExitDoor，跳过创建。");
                return;
            }

            var exitPos = nearPlayer + new Vector3(0f, 0f, 8f);

            var winZone = new GameObject("ExitDoor_WinZone");
            Undo.RegisterCreatedObjectUndo(winZone, "Create ExitDoor");
            winZone.transform.SetParent(parent, false);
            winZone.transform.position = exitPos;

            var trigger = Undo.AddComponent<BoxCollider>(winZone);
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 2.5f, 2f);
            trigger.center = new Vector3(0f, 1.25f, 0f);

            var exitDoor = Undo.AddComponent<ExitDoor>(winZone);

            var blocking = new GameObject("BlockingVolume");
            Undo.RegisterCreatedObjectUndo(blocking, "Create BlockingVolume");
            blocking.transform.SetParent(winZone.transform, false);
            blocking.transform.localPosition = new Vector3(0f, 1.25f, -1.2f);

            var blockCol = Undo.AddComponent<BoxCollider>(blocking);
            blockCol.isTrigger = false;
            blockCol.size = new Vector3(3f, 2.5f, 0.4f);

            var so = new SerializedObject(exitDoor);
            so.FindProperty("blockingCollider").objectReferenceValue = blockCol;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[FleeNightStudy] 已在 {exitPos} 创建 ExitDoor（请按关卡把出口挪到正确门洞）。");
        }

        static GameplayHintsHUD CreateGameplayHintsPanelUnderCanvas(Transform canvasParent)
        {
            var panelGo = new GameObject("GameplayHintsPanel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGo, "Create GameplayHintsPanel");
            panelGo.transform.SetParent(canvasParent, false);

            var rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(560f, 140f);

            return Undo.AddComponent<GameplayHintsHUD>(panelGo);
        }

        static void EnsureGameplayHintsHud(Transform parent)
        {
            var hud = Object.FindObjectOfType<GameplayHintsHUD>(true);
            var canvasGo = GetPrimaryGameUiCanvas(parent);

            if (hud == null)
            {
                hud = CreateGameplayHintsPanelUnderCanvas(canvasGo.transform);
            }
            else
            {
                if (hud.transform.parent != canvasGo.transform)
                    hud.transform.SetParent(canvasGo.transform, false);

                if (UiRectUtil.GetRectTransform(hud.gameObject) == null)
                {
                    Undo.DestroyObjectImmediate(hud.gameObject);
                    hud = CreateGameplayHintsPanelUnderCanvas(canvasGo.transform);
                }
            }

            hud.EnsurePanelStructure();

            if (!Application.isPlaying)
                hud.RebuildHintLines();

            var oldHint = GameObject.Find("DoorHintPanel");
            if (oldHint != null)
                oldHint.SetActive(false);

            EditorUtility.SetDirty(hud);
            Debug.Log("[FleeNightStudy] 已升级 GameplayHintsPanel（HintsContent + 三行提示）。");
        }

        static void UpgradeLegacyGameplayHintsHud(GameplayHintsHUD hud)
        {
            hud.EnsurePanelStructure();
            if (Application.isPlaying)
                hud.RefreshAll();
        }

        static TMP_Text CreateBottomLeftHintLine(Transform parent, string name, string text, float bottomOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, bottomOffset);
            rect.sizeDelta = new Vector2(520f, 30f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static GameObject CreateOverlayCanvas(Transform unusedParent)
        {
            var canvasGo = new GameObject("GameUI_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Game UI Canvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");

            var rect = canvasGo.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvasGo;
        }

        static void EnsureGameOverUi(Transform parent)
        {
            if (!HasValidGameResultUi())
            {
                RepairGameResultUi(parent);
                EnsureGameplayHintsHud(parent);
                return;
            }

            EnsureGameResultController();
            TryBindAllResultAssets();
            EnsureGameplayHintsHud(parent);
            Debug.Log("[FleeNightStudy] 胜负 UI 已就绪（VictoryPanel + GameResultController）。");
        }

        static bool HasValidGameResultUi()
        {
            var victory = FindVictoryPanelTransform();
            return victory != null
                   && victory.Find("StatsText") != null
                   && victory.Find("BottomActionBar") != null
                   && Object.FindObjectOfType<GameResultController>() != null;
        }

        static Transform FindVictoryPanelTransform()
        {
            foreach (var ui in Object.FindObjectsOfType<GameOverUI>(true))
            {
                var tr = ui.transform.Find("VictoryPanel");
                if (tr != null) return tr;
            }

            var root = GameObject.Find("GameResultUI");
            return root != null ? root.transform.Find("VictoryPanel") : null;
        }

        static bool HasLegacyGameResultUi()
        {
            foreach (var ui in Object.FindObjectsOfType<GameOverUI>(true))
            {
                if (ui == null) continue;
                var so = new SerializedObject(ui);
                if (so.FindProperty("panelRoot").objectReferenceValue != null)
                    return true;
            }

            return false;
        }

        static void RemoveBrokenGameResultUi()
        {
            foreach (var oldUi in Object.FindObjectsOfType<GameOverUI>(true))
            {
                if (oldUi == null) continue;

                var uiSo = new SerializedObject(oldUi);
                bool hasPanels = uiSo.FindProperty("victoryPanel").objectReferenceValue != null
                                 && uiSo.FindProperty("defeatPanel").objectReferenceValue != null;
                bool hasLegacy = uiSo.FindProperty("panelRoot").objectReferenceValue != null;
                if (hasPanels || hasLegacy)
                    continue;

                if (oldUi.gameObject.name == "GameOverPanel")
                    Undo.DestroyObjectImmediate(oldUi.gameObject);
            }
        }

        static void EnsureGameResultUiActive()
        {
            foreach (var ui in Object.FindObjectsOfType<GameOverUI>(true))
            {
                if (ui == null) continue;
                if (!ui.gameObject.activeSelf)
                    Undo.RecordObject(ui.gameObject, "Activate GameResultUI");
                ui.gameObject.SetActive(true);
            }
        }

        public static void RepairGameResultUi(Transform parent = null)
        {
            RemoveBrokenGameResultUi();

            var existingUi = Object.FindObjectOfType<GameOverUI>(true);
            if (existingUi != null && FindVictoryPanelTransform() == null)
                Undo.DestroyObjectImmediate(existingUi.gameObject);

            if (FindVictoryPanelTransform() == null)
                CreateGameResultUiStructure(parent);

            EnsureGameResultUiActive();
            var ui = Object.FindObjectOfType<GameOverUI>(true);
            if (ui != null)
            {
                GameResultUiBootstrap.NormalizeHierarchy(ui);
                GameResultUiBootstrap.RemoveRedundantCenterWidgets(ui);
            }
            EnsureGameResultController();
            TryBindAllResultAssets();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        static void EnsureGameResultController()
        {
            var managers = GameObject.Find("Managers");
            if (managers == null)
            {
                Debug.LogWarning("[FleeNightStudy] 未找到 Managers，无法挂 GameResultController。");
                return;
            }

            var ctrl = managers.GetComponent<GameResultController>();
            if (ctrl == null)
                ctrl = Undo.AddComponent<GameResultController>(managers);

            var ui = Object.FindObjectOfType<GameOverUI>(true);
            var audio = Object.FindObjectOfType<GameResultAudio>(true);
            var so = new SerializedObject(ctrl);
            so.FindProperty("gameOverUi").objectReferenceValue = ui;
            so.FindProperty("gameResultAudio").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(managers);
        }

        static void TryBindAllResultAssets()
        {
            var ui = Object.FindObjectOfType<GameOverUI>(true);
            if (ui == null) return;
            var audio = ui.GetComponent<GameResultAudio>();
            if (audio == null)
                audio = ui.gameObject.AddComponent<GameResultAudio>();
            GameResultAssetsSetup.CreateAndBindResultAssetsSilent(ui, audio);
        }

        static void CreateGameResultUiStructure(Transform parent)
        {
            if (FindVictoryPanelTransform() != null)
                return;

            var canvasGo = GetPrimaryGameUiCanvas(parent);

            var controller = new GameObject("GameResultUI", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(controller, "Create GameResultUI");
            controller.transform.SetParent(canvasGo.transform, false);
            var rootRect = controller.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var ui = Undo.AddComponent<GameOverUI>(controller);

            var victoryPanel = CreateResultPanel(
                controller.transform,
                "VictoryPanel",
                "胜利：逃离晚自习！",
                out TMP_Text victoryTmp,
                out Image victoryBg,
                out Button victoryExitBtn);

            var defeatPanel = CreateResultPanel(
                controller.transform,
                "DefeatPanel",
                "失败：被老师抓住。",
                out TMP_Text defeatTmp,
                out Image defeatBg,
                out Button defeatExitBtn);

            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);

            WireResultPanelButtons(ui, victoryPanel, victoryExitBtn);
            WireResultPanelButtons(ui, defeatPanel, defeatExitBtn);

            Undo.AddComponent<GameResultAudio>(controller);

            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            uiSo.FindProperty("defeatPanel").objectReferenceValue = defeatPanel;
            uiSo.FindProperty("victoryTextMeshPro").objectReferenceValue = victoryTmp;
            uiSo.FindProperty("defeatTextMeshPro").objectReferenceValue = defeatTmp;
            uiSo.FindProperty("victoryBackgroundImage").objectReferenceValue = victoryBg;
            uiSo.FindProperty("defeatBackgroundImage").objectReferenceValue = defeatBg;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            GameResultUiBootstrap.NormalizeHierarchy(ui);
            Debug.Log("[FleeNightStudy] 已创建 GameResultUI + VictoryPanel + DefeatPanel。");
        }

        static void WireResultPanelButtons(GameOverUI ui, GameObject panel, Button mainMenuButton)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                mainMenuButton.onClick, ui.OnClickExitToMainMenu);

            var bottomRestart = panel.transform.Find("BottomActionBar/BottomRestartButton")
                ?.GetComponent<Button>();
            if (bottomRestart != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    bottomRestart.onClick, ui.OnClickRestart);
            }
        }

        static GameObject CreateResultPanel(
            Transform parent,
            string panelName,
            string title,
            out TMP_Text titleTmp,
            out Image backgroundImage,
            out Button exitButton)
        {
            var panel = new GameObject(panelName);
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.AddComponent<CanvasGroup>();

            var bgGo = new GameObject("BackgroundImage");
            bgGo.transform.SetParent(panel.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            backgroundImage = bgGo.AddComponent<Image>();
            backgroundImage.color = panelName.StartsWith("Victory")
                ? new Color(0.05f, 0.22f, 0.12f, 1f)
                : new Color(0.22f, 0.05f, 0.05f, 1f);

            var dimGo = new GameObject("TextDimOverlay");
            dimGo.transform.SetParent(panel.transform, false);
            var dimRect = dimGo.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.42f);

            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.78f);
            titleRect.anchorMax = new Vector2(0.5f, 0.78f);
            titleRect.sizeDelta = new Vector2(680f, 100f);
            titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            titleTmp.fontSize = 40f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            var statsGo = new GameObject("StatsText");
            statsGo.transform.SetParent(panel.transform, false);
            var statsRect = statsGo.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.58f);
            statsRect.anchorMax = new Vector2(0.5f, 0.58f);
            statsRect.sizeDelta = new Vector2(680f, 200f);
            var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
            statsTmp.text = "";
            statsTmp.fontSize = 28f;
            statsTmp.alignment = TextAlignmentOptions.Center;
            statsTmp.color = Color.white;

            var bottomGo = new GameObject("BottomActionBar");
            bottomGo.transform.SetParent(panel.transform, false);
            var bottomRect = bottomGo.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.sizeDelta = new Vector2(0f, 120f);
            bottomGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            var bottomLayout = bottomGo.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.spacing = 28f;
            bottomLayout.padding = new RectOffset(40, 40, 18, 18);

            GameUiBuilder.CreateBarButton(bottomGo.transform, "重新开始",
                new Color(0.25f, 0.75f, 0.45f, 1f), 260f).gameObject.name = "BottomRestartButton";
            exitButton = GameUiBuilder.CreateBarButton(bottomGo.transform, "返回主菜单",
                new Color(0.38f, 0.38f, 0.44f, 1f), 260f);
            exitButton.gameObject.name = "MainMenuButton";

            return panel;
        }

        static void EnsureTeacherSpawnManager(Transform parent)
        {
            var managers = GameObject.Find("Managers");
            if (managers == null) return;
            if (managers.GetComponent<TeacherSpawnManager>() == null)
                Undo.AddComponent<TeacherSpawnManager>(managers);
        }

        [MenuItem("FleeNightStudy/应用老师巡逻路径")]
        public static void ApplyTeacherPatrolRoutesMenu()
        {
            ApplyTeacherPatrolRoutes();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "已为场景中的巡查老师应用三层巡逻路径（各 2 个端点）。\n请保存场景 (Ctrl+S) 并确保已烘焙 NavMesh。",
                "确定");
        }

        static void ApplyTeacherPatrolRoutes()
        {
            TeacherPatrolSetup.ApplyAllPatrolRoutes();
            Debug.Log("[FleeNightStudy] 已应用老师巡逻路径（三层楼各一条走廊线段）。");
        }

        static void EnsureTeacherCharacterVisuals()
        {
            const string patrolRes = "Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/PatrolTeacherVisual.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(patrolRes) == null)
                TeacherCharacterPrefabSetup.CreateAnimeStyleTeacherVisuals();
        }

        static void EnsurePatrolTeachers(Transform parent, Transform player)
        {
            var patrolRoot = GameObject.Find("PatrolPoints");
            if (patrolRoot == null)
            {
                patrolRoot = new GameObject("PatrolPoints");
                Undo.RegisterCreatedObjectUndo(patrolRoot, "Create PatrolPoints");
                patrolRoot.transform.SetParent(parent, false);
            }

            var existing = Object.FindObjectsOfType<TeacherController>(true)
                .Where(t => t != null && !t.IsHeadTeacher)
                .OrderBy(t => t.name)
                .ToList();

            for (int i = existing.Count; i < 3; i++)
                CreateSceneTeacher(parent, player, i + 1);

            ApplyTeacherPatrolRoutes();

            if (player != null)
            {
                foreach (var tc in Object.FindObjectsOfType<TeacherController>(true))
                {
                    if (tc == null || tc.IsHeadTeacher) continue;
                    var so = new SerializedObject(tc);
                    so.FindProperty("player").objectReferenceValue = player;
                    so.FindProperty("obstacleMask").intValue = ~0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[FleeNightStudy] 已配置 3 名老师及固定巡逻路径。");
        }

        static void CreateSceneTeacher(Transform parent, Transform player, int index)
        {
            int routeIndex = index - 1;
            float floorY = TeacherPatrolConfig.GetRouteDesignFloorY(routeIndex);
            var spawnPos = TeacherPatrolConfig.SnapToNavMeshOnFloor(
                TeacherPatrolConfig.GetRoute(routeIndex)[0], floorY);
            var patrolRoot = GameObject.Find("PatrolPoints")?.transform;

            var controller = TeacherNpcBuilder.Spawn(
                $"Teacher_NPC_{index}",
                false,
                spawnPos,
                parent,
                player,
                index - 1,
                patrolRoot);

            if (controller != null)
                Undo.RegisterCreatedObjectUndo(controller.gameObject, "Create Teacher");
        }

        static void EnsureTeacher(Transform parent, Transform player)
        {
            EnsurePatrolTeachers(parent, player);
        }

        [MenuItem("FleeNightStudy/烘焙 NavMesh（可选）")]
        public static void BakeNavMeshMenu()
        {
            TryBakeNavMesh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("FleeNightStudy", "NavMesh 烘焙已尝试，请查看 Console。", "确定");
        }

        static void TryBakeNavMesh()
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("[FleeNightStudy] 已尝试烘焙 NavMesh。若失败，请手动 Window → AI → Navigation → Bake。");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FleeNightStudy] 自动烘焙 NavMesh 失败：{ex.Message}。请手动烘焙。");
            }
        }

        static readonly Vector3 SchoolGateWorldHint = new Vector3(-3.929069f, -0.2000002f, 44.98773f);
        const float SchoolGateSearchRadius = 12f;

        static void EnsureSchoolExitVictoryGate()
        {
            int configured = 0;
            configured += SetupSchoolExitDoor(FindGateDoorObject("Door_01", SchoolGateWorldHint));
            configured += SetupSchoolExitDoor(FindGateDoorObject("Door_02_Snaps014", SchoolGateWorldHint));

            foreach (var exit in Object.FindObjectsOfType<ExitDoor>(true))
            {
                var so = new SerializedObject(exit);
                so.FindProperty("triggerVictoryOnEnter").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(exit);
            }

            if (configured > 0)
                Debug.Log($"[FleeNightStudy] 已配置 {configured} 扇校门口胜利门（课本集齐后按 E 胜利）。教学楼 ExitDoor 已改为仅解锁出口、不自动胜利。");
            else
                Debug.LogWarning("[FleeNightStudy] 未在坐标附近找到 Door_01 / Door_02_Snaps014，请手动挂 SchoolExitVictoryDoorInteractable。");
        }

        static GameObject FindGateDoorObject(string exactName, Vector3 worldHint)
        {
            GameObject best = null;
            float bestDist = float.MaxValue;
            foreach (var t in Object.FindObjectsOfType<Transform>(true))
            {
                if (t == null || t.name != exactName) continue;
                float d = (t.position - worldHint).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t.gameObject;
                }
            }

            if (best != null && bestDist <= SchoolGateSearchRadius * SchoolGateSearchRadius)
                return best;

            return null;
        }

        static int SetupSchoolExitDoor(GameObject doorGo)
        {
            if (doorGo == null) return 0;

            Undo.RecordObject(doorGo, "Setup school exit door");

            var sliding = doorGo.GetComponent<SlidingDoorInteractable>();
            if (sliding == null)
                sliding = Undo.AddComponent<SlidingDoorInteractable>(doorGo);

            TryAutoBindSlideLeaves(sliding);

            var soSliding = new SerializedObject(sliding);
            soSliding.FindProperty("disableBlockingCollidersWhenOpen").boolValue = true;
            soSliding.ApplyModifiedPropertiesWithoutUndo();

            var victory = doorGo.GetComponent<SchoolExitVictoryDoorInteractable>();
            if (victory == null)
                victory = Undo.AddComponent<SchoolExitVictoryDoorInteractable>(doorGo);

            var soVictory = new SerializedObject(victory);
            soVictory.FindProperty("slidingDoor").objectReferenceValue = sliding;
            soVictory.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(doorGo);
            return 1;
        }

        static void TryAutoBindSlideLeaves(SlidingDoorInteractable sliding)
        {
            if (sliding == null) return;
            var so = new SerializedObject(sliding);
            var leavesProp = so.FindProperty("slideLeaves");
            if (leavesProp.arraySize > 0)
                return;

            var candidates = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in sliding.transform)
            {
                if (child.GetComponent<MeshRenderer>() != null || child.GetComponent<MeshFilter>() != null)
                    candidates.Add(child);
            }

            if (candidates.Count == 0)
            {
                foreach (Transform child in sliding.transform)
                {
                    if (child.name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        candidates.Add(child);
                }
            }

            if (candidates.Count == 0) return;

            leavesProp.arraySize = candidates.Count;
            for (int i = 0; i < candidates.Count; i++)
            {
                var leaf = leavesProp.GetArrayElementAtIndex(i);
                leaf.FindPropertyRelative("target").objectReferenceValue = candidates[i];
                float sign = i == 0 ? -1f : 1f;
                leaf.FindPropertyRelative("slideLocalDelta").vector3Value = new Vector3(sign, 0f, 0f);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("FleeNightStudy/修复特殊门（校门+课本门）")]
        public static void FixSpecialDoorsMenu()
        {
            EnsureSchoolExitVictoryGate();
            FixTextbookDoorPassage(silent: true);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("FleeNightStudy", "已配置校门口胜利门与课本门通行。\n请保存场景后测试。", "确定");
        }

        public static void FixTextbookDoorPassage(bool silent = false)
        {
            var doors = Object.FindObjectsOfType<SlidingDoorInteractable>(true);
            SlidingDoorInteractable target = null;
            foreach (var d in doors)
            {
                if (d != null && d.name.Contains("Door_01_Snaps014"))
                {
                    target = d;
                    break;
                }
            }

            if (target == null)
            {
                if (!silent)
                    EditorUtility.DisplayDialog("FleeNightStudy", "未找到名为 Door_01_Snaps014 的 SlidingDoorInteractable。", "确定");
                return;
            }

            Undo.RecordObject(target, "Fix door passage");
            var so = new SerializedObject(target);
            so.FindProperty("disableBlockingCollidersWhenOpen").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);

            if (target.GetComponent<TextbookGatedDoorInteractable>() == null)
                Undo.AddComponent<TextbookGatedDoorInteractable>(target.gameObject);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log($"[FleeNightStudy] 已修复 {target.name}。");
            if (!silent)
                EditorUtility.DisplayDialog("FleeNightStudy", $"已修复 {target.name}。", "确定");
        }
    }
}
#endif
