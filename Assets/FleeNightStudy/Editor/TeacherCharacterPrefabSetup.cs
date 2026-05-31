#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FleeNightStudy.EditorTools
{
    /// <summary>生成老师用的动漫风角色视觉预制体，或从已有 FBX/预制体转换。</summary>
    public static class TeacherCharacterPrefabSetup
    {
        const string CharactersFolder = "Assets/FleeNightStudy/Characters";
        const string TeachersFolder = "Assets/FleeNightStudy/Characters/Teachers";
        const string ResourcesTeachersFolder = "Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers";

        const string AnonDesktopFolder =
            @"C:\Users\lenovo\Desktop\MyGO 6th Anniversary\MyGO Default v2\Anon- For The Rest Of Our Lives, Beginning Now";

        const string TomoriDesktopFolder =
            @"C:\Users\lenovo\Desktop\MyGO 6th Anniversary\MyGO Default v2\Tomori- For The Rest Of Our Lives, Beginning Now";

        const string RaanaDesktopFolder =
            @"C:\Users\lenovo\Desktop\MyGO 6th Anniversary\MyGO Default v2\Raana- For The Rest Of Our Lives, Beginning Now";

        const string SoyoDesktopFolder =
            @"C:\Users\lenovo\Desktop\MyGO 6th Anniversary\MyGO Default v2\Soyo- For The Rest Of Our Lives, Beginning Now";

        [MenuItem("FleeNightStudy/老师角色/一键导入全部 MyGO 老师", false, 0)]
        public static void ImportAllMyGoTeachers()
        {
            ImportRaanaMyGoAsPatrolTeacher();
            ImportTomoriMyGoAsPatrolTeacher();
            ImportAnonMyGoAsPatrolTeacher();
            ImportSoyoMyGoAsHeadTeacher();
            TeacherLocomotionSetup.TrySetupLocomotionSilent();
            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "已导入巡查老师：\n" +
                "• 三楼 Raana（Raana_Patrol）\n" +
                "• 二楼 Tomori（Tomori_Patrol）\n" +
                "• 一楼 Anon（Anon_Patrol）\n" +
                "• 班主任 Soyo（Soyo_HeadTeacher）\n\n" +
                "请 Play 测试（简单难度三名巡查）。",
                "确定");
        }

        [MenuItem("FleeNightStudy/老师角色/导入 Raana（三楼巡查）", false, 1)]
        public static void ImportRaanaMyGoAsPatrolTeacher()
        {
            ImportMyGoTeacher(new MyGoImportParams
            {
                DesktopFolder = RaanaDesktopFolder,
                ProjectFolderAsset = TeacherNpcModelRegistry.RaanaMyGoSourceFolder,
                ParentSourceFolder = TeacherNpcModelRegistry.RaanaSourceFolder,
                SubFolderName = "Raana_MyGO",
                BodyFileName = null,
                HeadFileName = null,
                PrefabAsset = TeacherNpcModelRegistry.RaanaPatrolPrefabAsset,
                PrefabRootName = "Raana_Patrol",
                ResourcesPrefabFile = "Raana_Patrol.prefab",
                HeadTeacher = false,
                RoleLabel = "Raana（要乐奈）",
                SuccessHint = "已用 Raana（MyGO 6th）生成三楼巡查老师（Raana_Patrol）。"
            });
        }

        [MenuItem("FleeNightStudy/老师角色/导入 Tomori（二楼巡查）", false, 2)]
        public static void ImportTomoriMyGoAsPatrolTeacher()
        {
            ImportMyGoTeacher(new MyGoImportParams
            {
                DesktopFolder = TomoriDesktopFolder,
                ProjectFolderAsset = TeacherNpcModelRegistry.TomoriMyGoSourceFolder,
                ParentSourceFolder = TeacherNpcModelRegistry.TomoriSourceFolder,
                SubFolderName = "Tomori_MyGO",
                BodyFileName = null,
                HeadFileName = null,
                PrefabAsset = TeacherNpcModelRegistry.TomoriPatrolPrefabAsset,
                PrefabRootName = "Tomori_Patrol",
                ResourcesPrefabFile = "Tomori_Patrol.prefab",
                HeadTeacher = false,
                RoleLabel = "Tomori（高松灯）",
                SuccessHint = "已用 Tomori（MyGO 6th）生成二楼巡查老师（Tomori_Patrol）。"
            });
        }

        [MenuItem("FleeNightStudy/老师角色/导入爱音（一楼巡查）", false, 3)]
        public static void ImportAnonMyGoAsPatrolTeacher()
        {
            ImportMyGoTeacher(new MyGoImportParams
            {
                DesktopFolder = AnonDesktopFolder,
                ProjectFolderAsset = TeacherNpcModelRegistry.AnonMyGoSourceFolder,
                ParentSourceFolder = TeacherNpcModelRegistry.AnonSourceFolder,
                SubFolderName = "Anon_MyGO",
                BodyFileName = "CH_037_cos_live_default_Body.fbx",
                HeadFileName = "CH_037_cos_live_default_Head.fbx",
                PrefabAsset = TeacherNpcModelRegistry.AnonPatrolPrefabAsset,
                PrefabRootName = "Anon_Patrol",
                ResourcesPrefabFile = "Anon_Patrol.prefab",
                HeadTeacher = false,
                RoleLabel = "爱音",
                SuccessHint = "已用爱音（MyGO 6th）生成一楼巡查老师（Anon_Patrol）。"
            });
        }

        [MenuItem("FleeNightStudy/老师角色/导入素世（班主任）", false, 4)]
        public static void ImportSoyoMyGoAsHeadTeacher()
        {
            ImportMyGoTeacher(new MyGoImportParams
            {
                DesktopFolder = SoyoDesktopFolder,
                ProjectFolderAsset = TeacherNpcModelRegistry.SoyoMyGoSourceFolder,
                ParentSourceFolder = TeacherNpcModelRegistry.SoyoSourceFolder,
                SubFolderName = "Soyo_MyGO",
                BodyFileName = "CH_039_cos_live_default_Body.fbx",
                HeadFileName = "CH_039_cos_live_default_Head.fbx",
                PrefabAsset = TeacherNpcModelRegistry.SoyoHeadTeacherPrefabAsset,
                PrefabRootName = "Soyo_HeadTeacher",
                ResourcesPrefabFile = "Soyo_HeadTeacher.prefab",
                HeadTeacher = true,
                RoleLabel = "素世（Soyo）",
                SuccessHint = "已用素世（MyGO 6th）替换班主任（Soyo_HeadTeacher）。"
            });
        }

        struct MyGoImportParams
        {
            public string DesktopFolder;
            public string ProjectFolderAsset;
            public string ParentSourceFolder;
            public string SubFolderName;
            public string BodyFileName;
            public string HeadFileName;
            public string PrefabAsset;
            public string PrefabRootName;
            public string ResourcesPrefabFile;
            public bool HeadTeacher;
            public string RoleLabel;
            public string SuccessHint;
        }

        static void ImportMyGoTeacher(MyGoImportParams p)
        {
            SyncMyGoFromDesktop(p.DesktopFolder, p.ProjectFolderAsset, p.ParentSourceFolder, p.SubFolderName);
            AssetDatabase.Refresh();

            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            var folderOnDisk = Path.Combine(projectRoot,
                p.ProjectFolderAsset.Replace('/', Path.DirectorySeparatorChar));

            if (!TryResolveMyGoFbxFileNames(folderOnDisk, p.BodyFileName, p.HeadFileName,
                    out string bodyFileName, out string headFileName))
            {
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    $"{p.RoleLabel} 文件夹中未找到 *_Body.fbx 与 *_Head.fbx。\n\n桌面路径：\n{p.DesktopFolder}",
                    "确定");
                return;
            }

            string bodyAsset = $"{p.ProjectFolderAsset}/{bodyFileName}";
            string headAsset = $"{p.ProjectFolderAsset}/{headFileName}";

            TeacherMaterialFixup.ConfigureFbxMaterialImport(bodyAsset);
            TeacherMaterialFixup.ConfigureFbxMaterialImport(headAsset);
            AssetDatabase.Refresh();

            if (!ValidateMyGoSourceFiles(p.ProjectFolderAsset, bodyFileName, headFileName, bodyAsset, headAsset,
                    out string missing))
            {
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    $"{p.RoleLabel} 模型文件不完整，无法导入。\n\n{missing}\n\n" +
                    $"需要：\n• {p.BodyFileName}\n• {p.HeadFileName}\n\n" +
                    $"请从桌面 MyGO 文件夹复制到：\n{p.ProjectFolderAsset}",
                    "确定");
                return;
            }

            var body = AssetDatabase.LoadAssetAtPath<GameObject>(bodyAsset);
            var head = AssetDatabase.LoadAssetAtPath<GameObject>(headAsset);
            if (body == null)
            {
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    "Unity 仍无法加载身体 FBX。\n请在 Project 中选中该文件并 Reimport。\n\n" + bodyAsset,
                    "确定");
                return;
            }

            TeacherLocomotionSetup.SetHumanoid(bodyAsset);
            if (head != null)
                TeacherLocomotionSetup.SetHumanoid(headAsset);

            SaveVisualPrefab(
                WrapImportedModelParts(body, head, p.PrefabRootName, p.HeadTeacher, p.ProjectFolderAsset),
                p.PrefabAsset);
            CopyToResources(p.PrefabAsset, $"{ResourcesTeachersFolder}/{p.ResourcesPrefabFile}");

            TeacherLocomotionSetup.TrySetupLocomotionSilent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                $"{p.SuccessHint}\n头已对齐并挂到身体 Head 骨骼。\n请 Play 测试（班主任在拾取第一本教材后出现）。",
                "确定");
        }

        public static void SyncAnonFromDesktop(string desktopFolder) =>
            SyncMyGoFromDesktop(desktopFolder, TeacherNpcModelRegistry.AnonMyGoSourceFolder,
                TeacherNpcModelRegistry.AnonSourceFolder, "Anon_MyGO");

        public static void SyncSoyoFromDesktop(string desktopFolder) =>
            SyncMyGoFromDesktop(desktopFolder, TeacherNpcModelRegistry.SoyoMyGoSourceFolder,
                TeacherNpcModelRegistry.SoyoSourceFolder, "Soyo_MyGO");

        static void SyncMyGoFromDesktop(string desktopFolder, string projectFolderAsset, string parentSourceFolder,
            string subFolderName)
        {
            if (!Directory.Exists(desktopFolder))
            {
                Debug.LogWarning($"[FleeNightStudy] 桌面 MyGO 文件夹不存在: {desktopFolder}");
                return;
            }

            EnsureFolders();
            if (!AssetDatabase.IsValidFolder(projectFolderAsset))
            {
                var parentName = Path.GetFileName(parentSourceFolder);
                if (!AssetDatabase.IsValidFolder(parentSourceFolder))
                    AssetDatabase.CreateFolder($"{CharactersFolder}/Source", parentName);
                AssetDatabase.CreateFolder(parentSourceFolder, subFolderName);
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            var destFolder = Path.Combine(projectRoot,
                projectFolderAsset.Replace('/', Path.DirectorySeparatorChar));

            foreach (var file in Directory.GetFiles(desktopFolder))
                File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true);

            AssetDatabase.Refresh();
        }

        static bool TryResolveMyGoFbxFileNames(string folderOnDisk, string bodyHint, string headHint,
            out string bodyFileName, out string headFileName)
        {
            bodyFileName = string.IsNullOrWhiteSpace(bodyHint) ? null : bodyHint;
            headFileName = string.IsNullOrWhiteSpace(headHint) ? null : headHint;

            if (!Directory.Exists(folderOnDisk))
                return false;

            if (bodyFileName != null && headFileName != null)
                return File.Exists(Path.Combine(folderOnDisk, bodyFileName)) &&
                       File.Exists(Path.Combine(folderOnDisk, headFileName));

            foreach (var file in Directory.GetFiles(folderOnDisk, "*.fbx", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith("_Body.fbx", System.StringComparison.OrdinalIgnoreCase))
                    bodyFileName = name;
                else if (name.EndsWith("_Head.fbx", System.StringComparison.OrdinalIgnoreCase))
                    headFileName = name;
            }

            return bodyFileName != null && headFileName != null;
        }

        static bool ValidateMyGoSourceFiles(string projectFolderAsset, string bodyFileName, string headFileName,
            string bodyAsset, string headAsset, out string missingReport)
        {
            var lines = new System.Collections.Generic.List<string>();
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            var folder = Path.Combine(projectRoot,
                projectFolderAsset.Replace('/', Path.DirectorySeparatorChar));

            void Check(string fileName, string assetPath)
            {
                var full = Path.Combine(folder, fileName);
                if (!File.Exists(full))
                    lines.Add("缺少: " + fileName);
                else if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                    lines.Add("磁盘有文件但 Unity 未导入: " + fileName + "（请 Reimport）");
            }

            Check(bodyFileName, bodyAsset);
            Check(headFileName, headAsset);

            missingReport = lines.Count == 0 ? string.Empty : string.Join("\n", lines);
            return lines.Count == 0;
        }

        /// <summary>从 FBX 重建老师视觉预制体（供材质修复等 Editor 流程调用，无菜单项）。</summary>
        public static bool BindCharacterAsset(GameObject source, bool headTeacher, string prefabName, string assetPath,
            bool showDialog = true)
        {
            if (source == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("FleeNightStudy", "找不到角色模型资源。", "确定");
                return false;
            }

            EnsureFolders();
            SaveVisualPrefab(WrapImportedModel(source, prefabName, headTeacher), assetPath);
            CopyToResources(assetPath, $"{ResourcesTeachersFolder}/{prefabName}.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
                Debug.Log($"[FleeNightStudy] 已重建老师视觉: {prefabName} <- {source.name}");
            return true;
        }

        /// <summary>场景一键设置在无 MyGO 模型时生成占位老师。</summary>
        public static void CreateAnimeStyleTeacherVisuals()
        {
            EnsureFolders();

            SaveVisualPrefab(BuildAnimeStyleCharacter("PatrolTeacherVisual", false),
                $"{TeachersFolder}/PatrolTeacherVisual.prefab");
            SaveVisualPrefab(BuildAnimeStyleCharacter("HeadTeacherVisual", true),
                $"{TeachersFolder}/HeadTeacherVisual.prefab");

            CopyToResources($"{TeachersFolder}/PatrolTeacherVisual.prefab",
                $"{ResourcesTeachersFolder}/PatrolTeacherVisual.prefab");
            CopyToResources($"{TeachersFolder}/HeadTeacherVisual.prefab",
                $"{ResourcesTeachersFolder}/HeadTeacherVisual.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EnsureSourceFolders()
        {
            if (!AssetDatabase.IsValidFolder($"{CharactersFolder}/Source"))
                AssetDatabase.CreateFolder(CharactersFolder, "Source");
            EnsureCharacterSourceFolder(TeacherNpcModelRegistry.AnonSourceFolder, "Anon");
            EnsureCharacterSourceFolder(TeacherNpcModelRegistry.TomoriSourceFolder, "Tomori");
            EnsureCharacterSourceFolder(TeacherNpcModelRegistry.RaanaSourceFolder, "Raana");
            EnsureCharacterSourceFolder(TeacherNpcModelRegistry.SoyoSourceFolder, "Soyo");
        }

        static void EnsureCharacterSourceFolder(string sourceFolder, string characterName)
        {
            if (!AssetDatabase.IsValidFolder(sourceFolder))
                AssetDatabase.CreateFolder($"{CharactersFolder}/Source", characterName);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(CharactersFolder))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy", "Characters");
            if (!AssetDatabase.IsValidFolder(TeachersFolder))
                AssetDatabase.CreateFolder(CharactersFolder, "Teachers");
            if (!AssetDatabase.IsValidFolder("Assets/FleeNightStudy/Resources/FleeNightStudy"))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy/Resources", "FleeNightStudy");
            if (!AssetDatabase.IsValidFolder(ResourcesTeachersFolder))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy/Resources/FleeNightStudy", "Teachers");
            EnsureSourceFolders();
        }

        static GameObject BuildAnimeStyleCharacter(string name, bool headTeacher)
        {
            var root = new GameObject(name);
            var visual = root.AddComponent<TeacherNpcVisual>();

            var bodyColor = headTeacher ? new Color(0.45f, 0.12f, 0.14f) : new Color(0.2f, 0.24f, 0.42f);
            var skin = new Color(1f, 0.86f, 0.78f);
            var hair = new Color(0.12f, 0.1f, 0.14f);

            CreatePart(root.transform, "HairBack", PrimitiveType.Sphere,
                new Vector3(0f, 1.52f, -0.06f), new Vector3(0.52f, 0.48f, 0.5f), hair);
            CreatePart(root.transform, "Head", PrimitiveType.Sphere,
                new Vector3(0f, 1.38f, 0f), new Vector3(0.42f, 0.4f, 0.4f), skin);
            CreatePart(root.transform, "HairFront", PrimitiveType.Cube,
                new Vector3(0f, 1.58f, 0.1f), new Vector3(0.44f, 0.14f, 0.2f), hair);
            CreatePart(root.transform, "Body", PrimitiveType.Capsule,
                new Vector3(0f, 0.82f, 0f), new Vector3(0.38f, 0.5f, 0.28f), bodyColor);
            CreatePart(root.transform, "Skirt", PrimitiveType.Cylinder,
                new Vector3(0f, 0.42f, 0f), new Vector3(0.42f, 0.22f, 0.42f), bodyColor * 0.9f);
            CreatePart(root.transform, "LegL", PrimitiveType.Capsule,
                new Vector3(-0.12f, 0.18f, 0f), new Vector3(0.14f, 0.28f, 0.14f), bodyColor * 0.85f);
            CreatePart(root.transform, "LegR", PrimitiveType.Capsule,
                new Vector3(0.12f, 0.18f, 0f), new Vector3(0.14f, 0.28f, 0.14f), bodyColor * 0.85f);

            visual.ApplyRole(headTeacher);
            return root;
        }

        static GameObject WrapImportedModel(GameObject source, string name, bool headTeacher)
        {
            return WrapImportedModelParts(source, null, name, headTeacher,
                headTeacher ? TeacherNpcModelRegistry.SoyoSourceFolder : TeacherNpcModelRegistry.AnonSourceFolder + "/textures");
        }

        static GameObject WrapImportedModelParts(GameObject body, GameObject head, string name, bool headTeacher,
            string texturesFolder)
        {
            var root = new GameObject(name);
            root.AddComponent<TeacherNpcVisual>();

            var modelRoot = new GameObject("Model");
            modelRoot.transform.SetParent(root.transform, false);
            modelRoot.transform.localPosition = Vector3.zero;
            modelRoot.transform.localRotation = Quaternion.identity;
            modelRoot.transform.localScale = Vector3.one;

            GameObject bodyInstance = null;
            if (body != null)
                bodyInstance = AttachModelPart(modelRoot.transform, body, "Body");

            // MyGO 爱音：Head.fbx 是头发/脸部配件，必须挂到 Body 的 Head 骨骼上，不能当第二套骨骼并排动画
            if (bodyInstance != null && head != null)
                MergeHeadAccessoryOntoBody(bodyInstance, head);
            else if (head != null)
                AttachModelPart(modelRoot.transform, head, "Head");

            const float anonTargetHeight = 1.72f;
            float fittedScale = FitModelToHumanHeight(modelRoot.transform, anonTargetHeight);
            var visual = root.GetComponent<TeacherNpcVisual>();
            visual.SetModelHeightScale(fittedScale);
            visual.ApplyRole(headTeacher);

            foreach (var col in root.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);
            TeacherMaterialFixup.FixHierarchyMaterials(root, texturesFolder);
            TeacherLocomotionSetup.ApplyLocomotionToVisualRoot(root, headTeacher);

            if (head != null)
            {
                var binder = root.GetComponent<TeacherAnonHeadBinder>();
                if (binder == null)
                    binder = root.AddComponent<TeacherAnonHeadBinder>();
                binder.ResolveBones();
            }

            return root;
        }

        static GameObject AttachModelPart(Transform parent, GameObject source, string partName)
        {
            // 合并头身时要改层级，必须用 Instantiate（不能用 PrefabUtility.InstantiatePrefab）
            var model = Object.Instantiate(source);

            model.name = partName;
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            return model;
        }

        static void MergeHeadAccessoryOntoBody(GameObject bodyInstance, GameObject headSource)
        {
            var bodyHead = FindNeckHeadBone(bodyInstance.transform);
            if (bodyHead == null)
            {
                Debug.LogWarning("[FleeNightStudy] 未找到身体 Neck→Head 骨骼，跳过头部合并。");
                return;
            }

            var headInstance = Object.Instantiate(headSource);
            var meteor = headInstance.transform.Find("meteor_CH");
            var meshNode = headInstance.transform.Find("mesh");

            if (meteor != null)
            {
                TeacherAnonHeadBinder.AlignMeteorToBodyHead(meteor, bodyHead);
                meteor.SetParent(bodyHead, false);
                meteor.localScale = Vector3.one;
            }

            if (meshNode != null)
            {
                meshNode.SetParent(bodyHead, false);
                meshNode.localPosition = Vector3.zero;
                meshNode.localRotation = Quaternion.identity;
                meshNode.localScale = Vector3.one;
            }

            foreach (var anim in headInstance.GetComponentsInChildren<Animator>(true))
            {
                anim.runtimeAnimatorController = null;
                anim.enabled = false;
            }

            Object.DestroyImmediate(headInstance);

            Debug.Log("[FleeNightStudy] 爱音头部 meteor_CH 已对齐并挂到身体 Head 骨骼。");
        }

        static Transform FindNeckHeadBone(Transform bodyRoot)
        {
            Transform fallback = null;
            foreach (var t in bodyRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Head")
                    continue;

                if (t.parent != null && t.parent.name == "Neck")
                    return t;

                if (fallback == null)
                    fallback = t;
            }

            return fallback;
        }

        [MenuItem("FleeNightStudy/老师角色/换回彩色占位老师（调试）", false, 20)]
        public static void UseColoredPlaceholderTeachers()
        {
            CreateAnimeStyleTeacherVisuals();

            CopyToResources($"{TeachersFolder}/PatrolTeacherVisual.prefab",
                $"{ResourcesTeachersFolder}/Anon_Patrol.prefab");
            CopyToResources($"{TeachersFolder}/HeadTeacherVisual.prefab",
                $"{ResourcesTeachersFolder}/Soyo_HeadTeacher.prefab");

            AssetDatabase.CopyAsset($"{TeachersFolder}/PatrolTeacherVisual.prefab",
                TeacherNpcModelRegistry.AnonPatrolPrefabAsset);
            AssetDatabase.CopyAsset($"{TeachersFolder}/HeadTeacherVisual.prefab",
                TeacherNpcModelRegistry.SoyoHeadTeacherPrefabAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "已换回彩色几何体占位老师：\n" +
                "• 巡查：蓝紫色\n" +
                "• 班主任：红紫色\n\n" +
                "若要换回 MyGO 模型：菜单 FleeNightStudy → 老师角色 → 一键导入全部 MyGO 老师。",
                "确定");
        }

        static float FitModelToHumanHeight(Transform model, float targetHeight)
        {
            float height = MeasureRenderersBoundsHeight(model);
            if (height < 0.001f)
            {
                // MyGO/MMD 导出常见约 0.02m，直接给经验倍率
                float fallback = targetHeight / 0.02f;
                model.localScale = Vector3.one * fallback;
                AlignModelFeetToOrigin(model);
                return fallback;
            }

            float scale = Mathf.Clamp(targetHeight / height, 0.05f, 200f);
            model.localScale = Vector3.one * scale;
            AlignModelFeetToOrigin(model);
            return scale;
        }

        static void AlignModelFeetToOrigin(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float worldLift = bounds.min.y - model.position.y;
            if (Mathf.Abs(worldLift) < 0.0001f)
                return;

            // worldLift = 脚底.y - 枢轴.y；沿 Y 加上 worldLift 让脚底落到父节点高度
            var parent = model.parent;
            if (parent != null)
                model.localPosition += parent.InverseTransformDirection(new Vector3(0f, worldLift, 0f));
            else
                model.position += new Vector3(0f, worldLift, 0f);
        }

        static float MeasureRenderersBoundsHeight(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return 0f;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.size.y;
        }

        static void CreatePart(Transform parent, string partName, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = partName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateColorMaterial(color);
        }

        static Material CreateColorMaterial(Color color)
        {
            var shader = Shader.Find("HDRP/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        public static void SaveVisualPrefabPublic(GameObject root, string assetPath) => SaveVisualPrefab(root, assetPath);

        static void SaveVisualPrefab(GameObject root, string assetPath)
        {
            UnpackNestedPrefabInstances(root);
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);
        }

        static void UnpackNestedPrefabInstances(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                var go = transforms[i].gameObject;
                if (go == root)
                    continue;
                if (PrefabUtility.IsPartOfAnyPrefab(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }

        static void CopyToResources(string sourcePath, string destPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destPath) != null)
                AssetDatabase.DeleteAsset(destPath);

            if (!AssetDatabase.CopyAsset(sourcePath, destPath))
                Debug.LogWarning($"[FleeNightStudy] 复制到 Resources 失败: {destPath}");
        }
    }
}
#endif
