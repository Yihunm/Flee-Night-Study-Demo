#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FleeNightStudy;
using FleeNightStudy.Editor;

namespace FleeNightStudy.EditorTools
{
    static class GameUiBuilder
    {
        const string DefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string DefaultFontPathAlt = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public static void ApplyTitleStyle(TMP_Text tmp, Color faceColor, float fontSize, float outlineWidth)
        {
            ApplyDefaultFont(tmp);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = faceColor;
            tmp.enableWordWrapping = false;
            SafeSetOutline(tmp, outlineWidth, Color.black);
        }

        public static void ApplyMenuItemStyle(TMP_Text tmp, Color faceColor, float fontSize)
        {
            ApplyDefaultFont(tmp);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = faceColor;
            tmp.enableWordWrapping = false;
            SafeSetOutline(tmp, 0.15f, new Color(0f, 0f, 0f, 0.75f));
        }

        static void ApplyDefaultFont(TMP_Text tmp)
        {
            if (tmp == null)
                return;

            var font = ResolveDefaultFont();
            if (font == null)
            {
                Debug.LogWarning("[GameUiBuilder] 无可用中文字体，请先运行 FleeNightStudy → 紧急修复 TMP 字体。");
                return;
            }

            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        static TMP_FontAsset ResolveDefaultFont()
        {
            var font = ChineseTmpFontSetup.GetUiFontAsset();
            if (TmpFontHelper.IsFontUsable(font))
                return font;

            if (TMP_Settings.defaultFontAsset != null && TmpFontHelper.IsFontUsable(TMP_Settings.defaultFontAsset))
                return TMP_Settings.defaultFontAsset;

            var font2 = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
            if (TmpFontHelper.IsFontUsable(font2))
                return font2;

            font2 = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPathAlt);
            if (TmpFontHelper.IsFontUsable(font2))
                return font2;

            return null;
        }

        static void SafeSetOutline(TMP_Text tmp, float width, Color color)
        {
            if (tmp == null || width <= 0f)
                return;
            if (tmp.font == null || tmp.fontSharedMaterial == null)
                return;

            try
            {
                tmp.outlineWidth = width;
                tmp.outlineColor = color;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameUiBuilder] 跳过 TMP outline：{ex.Message}");
            }
        }

        public static void ApplyModalBodyStyle(TMP_Text tmp, float fontSize)
        {
            ApplyDefaultFont(tmp);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            tmp.enableWordWrapping = true;
        }

        public static Button CreateModalButton(
            Transform parent,
            string label,
            float fontSize,
            float height)
        {
            var btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(parent, false);

            var layoutElement = btnGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 420f;
            layoutElement.minWidth = 320f;
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;

            var image = btnGo.AddComponent<Image>();
            image.color = new Color(0.14f, 0.18f, 0.26f, 0.96f);
            var button = btnGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.92f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            ApplyMenuItemStyle(tmp, new Color(0.96f, 0.97f, 1f, 1f), fontSize);
            tmp.raycastTarget = false;

            image.raycastTarget = true;
            return button;
        }

        public static Button CreateCenterTextButton(
            Transform parent,
            string label,
            Color textColor,
            float fontSize,
            float height)
        {
            var btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(parent, false);

            var layoutElement = btnGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 420f;
            layoutElement.minWidth = 320f;
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;

            var image = btnGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.42f);
            var button = btnGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.08f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.18f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            ApplyMenuItemStyle(tmp, textColor, fontSize);
            tmp.raycastTarget = false;

            image.raycastTarget = true;

            return button;
        }

        public static Button CreateBarButton(Transform bar, string label, Color color, float width)
        {
            var btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(bar, false);
            var layoutElement = btnGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.minHeight = 56f;

            btnGo.AddComponent<Image>().color = color;
            var button = btnGo.AddComponent<Button>();

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            ApplyMenuItemStyle(tmp, Color.white, 26f);

            return button;
        }

        public static void EnsureBottomActionBar(Transform panel, out Transform bar)
        {
            var existing = panel.Find("BottomActionBar");
            if (existing != null)
            {
                bar = existing;
                return;
            }

            var barGo = new GameObject("BottomActionBar");
            barGo.transform.SetParent(panel, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(0f, 120f);
            barGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var layout = barGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 28f;
            layout.padding = new RectOffset(40, 40, 18, 18);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            bar = barGo.transform;
        }
    }
}
#endif
