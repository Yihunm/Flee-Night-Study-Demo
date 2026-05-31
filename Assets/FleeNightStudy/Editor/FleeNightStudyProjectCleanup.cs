#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FleeNightStudy.EditorTools
{
    public static class FleeNightStudyProjectCleanup
    {
        [MenuItem("FleeNightStudy/老师角色/清理冗余资源", false, 30)]
        public static void RunCleanup()
        {
            if (!EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    "将删除未使用的旧模型目录、孤立 .meta 等。\n是否继续？",
                    "删除",
                    "取消"))
                return;

            int n = 0;
            n += DeleteIfExists(TeacherNpcModelRegistry.AnonSourceFolder + "/source");
            n += DeleteIfExists(TeacherNpcModelRegistry.AnonSourceFolder + "/textures");
            n += DeleteIfExists(TeacherNpcModelRegistry.SoyoSourceFolder + "/scene.gltf");
            n += DeleteIfExists(TeacherNpcModelRegistry.SoyoSourceFolder + "/scene.bin");
            n += DeleteIfExists(TeacherNpcModelRegistry.SoyoSourceFolder + "/license.txt");
            n += DeleteIfExists(TeacherNpcModelRegistry.AnonMyGoSourceFolder + "/Materials");
            n += DeleteIfExists(TeacherNpcModelRegistry.SoyoMyGoSourceFolder + "/Materials");
            n += DeleteIfExists(TeacherNpcModelRegistry.AnonMyGoSourceFolder + "/Anon.pmx");
            n += DeleteIfExists(TeacherNpcModelRegistry.SoyoMyGoSourceFolder + "/Soyo.pmx");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("FleeNightStudy", $"已清理 {n} 项冗余资源。", "确定");
        }

        static int DeleteIfExists(string assetPath)
        {
            var full = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(full) && !Directory.Exists(full))
                return 0;

            if (Directory.Exists(full))
                Directory.Delete(full, true);
            else
                File.Delete(full);

            var meta = full + ".meta";
            if (File.Exists(meta))
                File.Delete(meta);

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            return 1;
        }
    }
}
#endif
