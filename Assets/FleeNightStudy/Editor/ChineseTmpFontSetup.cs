using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace FleeNightStudy.Editor
{
    /// <summary>
    /// UI 中文字体：直接生成到 Resources，作为主字体使用（不走 Fallback）。
    /// </summary>
    public static class ChineseTmpFontSetup
    {
        const string ResourcesFontDir = "Assets/FleeNightStudy/Resources/FleeNightStudy";
        const string ResourcesFontPath = "Assets/FleeNightStudy/Resources/FleeNightStudy/ChineseUI SDF.asset";
        const string DefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string DefaultFontPathAlt = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string SourceFontDir = "Assets/FleeNightStudy/Fonts/Source";
        const string CommonCharacters =
            GameUiCopy.AllUiCharacters +
            "再收集本课本以解锁大门还剩按E开门WASD移动游戏结束失败胜利重新开始0123456789" +
            "开始游戏退出返回逃离晚自习收集躲开老师逃出学校操作说明《》鼠标视角手电筒" +
            "简单普通难度玩家当前道具齐全名巡查班主任分钟切换使用加速辣条隐身校服磁铁闹钟粉笔弹校门口离开";

        [MenuItem("FleeNightStudy/修复中文字体")]
        public static void CreateAndApplyChineseFont()
        {
            EmergencyRepairTmpFonts(showDialog: true, applyAllScenes: true);
        }

        [MenuItem("FleeNightStudy/紧急修复 TMP 字体")]
        public static void EmergencyRepairTmpFontsMenu()
        {
            EmergencyRepairTmpFonts(showDialog: true, applyAllScenes: true);
        }

        public static void SanitizeDefaultProjectFonts()
        {
            RemoveBrokenChineseFallbacks();
            AssetDatabase.SaveAssets();
        }

        public static void ApplyMainMenuFontsSilent(GameObject menuRoot)
        {
            RemoveBrokenChineseFallbacks();
            var uiFont = GetUiFontAsset();
            if (uiFont == null || menuRoot == null)
            {
                Debug.LogWarning("[FleeNightStudy] 中文字体不可用，主菜单可能显示方框。请运行「紧急修复 TMP 字体」。");
                return;
            }

            ApplyFontToRoot(menuRoot, uiFont);
        }

        public static TMP_FontAsset GetUiFontAsset()
        {
            var chinese = LoadChineseFontAsset();
            if (IsChineseFontReady(chinese))
                return chinese;

            return null;
        }

        public static void EmergencyRepairTmpFonts(bool showDialog, bool applyAllScenes)
        {
            RemoveBrokenChineseFallbacks();

            var sourceFont = LoadOrImportSystemChineseFont();
            if (sourceFont == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "FleeNightStudy",
                        "未找到 simhei.ttf。\n请确认 C:\\Windows\\Fonts\\simhei.ttf 存在，\n" +
                        "或手动复制到 Assets/FleeNightStudy/Fonts/Source/ 后重试。",
                        "确定");
                return;
            }

            var chineseFont = ForceRecreateChineseFontAsset(sourceFont);
            if (chineseFont == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "FleeNightStudy",
                        "重建中文字体失败。\n请查看 Console 中 [FleeNightStudy] 日志。",
                        "确定");
                return;
            }

            int count = 0;
            if (applyAllScenes)
            {
                foreach (var tmp in Object.FindObjectsOfType<TMP_Text>(true))
                {
                    if (!ShouldApplyChineseFont(tmp))
                        continue;

                    Undo.RecordObject(tmp, "Apply UI TMP font");
                    tmp.font = chineseFont;
                    if (chineseFont.material != null)
                        tmp.fontSharedMaterial = chineseFont.material;
                    tmp.ForceMeshUpdate(true);
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    $"中文字体已重建（{chineseFont.characterTable.Count} 个字形）。\n" +
                    $"已更新 {count} 个 TMP。\n请保存场景后 Play。",
                    "确定");
        }

        static void RemoveBrokenChineseFallbacks()
        {
            foreach (var path in new[] { DefaultFontPath, DefaultFontPathAlt })
            {
                var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (primary == null || primary.fallbackFontAssetTable == null)
                    continue;

                var valid = new List<TMP_FontAsset>();
                foreach (var f in primary.fallbackFontAssetTable)
                {
                    if (f == null || f.name == "ChineseUI SDF" || !TmpFontHelper.IsFontUsable(f))
                        continue;
                    valid.Add(f);
                }

                primary.fallbackFontAssetTable = valid;
                EditorUtility.SetDirty(primary);
            }

            DeleteAssetIfExists("Assets/FleeNightStudy/Fonts/ChineseUI SDF.asset");
            var broken = LoadChineseFontAsset();
            if (broken != null && !IsChineseFontReady(broken))
                DeleteAssetIfExists(ResourcesFontPath);
        }

        static TMP_FontAsset ForceRecreateChineseFontAsset(Font sourceFont)
        {
            EnsureFolder(ResourcesFontDir);
            DeleteAssetIfExists(ResourcesFontPath);

            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (fontAsset == null)
            {
                Debug.LogError("[FleeNightStudy] CreateFontAsset 返回 null。");
                return null;
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            if (!fontAsset.TryAddCharacters(CommonCharacters, out string missing))
                Debug.LogWarning("[FleeNightStudy] TryAddCharacters 返回 false。");
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[FleeNightStudy] 未写入图集的字符：{missing}");

            SaveFontAssetWithSubAssets(fontAsset, ResourcesFontPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var loaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourcesFontPath);
            if (!IsChineseFontReady(loaded))
            {
                Debug.LogError("[FleeNightStudy] 字体保存后仍无法显示中文（缺少「逃」字）。请检查 simhei.ttf 是否包含 Font Data。");
                return null;
            }

            Debug.Log($"[FleeNightStudy] 中文字体就绪：{ResourcesFontPath}，字形数={loaded.characterTable.Count}");
            return loaded;
        }

        static void SaveFontAssetWithSubAssets(TMP_FontAsset fontAsset, string assetPath)
        {
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var tex = fontAsset.atlasTextures[i];
                    if (tex != null)
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }

            EditorUtility.SetDirty(fontAsset);
        }

        static void ApplyFontToRoot(GameObject root, TMP_FontAsset font)
        {
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(tmp, "Apply TMP font");
                tmp.font = font;
                if (font.material != null)
                    tmp.fontSharedMaterial = font.material;
                tmp.ForceMeshUpdate(true);
                EditorUtility.SetDirty(tmp);
            }

            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (input.textComponent == null)
                    continue;
                Undo.RecordObject(input.textComponent, "Apply TMP font");
                input.textComponent.font = font;
                if (font.material != null)
                    input.textComponent.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(input.textComponent);
            }
        }

        static TMP_FontAsset LoadChineseFontAsset()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourcesFontPath);
        }

        static bool IsChineseFontReady(TMP_FontAsset font)
        {
            return font != null
                   && TmpFontHelper.IsFontUsable(font)
                   && font.HasCharacter('逃')
                   && font.HasCharacter('《');
        }

        static bool ShouldApplyChineseFont(TMP_Text tmp)
        {
            if (tmp == null)
                return false;

            if (tmp.GetComponentInParent<MainMenuUI>(true) != null)
                return true;
            if (tmp.GetComponentInParent<GameplayHintsHUD>(true) != null)
                return true;
            if (tmp.GetComponentInParent<GameOverUI>(true) != null)
                return true;
            if (tmp.GetComponentInParent<ControlsManualUI>(true) != null)
                return true;

            return tmp.gameObject.name.IndexOf("Hint", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Font LoadOrImportSystemChineseFont()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { SourceFontDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var f = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (f != null)
                    return f;
            }

            const string simHei = @"C:\Windows\Fonts\simhei.ttf";
            if (File.Exists(simHei))
                return ImportFontToProject(simHei);

            string[] fallbacks =
            {
                @"C:\Windows\Fonts\msyh.ttc",
                @"C:\Windows\Fonts\simsun.ttc",
            };

            foreach (var path in fallbacks)
            {
                if (!File.Exists(path))
                    continue;
                var f = ImportFontToProject(path);
                if (f != null)
                    return f;
            }

            return null;
        }

        static Font ImportFontToProject(string systemPath)
        {
            EnsureFolder(SourceFontDir);
            var fileName = Path.GetFileName(systemPath);
            var destPath = $"{SourceFontDir}/{fileName}";

            if (!File.Exists(systemPath))
                return null;

            File.Copy(systemPath, destPath, true);
            AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(destPath) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.fontSize = 90;
                importer.fontRenderingMode = FontRenderingMode.Smooth;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Font>(destPath);
        }

        static void DeleteAssetIfExists(string assetPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                AssetDatabase.DeleteAsset(assetPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
