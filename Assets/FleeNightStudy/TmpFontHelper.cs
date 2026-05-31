using TMPro;
using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>Runtime TMP font helper with safe fallbacks for cloned projects.</summary>
    public static class TmpFontHelper
    {
        const string ChineseFontResourcePath = "FleeNightStudy/ChineseUI SDF";
        const string TmpDefaultFontResourcePath = "Fonts & Materials/LiberationSans SDF";

        static TMP_FontAsset _cachedUiFont;
        static bool _warnedFallback;
        static bool _warnedMissing;

        public static TMP_FontAsset ResolveUiFont()
        {
            if (_cachedUiFont != null && IsFontUsable(_cachedUiFont))
                return _cachedUiFont;

            if (TryUseUiFont(Resources.Load<TMP_FontAsset>(ChineseFontResourcePath)))
                return _cachedUiFont;

            if (TryUseUiFont(Resources.Load<TMP_FontAsset>(TmpDefaultFontResourcePath)))
            {
                WarnFallbackOnce();
                return _cachedUiFont;
            }

            if (TryUseUiFont(TMP_Settings.defaultFontAsset))
            {
                WarnFallbackOnce();
                return _cachedUiFont;
            }

            var all = Resources.LoadAll<TMP_FontAsset>(string.Empty);
            if (all != null)
            {
                foreach (var font in all)
                {
                    if (TryUseUiFont(font))
                    {
                        WarnFallbackOnce();
                        return _cachedUiFont;
                    }
                }
            }

            WarnMissingOnce();
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
            return IsFontUsable(font) && font.HasCharacter('中');
        }

        public static void ApplyDefaultFontRecursive(GameObject root)
        {
            if (root == null)
                return;

            var font = ResolveUiFont();
            if (font == null)
                return;

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
            }

            tmp.text = content;
            tmp.ForceMeshUpdate(true);
        }

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

        static void WarnFallbackOnce()
        {
            if (_warnedFallback)
                return;

            _warnedFallback = true;
            Debug.LogWarning(
                "[FleeNightStudy] ChineseUI SDF was not available. Falling back to another TMP font. " +
                "Chinese text may not render correctly until the Chinese TMP font is regenerated.");
        }

        static void WarnMissingOnce()
        {
            if (_warnedMissing)
                return;

            _warnedMissing = true;
            Debug.LogWarning(
                "[FleeNightStudy] No usable TMP font was found. Import TMP Essentials or regenerate the Chinese TMP font.");
        }
    }
}
