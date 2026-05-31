using TMPro;
using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>运行时修复 TMP 字体引用，避免图集丢失导致 UI 不显示。</summary>
    public static class TmpFontHelper
    {
        const string ChineseFontResourcePath = "FleeNightStudy/ChineseUI SDF";

        static TMP_FontAsset _cachedUiFont;

        public static TMP_FontAsset ResolveUiFont()
        {
            if (_cachedUiFont != null && IsFontUsable(_cachedUiFont))
                return _cachedUiFont;

            var chinese = Resources.Load<TMP_FontAsset>(ChineseFontResourcePath);
            if (TryUseUiFont(chinese))
                return _cachedUiFont;

            var all = Resources.LoadAll<TMP_FontAsset>("FleeNightStudy");
            if (all != null)
            {
                foreach (var font in all)
                {
                    if (TryUseUiFont(font))
                        return _cachedUiFont;
                }
            }

            return null;
        }

        static bool TryUseUiFont(TMP_FontAsset font)
        {
            if (!IsFontUsable(font))
                return false;

            _cachedUiFont = font;
            return true;
        }

        public static bool IsChineseFontReady(TMP_FontAsset font)
        {
            return IsFontUsable(font) && font.HasCharacter('按');
        }

        public static void ApplyDefaultFontRecursive(GameObject root)
        {
            if (root == null)
                return;

            var font = ResolveUiFont();
            if (font == null)
            {
                Debug.LogWarning("[FleeNightStudy] 未找到中文字体 Resources/FleeNightStudy/ChineseUI SDF，请运行「紧急修复 TMP 字体」。");
                return;
            }

            EnsureFontCharacters(font, GameUiCopy.AllUiCharacters);

            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null)
                    continue;
                ApplyUiFont(tmp, font);
                EnsureFontCharacters(font, tmp.text);
            }

            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (input?.textComponent != null)
                {
                    ApplyUiFont(input.textComponent, font);
                    EnsureFontCharacters(font, input.textComponent.text);
                }
                if (input?.placeholder is TMP_Text placeholder)
                {
                    ApplyUiFont(placeholder, font);
                    EnsureFontCharacters(font, placeholder.text);
                }
            }
        }

        static void EnsureFontCharacters(TMP_FontAsset font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text))
                return;

            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                return;

            font.TryAddCharacters(text, out _);
        }

        public static void ApplyUiFont(TMP_Text tmp, TMP_FontAsset font)
        {
            if (tmp == null || font == null)
                return;

            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;

            tmp.ForceMeshUpdate(true);
        }

        /// <summary>先绑定中文字体再赋值，避免 TMP 在 font 为空时抛 NullReferenceException。</summary>
        public static void SetUiText(TMP_Text tmp, string text)
        {
            if (tmp == null)
                return;

            var content = text ?? string.Empty;
            var font = ResolveUiFont();
            if (font != null)
            {
                ApplyUiFont(tmp, font);
                EnsureFontCharacters(font, content);
                tmp.text = content;
            }
            else
            {
                tmp.text = content;
                Debug.LogWarning("[FleeNightStudy] 未找到中文字体，部分 UI 可能显示为方框。请运行「紧急修复 TMP 字体」。");
            }

            tmp.ForceMeshUpdate(true);
        }

        /// <summary>与课本提示行相同的 HUD 单行样式。</summary>
        public static void ApplyHintLine(TMP_Text line, string text)
        {
            SetUiText(line, text);
        }

        public static bool IsFontUsable(TMP_FontAsset font)
        {
            if (font == null)
                return false;

            try
            {
                var textures = font.atlasTextures;
                if (textures == null || textures.Length == 0)
                    return false;

                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i] == null || textures[i].width <= 0)
                        return false;
                }

                return font.material != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
