using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FleeNightStudy
{
    /// <summary>玩法场景启动：挂载系统、生成 HUD、按难度初始化。</summary>
    public class GameplayBootstrap : MonoBehaviour
    {
        void Awake()
        {
            GameResultUiBootstrap.FixOverlayCanvases();
            GameResultUiBootstrap.Ensure();

            EnsureComponent<GameplayAudioManager>();
            EnsureComponent<GameplayBgmController>();
            EnsureComponent<GameCountdownTimer>();
            EnsureComponent<TeacherSpawnManager>();
            EnsureComponent<GameplayPauseUI>();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (player.GetComponent<PlayerInventory>() == null)
                    player.AddComponent<PlayerInventory>();
                if (player.GetComponent<FootstepAudio>() == null)
                    player.AddComponent<FootstepAudio>();
            }

            EnsureGameplayCanvasUi();
            ApplyGameplayFonts();
        }

        void Start()
        {
            ApplyGameplayFonts();
            foreach (var hud in Object.FindObjectsOfType<GameplayHintsHUD>(true))
            {
                hud.EnsurePanelStructure();
                hud.RebuildHintLines();
                hud.RefreshAll();
            }

            var manualPanel = GameObject.Find("ControlsManualPanel");
            if (manualPanel != null)
                ControlsManualLayout.Ensure(manualPanel);
        }

        static void ApplyGameplayFonts()
        {
            foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;
                TmpFontHelper.ApplyDefaultFontRecursive(canvas.gameObject);
            }

            foreach (var ui in Object.FindObjectsOfType<GameOverUI>(true))
            {
                if (ui != null)
                    TmpFontHelper.ApplyDefaultFontRecursive(ui.gameObject);
            }
        }

        void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() == null)
                gameObject.AddComponent<T>();
        }

        void EnsureGameplayCanvasUi()
        {
            var canvasGo = FixOverlayCanvasHierarchy();

            EnsureHintsPanel(canvasGo.transform);
            EnsureTimerHud(canvasGo.transform);
            EnsureMinimap(canvasGo.transform);
            EnsureControlsManual(canvasGo.transform);
        }

        static GameObject FixOverlayCanvasHierarchy()
        {
            var overlays = new System.Collections.Generic.List<Canvas>();
            foreach (var c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                var tr = c.transform;
                if (tr.parent != null)
                    tr.SetParent(null);

                if (tr.localScale.sqrMagnitude < 0.01f || tr.localScale != Vector3.one)
                    tr.localScale = Vector3.one;

                overlays.Add(c);
            }

            Canvas primary = null;
            foreach (var c in overlays)
            {
                if (c.name != "GameUI_Canvas") continue;
                if (primary == null || c.transform.childCount > primary.transform.childCount)
                    primary = c;
            }

            if (primary == null && overlays.Count > 0)
                primary = overlays[0];

            if (primary != null)
            {
                foreach (var c in overlays)
                {
                    if (c == primary) continue;
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
                return primary.gameObject;
            }

            var canvas = new GameObject("GameUI_Canvas");
            canvas.layer = LayerMask.NameToLayer("UI");
            var rect = canvas.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            var cv = canvas.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var newScaler = canvas.AddComponent<CanvasScaler>();
            newScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            newScaler.referenceResolution = new Vector2(1920f, 1080f);
            newScaler.matchWidthOrHeight = 0.5f;
            canvas.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        static void EnsureHintsPanel(Transform canvas)
        {
            var hud = Object.FindObjectOfType<GameplayHintsHUD>();
            if (hud == null)
            {
                var panel = UiRectUtil.CreateUiObject(canvas, "GameplayHintsPanel");
                hud = panel.AddComponent<GameplayHintsHUD>();
            }

            UpgradeExistingHintsPanel(hud, canvas);
        }

        static void UpgradeExistingHintsPanel(GameplayHintsHUD hud, Transform canvas)
        {
            if (hud.transform.parent != canvas)
                hud.transform.SetParent(canvas, false);

            hud.EnsurePanelStructure();

            if (Application.isPlaying)
            {
                hud.RebuildHintLines();
                hud.RefreshAll();
            }
        }

        static void EnsureTimerHud(Transform canvas)
        {
            if (Object.FindObjectOfType<GameplayTimerHUD>() != null) return;
            var go = new GameObject("TimerHUD");
            go.transform.SetParent(canvas, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -12f);
            rect.sizeDelta = new Vector2(400f, 80f);

            var timer = CreateCenterTmp(go.transform, "TimerText", "", 28f, new Vector2(0f, -8f));
            var msg = CreateCenterTmp(go.transform, "MessageText", "", 22f, new Vector2(0f, -40f));
            msg.color = new Color(1f, 0.75f, 0.35f);

            var hud = go.AddComponent<GameplayTimerHUD>();
            hud.Bind(timer, msg);
        }

        static void EnsureMinimap(Transform canvas)
        {
            if (Object.FindObjectOfType<MinimapHUD>() != null) return;
            var go = new GameObject("MinimapHUD");
            go.transform.SetParent(canvas, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.sizeDelta = new Vector2(180f, 180f);
            go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var imgGo = new GameObject("MapImage");
            imgGo.transform.SetParent(go.transform, false);
            var imgRect = imgGo.AddComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = new Vector2(4f, 4f);
            imgRect.offsetMax = new Vector2(-4f, -4f);
            var raw = imgGo.AddComponent<RawImage>();

            var mini = go.AddComponent<MinimapHUD>();
            mini.Bind(rect, raw);
        }

        static void EnsureControlsManual(Transform canvas)
        {
            var panelGo = GameObject.Find("ControlsManualPanel");
            if (panelGo == null)
            {
                panelGo = CreateControlsManualPanel(canvas);
            }

            ControlsManualLayout.Ensure(panelGo);

            var bodyText = panelGo.transform.Find("Box/BodyScroll/Viewport/Content/Body")?.GetComponent<TMP_Text>()
                           ?? panelGo.transform.Find("Box/Body")?.GetComponent<TMP_Text>();

            var misplaced = panelGo.GetComponent<ControlsManualUI>();
            if (misplaced != null)
                Object.Destroy(misplaced);

            var driver = Object.FindObjectOfType<ControlsManualUI>();
            if (driver == null)
            {
                var host = GameObject.Find("Managers") ?? GameStateManager.Instance?.gameObject;
                if (host != null)
                    driver = host.AddComponent<ControlsManualUI>();
            }

            if (driver != null)
                driver.Bind(panelGo, bodyText);

            panelGo.SetActive(false);
        }

        static GameObject CreateControlsManualPanel(Transform canvas)
        {
            var panelGo = new GameObject("ControlsManualPanel");
            panelGo.transform.SetParent(canvas, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);
            Stretch(panelGo);
            ControlsManualLayout.Ensure(panelGo);
            panelGo.SetActive(false);
            return panelGo;
        }

        static TMP_Text CreateHintLine(Transform parent, string name, string text, float bottom)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, bottom);
            rect.sizeDelta = new Vector2(580f, 28f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            tmp.raycastTarget = false;
            return tmp;
        }

        static TMP_Text CreateCenterTmp(Transform parent, string name, string text, float size, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(380f, 36f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
