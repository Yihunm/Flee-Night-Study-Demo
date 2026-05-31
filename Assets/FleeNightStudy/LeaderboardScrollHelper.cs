using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleeNightStudy
{
    /// <summary>
    /// 排行榜列表 ScrollRect 搭建与刷新（支持前 10 名 + 滚轮/拖拽滚动）。
    /// </summary>
    public static class LeaderboardScrollHelper
    {
        public const float ScrollAreaPreferredHeight = 300f;

        public static TMP_Text EnsureScrollBody(Transform box, out ScrollRect scrollRect)
        {
            scrollRect = null;
            if (box == null)
                return null;

            var existingScroll = box.Find("LeaderboardScroll");
            if (existingScroll != null)
            {
                scrollRect = existingScroll.GetComponent<ScrollRect>();
                var existingBody = existingScroll.Find("Viewport/Content/Body")?.GetComponent<TMP_Text>();
                if (existingBody != null)
                {
                    FinalizeScrollSetup(existingScroll, scrollRect, existingBody);
                    return existingBody;
                }
            }

            MigrateLegacyBody(box);

            GameObject scrollGo;
            if (existingScroll != null)
                scrollGo = existingScroll.gameObject;
            else
                scrollGo = UiRectUtil.CreateUiObject(box, "LeaderboardScroll");

            scrollRect = scrollGo.GetComponent<ScrollRect>() ?? scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.inertia = true;

            var viewport = EnsureViewport(scrollGo.transform);
            var content = EnsureContent(viewport);
            var body = EnsureBody(content);

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            FinalizeScrollSetup(scrollGo.transform, scrollRect, body);
            scrollGo.transform.SetSiblingIndex(Mathf.Min(1, box.childCount - 1));
            return body;
        }

        static RectTransform EnsureViewport(Transform scrollTr)
        {
            var viewportTr = scrollTr.Find("Viewport");
            if (viewportTr != null && UiRectUtil.GetRectTransform(viewportTr.gameObject) == null)
                DestroyUiObject(viewportTr.gameObject);

            if (viewportTr == null || UiRectUtil.GetRectTransform(viewportTr.gameObject) == null)
            {
                var viewportGo = UiRectUtil.CreateUiObject(scrollTr, "Viewport");
                Stretch(viewportGo);
                viewportGo.AddComponent<RectMask2D>();
                var viewportImage = viewportGo.AddComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImage.raycastTarget = true;
                return UiRectUtil.GetRectTransform(viewportGo);
            }

            var existing = UiRectUtil.GetRectTransform(viewportTr.gameObject);
            Stretch(viewportTr.gameObject);
            if (viewportTr.GetComponent<RectMask2D>() == null)
                viewportTr.gameObject.AddComponent<RectMask2D>();
            var image = viewportTr.GetComponent<Image>() ?? viewportTr.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;
            return existing;
        }

        static RectTransform EnsureContent(RectTransform viewport)
        {
            var contentTr = viewport.Find("Content");
            if (contentTr != null && UiRectUtil.GetRectTransform(contentTr.gameObject) == null)
                DestroyUiObject(contentTr.gameObject);

            if (contentTr == null)
            {
                var contentGo = UiRectUtil.CreateUiObject(viewport, "Content");
                SetupContentRect(contentGo);
                var fitter = contentGo.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                return UiRectUtil.GetRectTransform(contentGo);
            }

            SetupContentRect(contentTr.gameObject);
            var contentFitter = contentTr.GetComponent<ContentSizeFitter>()
                                ?? contentTr.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return UiRectUtil.GetRectTransform(contentTr.gameObject);
        }

        static TMP_Text EnsureBody(RectTransform content)
        {
            var bodyTr = content.Find("Body");
            if (bodyTr != null && UiRectUtil.GetRectTransform(bodyTr.gameObject) == null)
                DestroyUiObject(bodyTr.gameObject);

            TMP_Text body;
            if (bodyTr == null)
            {
                var bodyGo = UiRectUtil.CreateUiObject(content, "Body");
                SetupBodyRect(bodyGo);
                var bodyLayout = bodyGo.AddComponent<LayoutElement>();
                bodyLayout.minWidth = 520f;
                bodyLayout.preferredWidth = 520f;
                body = bodyGo.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                body = bodyTr.GetComponent<TMP_Text>() ?? bodyTr.gameObject.AddComponent<TextMeshProUGUI>();
                SetupBodyRect(bodyTr.gameObject);
            }

            return body;
        }

        static void FinalizeScrollSetup(Transform scrollTr, ScrollRect scrollRect, TMP_Text body)
        {
            if (scrollTr == null || scrollRect == null || body == null)
                return;

            FixScrollRectForLayout(UiRectUtil.GetRectTransform(scrollTr.gameObject));

            var viewport = scrollRect.viewport ?? UiRectUtil.GetRectTransform(scrollTr.Find("Viewport")?.gameObject);
            var content = scrollRect.content ?? UiRectUtil.GetRectTransform(scrollTr.Find("Viewport/Content")?.gameObject);
            if (viewport != null)
            {
                Stretch(viewport.gameObject);
                scrollRect.viewport = viewport;
                var vpImage = viewport.GetComponent<Image>();
                if (vpImage != null)
                    vpImage.raycastTarget = true;
            }

            if (content != null)
            {
                SetupContentRect(content.gameObject);
                scrollRect.content = content;
                var contentFitter = content.GetComponent<ContentSizeFitter>()
                                      ?? content.gameObject.AddComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            ApplyBodyTextSettings(body);
            RemoveBlockingRootGraphic(scrollTr.gameObject);

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;
        }

        static void FixScrollRectForLayout(RectTransform scrollRect)
        {
            if (scrollRect == null)
                return;

            scrollRect.anchorMin = new Vector2(0f, 1f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = Vector2.zero;
            scrollRect.sizeDelta = new Vector2(0f, ScrollAreaPreferredHeight);

            var layout = scrollRect.GetComponent<LayoutElement>() ?? scrollRect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 220f;
            layout.preferredHeight = ScrollAreaPreferredHeight;
            layout.flexibleHeight = 0f;
            layout.minWidth = 0f;
            layout.preferredWidth = -1f;
            layout.flexibleWidth = 1f;
        }

        static void SetupContentRect(GameObject contentGo)
        {
            var contentRect = UiRectUtil.GetRectTransform(contentGo);
            if (contentRect == null)
                return;

            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
        }

        static void SetupBodyRect(GameObject bodyGo)
        {
            var bodyRect = UiRectUtil.GetRectTransform(bodyGo);
            if (bodyRect == null)
                return;

            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(0f, 0f);

            var bodyFitter = bodyGo.GetComponent<ContentSizeFitter>() ?? bodyGo.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void ApplyBodyTextSettings(TMP_Text body)
        {
            body.alignment = TextAlignmentOptions.TopLeft;
            body.enableWordWrapping = false;
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 2f;
            body.fontSize = 20f;
            body.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            body.raycastTarget = false;
        }

        /// <summary>根节点透明 Image 会挡住 Viewport 的滚轮事件，运行时移除。</summary>
        static void RemoveBlockingRootGraphic(GameObject scrollGo)
        {
            if (scrollGo == null)
                return;

            foreach (var image in scrollGo.GetComponents<Image>())
            {
                if (image == null || scrollGo.transform.Find("Viewport") == null)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(image);
                else
                    Object.DestroyImmediate(image);
            }
        }

        static void MigrateLegacyBody(Transform box)
        {
            var legacyBody = box.Find("BodyViewport/Body") ?? box.Find("Body");
            if (legacyBody == null)
                return;

            DestroyUiObject(legacyBody.parent != box ? legacyBody.parent.gameObject : legacyBody.gameObject);

            var legacyViewport = box.Find("BodyViewport");
            if (legacyViewport != null)
                DestroyUiObject(legacyViewport.gameObject);
        }

        public static void RefreshScrollContent(TMP_Text body)
        {
            if (body == null)
                return;

            body.ForceMeshUpdate(true, true);

            float width = body.rectTransform.rect.width;
            if (width < 1f)
                width = 520f;

            float textHeight = body.GetPreferredValues(width, 0f).y + 12f;
            body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

            var content = body.transform.parent as RectTransform;
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                float contentHeight = Mathf.Max(textHeight, LayoutUtility.GetPreferredHeight(content));
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            }

            var scroll = body.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 1f;
                scroll.enabled = true;
            }
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

        static void DestroyUiObject(GameObject go)
        {
            if (go == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }
    }
}
