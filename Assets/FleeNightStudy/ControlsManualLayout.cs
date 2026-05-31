using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleeNightStudy
{
    /// <summary>操作手册弹窗布局：标题 + 可滚动正文 + 底部返回。</summary>
    public static class ControlsManualLayout
    {
        public static void Ensure(GameObject panelRoot)
        {
            if (panelRoot == null)
                return;

            var box = panelRoot.transform.Find("Box");
            if (box == null)
            {
                box = BuildBox(panelRoot.transform).transform;
            }
            else if (box.Find("BodyScroll") == null)
            {
                Object.Destroy(box.gameObject);
                box = BuildBox(panelRoot.transform).transform;
            }
            else
            {
                FixExistingBox(box);
            }

            var body = box.Find("BodyScroll/Viewport/Content/Body")?.GetComponent<TMP_Text>();
            if (body != null)
            {
                body.fontSize = 24f;
                body.alignment = TextAlignmentOptions.TopLeft;
                body.color = new Color(0.08f, 0.09f, 0.12f, 1f);
                body.enableWordWrapping = true;
                body.raycastTarget = false;
                TmpFontHelper.SetUiText(body, GameUiCopy.InstructionsBody);
            }

            var title = box.Find("TitleText")?.GetComponent<TMP_Text>();
            if (title != null)
                TmpFontHelper.SetUiText(title, "操作手册");

            EnsureBackButton(box);
            TmpFontHelper.ApplyDefaultFontRecursive(box.gameObject);
        }

        static GameObject BuildBox(Transform panelRoot)
        {
            var box = new GameObject("Box");
            box.transform.SetParent(panelRoot, false);

            var boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(720f, 520f);
            box.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.97f);

            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateTitle(box.transform);
            CreateBodyScroll(box.transform);
            CreateBackButton(box.transform);

            return box;
        }

        static void FixExistingBox(Transform box)
        {
            var layout = box.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(32, 32, 28, 28);
                layout.spacing = 14f;
                layout.childControlHeight = true;
            }

            var oldBody = box.Find("Body");
            if (oldBody != null)
                Object.Destroy(oldBody.gameObject);

            var back = box.Find("ManualBackButton");
            if (back != null)
                back.SetAsLastSibling();
        }

        static void CreateTitle(Transform box)
        {
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(box, false);
            titleGo.AddComponent<RectTransform>();
            var le = titleGo.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;
            le.minHeight = 48f;

            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.fontSize = 34f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.06f, 0.07f, 0.10f, 1f);
            title.raycastTarget = false;
            TmpFontHelper.SetUiText(title, "操作手册");
        }

        static void CreateBodyScroll(Transform box)
        {
            var scrollGo = UiRectUtil.CreateUiObject(box, "BodyScroll");

            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.preferredHeight = 360f;
            scrollLe.minHeight = 280f;
            scrollLe.flexibleHeight = 1f;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewportGo = UiRectUtil.CreateUiObject(scrollGo.transform, "Viewport");
            var viewportRect = UiRectUtil.GetRectTransform(viewportGo);
            Stretch(viewportRect);
            viewportGo.AddComponent<RectMask2D>();
            var vpImage = viewportGo.AddComponent<Image>();
            vpImage.color = new Color(1f, 1f, 1f, 0.01f);
            vpImage.raycastTarget = true;

            var contentGo = UiRectUtil.CreateUiObject(viewportGo.transform, "Content");
            var contentRect = UiRectUtil.GetRectTransform(contentGo);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bodyGo = UiRectUtil.CreateUiObject(contentGo.transform, "Body");
            var bodyRect = UiRectUtil.GetRectTransform(bodyGo);
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.sizeDelta = new Vector2(0f, 0f);

            var bodyLe = bodyGo.AddComponent<LayoutElement>();
            bodyLe.minWidth = 600f;
            bodyLe.preferredWidth = 600f;

            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.fontSize = 24f;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            body.enableWordWrapping = true;
            body.richText = false;
            body.raycastTarget = false;
            TmpFontHelper.SetUiText(body, GameUiCopy.InstructionsBody);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
        }

        static void CreateBackButton(Transform box)
        {
            if (box.Find("ManualBackButton") != null)
                return;

            var btnGo = new GameObject("ManualBackButton");
            btnGo.transform.SetParent(box, false);
            btnGo.AddComponent<RectTransform>();
            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;
            le.minHeight = 44f;

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
            label.fontSize = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.97f, 1f, 1f);
            label.raycastTarget = false;
            TmpFontHelper.SetUiText(label, "返回");

            btnGo.transform.SetAsLastSibling();
        }

        static void EnsureBackButton(Transform box)
        {
            CreateBackButton(box);
            var back = box.Find("ManualBackButton");
            if (back != null)
                back.SetAsLastSibling();

            var label = box.Find("ManualBackButton/Text")?.GetComponent<TMP_Text>();
            if (label != null)
                TmpFontHelper.SetUiText(label, "返回");
        }

        static void Stretch(GameObject go)
        {
            if (go == null)
                return;

            Stretch(UiRectUtil.GetRectTransform(go));
        }

        static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
