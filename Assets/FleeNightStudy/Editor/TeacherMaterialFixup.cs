#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FleeNightStudy.EditorTools
{
    /// <summary>将老师模型材质转为 HDRP Lit 并保存为 .mat 资源（避免预制体重复 ID / 材质泄漏）。</summary>
    public static class TeacherMaterialFixup
    {
        const string GeneratedMaterialsFolder = "Assets/FleeNightStudy/Characters/Materials/Generated";

        public static void FixHierarchyMaterials(GameObject root, string texturesFolderAssetPath = null)
        {
            if (root == null)
                return;

            var shader = Shader.Find("HDRP/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("[FleeNightStudy] 未找到 HDRP/Lit，无法修复材质。");
                return;
            }

            EnsureGeneratedMaterialsFolder();
            var externalTextures = LoadTexturesFromFolder(texturesFolderAssetPath);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    var oldMat = shared[i];
                    var tex = ExtractAlbedoTexture(oldMat);
                    if (tex == null && oldMat != null)
                        tex = FindTextureForMaterial(oldMat.name, externalTextures);

                    string safeName = SanitizeAssetName($"{root.name}_{renderer.name}_{i}_{oldMat?.name}");
                    shared[i] = GetOrCreateMaterialAsset(safeName, tex, shader);
                }

                renderer.sharedMaterials = shared;
            }
        }

        static Material GetOrCreateMaterialAsset(string assetName, Texture tex, Shader shader)
        {
            string path = $"{GeneratedMaterialsFolder}/{assetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }

            if (tex != null)
            {
                if (mat.HasProperty("_BaseColorMap"))
                    mat.SetTexture("_BaseColorMap", tex);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static string SanitizeAssetName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }

        static void EnsureGeneratedMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FleeNightStudy/Characters"))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy", "Characters");
            if (!AssetDatabase.IsValidFolder("Assets/FleeNightStudy/Characters/Materials"))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy/Characters", "Materials");
            if (!AssetDatabase.IsValidFolder(GeneratedMaterialsFolder))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy/Characters/Materials", "Generated");
        }

        public static void ConfigureFbxMaterialImport(string fbxAssetPath)
        {
            var importer = AssetImporter.GetAtPath(fbxAssetPath) as ModelImporter;
            if (importer == null)
                return;

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.SaveAndReimport();
        }

        static Texture2D ExtractAlbedoTexture(Material mat)
        {
            if (mat == null)
                return null;

            if (mat.HasProperty("_BaseColorMap"))
            {
                var t = mat.GetTexture("_BaseColorMap");
                if (t is Texture2D tex)
                    return tex;
            }

            if (mat.mainTexture is Texture2D main)
                return main;

            if (mat.HasProperty("_MainTex"))
            {
                var t = mat.GetTexture("_MainTex");
                if (t is Texture2D tex)
                    return tex;
            }

            return null;
        }

        static Texture2D[] LoadTexturesFromFolder(string folderAssetPath)
        {
            if (string.IsNullOrEmpty(folderAssetPath) || !AssetDatabase.IsValidFolder(folderAssetPath))
                return System.Array.Empty<Texture2D>();

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderAssetPath });
            var list = new System.Collections.Generic.List<Texture2D>();
            foreach (var guid in guids)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                if (tex != null)
                    list.Add(tex);
            }

            return list.ToArray();
        }

        static Texture2D FindTextureForMaterial(string materialName, Texture2D[] textures)
        {
            if (textures == null || textures.Length == 0)
                return null;

            if (string.IsNullOrEmpty(materialName))
                return textures[0];

            var lower = materialName.ToLowerInvariant();
            if (lower.Contains("hair"))
                return FindTextureByKeyword(textures, "hair") ?? textures[0];
            if (lower.Contains("eye"))
                return FindTextureByKeyword(textures, "eye") ?? textures[0];
            if (lower.Contains("face") || lower.Contains("head"))
                return FindTextureByKeyword(textures, "face") ?? textures[0];
            if (lower.Contains("body"))
                return FindTextureByKeyword(textures, "body") ?? textures[0];

            foreach (var tex in textures)
            {
                if (tex == null)
                    continue;
                if (lower.Contains(tex.name.ToLowerInvariant()) ||
                    tex.name.ToLowerInvariant().Contains(lower))
                    return tex;
            }

            return textures[0];
        }

        static Texture2D FindTextureByKeyword(Texture2D[] textures, string keyword)
        {
            foreach (var tex in textures)
            {
                if (tex != null && tex.name.ToLowerInvariant().Contains(keyword))
                    return tex;
            }

            return null;
        }

        [MenuItem("FleeNightStudy/老师角色/修复全部老师材质（HDRP）", false, 12)]
        public static void FixExistingTeacherMaterialsMenu()
        {
            RebuildPatrolPrefabIfSourceReady(TeacherNpcModelRegistry.RaanaMyGoSourceFolder,
                TeacherNpcModelRegistry.RaanaPatrolPrefabAsset,
                TeacherCharacterPrefabSetup.ImportRaanaMyGoAsPatrolTeacher);
            RebuildPatrolPrefabIfSourceReady(TeacherNpcModelRegistry.TomoriMyGoSourceFolder,
                TeacherNpcModelRegistry.TomoriPatrolPrefabAsset,
                TeacherCharacterPrefabSetup.ImportTomoriMyGoAsPatrolTeacher);
            RebuildPatrolPrefabIfSourceReady(TeacherNpcModelRegistry.AnonMyGoSourceFolder,
                TeacherNpcModelRegistry.AnonPatrolPrefabAsset,
                TeacherCharacterPrefabSetup.ImportAnonMyGoAsPatrolTeacher);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TeacherNpcModelRegistry.SoyoBodyFbxAsset) != null)
                RebuildSoyoHeadTeacherPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "已尝试将老师模型材质转为 HDRP 并重新绑定贴图。\n请 Play 查看。",
                "确定");
        }

        static void RebuildPatrolPrefabIfSourceReady(string myGoFolder, string prefabAsset, System.Action importAction)
        {
            if (!HasMyGoBodyInFolder(myGoFolder))
                return;

            DeleteAssetIfExists(prefabAsset);
            var resourcesName = Path.GetFileName(prefabAsset);
            DeleteAssetIfExists($"Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/{resourcesName}");
            importAction();
        }

        static bool HasMyGoBodyInFolder(string folderAsset)
        {
            if (!AssetDatabase.IsValidFolder(folderAsset))
                return false;

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folderAsset }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("_Body.fbx", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void RebuildAnonPatrolPrefab()
        {
            DeleteAssetIfExists(TeacherNpcModelRegistry.AnonPatrolPrefabAsset);
            DeleteAssetIfExists("Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/Anon_Patrol.prefab");

            TeacherCharacterPrefabSetup.ImportAnonMyGoAsPatrolTeacher();
        }

        public static void RebuildSoyoHeadTeacherPrefab()
        {
            DeleteAssetIfExists(TeacherNpcModelRegistry.SoyoHeadTeacherPrefabAsset);
            DeleteAssetIfExists("Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/Soyo_HeadTeacher.prefab");

            TeacherCharacterPrefabSetup.ImportSoyoMyGoAsHeadTeacher();
        }

        static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
        }

        static void RebuildPrefabWithMaterials(string prefabPath, string modelPath, string texturesFolder, bool headTeacher)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null)
            {
                Debug.LogWarning($"[FleeNightStudy] 找不到模型: {modelPath}");
                return;
            }

            TeacherCharacterPrefabSetup.BindCharacterAsset(source, headTeacher,
                Path.GetFileNameWithoutExtension(prefabPath), prefabPath, false);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            FixHierarchyMaterials(instance, texturesFolder);
            TeacherLocomotionSetup.ApplyLocomotionToVisualRoot(instance, headTeacher);
            TeacherCharacterPrefabSetup.SaveVisualPrefabPublic(instance, prefabPath);
            CopyToResources(prefabPath);
        }

        static void CopyToResources(string sourcePrefab)
        {
            var name = Path.GetFileName(sourcePrefab);
            var dest = $"Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/{name}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dest) != null)
                AssetDatabase.DeleteAsset(dest);
            AssetDatabase.CopyAsset(sourcePrefab, dest);
        }
    }
}
#endif
