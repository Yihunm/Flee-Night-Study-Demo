#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FleeNightStudy.EditorTools
{
    /// <summary>
    /// 为人形老师配置 Humanoid 骨骼、Animator、走路控制器；支持 Mixamo 动画 FBX。
    /// </summary>
    public static class TeacherLocomotionSetup
    {
        const string AnimationsFolder = "Assets/FleeNightStudy/Animations";
        const string MixamoFolder = "Assets/FleeNightStudy/Animations/Mixamo";
        const string ControllerPath = "Assets/FleeNightStudy/Animations/TeacherLocomotion.controller";
        const string ResourcesAnimFolder = "Assets/FleeNightStudy/Resources/FleeNightStudy/Animations";

        const string IdleFile = "Idle.fbx";
        const string WalkFile = "Walking.fbx";
        const string RunFile = "Running.fbx";

        const string DesktopIdle = @"C:\Users\lenovo\Desktop\X Bot@Idle.fbx";
        const string DesktopWalk = @"C:\Users\lenovo\Desktop\X Bot@Walking.fbx";
        const string DesktopRun = @"C:\Users\lenovo\Desktop\X Bot@Running.fbx";

        [MenuItem("FleeNightStudy/老师角色/导入 Mixamo 走路动画（桌面 X Bot）", false, 10)]
        public static void ImportDesktopMixamoAnimations()
        {
            EnsureFolders();
            CopyDesktopMixamoFiles();
            AssetDatabase.Refresh();

            ConfigureMixamoAnimationImports();
            ConfigureTeacherModelsAsHumanoid();

            if (!TrySetupLocomotionSilent())
            {
                EditorUtility.DisplayDialog(
                    "FleeNightStudy",
                    "动画文件已复制，但未能生成控制器。\n请查看 Console 是否 Rig/Avatar 报错。",
                    "确定");
                return;
            }

            EditorUtility.DisplayDialog(
                "FleeNightStudy",
                "Mixamo 走路动画已导入并绑定到爱音 / 素世。\n请 Play 测试老师移动时的摆臂迈腿。",
                "确定");
        }

        [MenuItem("FleeNightStudy/老师角色/修复走路动画与 Avatar", false, 11)]
        public static void FixLocomotionLoopAndAvatar()
        {
            ConfigureMixamoAnimationImports();
            ConfigureTeacherModelsAsHumanoid();
            TrySetupLocomotionSilent();
            EditorUtility.DisplayDialog("FleeNightStudy", "已修复动画循环并重新绑定 Avatar。\n请 Play 测试。", "确定");
        }

        /// <summary>供一键导入调用，无弹窗。</summary>
        public static bool TrySetupLocomotionSilent()
        {
            EnsureFolders();
            ConfigureMixamoAnimationImports();
            ConfigureTeacherModelsAsHumanoid();
            var controller = BuildOrUpdateController();
            if (controller == null)
                return false;

            CopyControllerToResources(controller);
            RebindExistingTeacherVisuals();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }


        public static void ApplyLocomotionToVisualRoot(GameObject visualRoot, bool headTeacher)
        {
            if (visualRoot == null)
                return;

            EnsureFolders();
            var animator = visualRoot.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = FindBestAnimatorTarget(visualRoot);

            if (animator == null)
                return;

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                             ?? Resources.Load<RuntimeAnimatorController>("FleeNightStudy/Animations/TeacherLocomotion")
                                 as AnimatorController;

            if (controller != null)
            {
                animator.applyRootMotion = false;
                animator.runtimeAnimatorController = controller;
            }

            AssignModelAvatar(animator, headTeacher);

            var locomotion = visualRoot.GetComponent<TeacherNpcLocomotion>();
            if (locomotion == null)
                locomotion = visualRoot.AddComponent<TeacherNpcLocomotion>();

            locomotion.BindAnimator(animator);
            DisableExtraAnimators(visualRoot, animator);

            if (visualRoot.GetComponent<TeacherNpcHipStabilizer>() == null)
                visualRoot.AddComponent<TeacherNpcHipStabilizer>();
        }

        static void DisableExtraAnimators(GameObject visualRoot, Animator active)
        {
            foreach (var anim in visualRoot.GetComponentsInChildren<Animator>(true))
            {
                if (anim == null || anim == active)
                    continue;

                anim.runtimeAnimatorController = null;
                anim.enabled = false;
            }
        }

        public static void ConfigureTeacherModelsAsHumanoid()
        {
            SetHumanoidIfExists(TeacherNpcModelRegistry.AnonBodyFbxAsset);
            SetHumanoidIfExists(TeacherNpcModelRegistry.AnonHeadFbxAsset);
            SetHumanoidIfExists(TeacherNpcModelRegistry.SoyoBodyFbxAsset);
            SetHumanoidIfExists(TeacherNpcModelRegistry.SoyoHeadFbxAsset);
            ConfigureHumanoidForMyGoFolder(TeacherNpcModelRegistry.TomoriMyGoSourceFolder);
            ConfigureHumanoidForMyGoFolder(TeacherNpcModelRegistry.RaanaMyGoSourceFolder);
        }

        static void SetHumanoidIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                SetHumanoid(assetPath);
        }

        static void ConfigureHumanoidForMyGoFolder(string folderAsset)
        {
            if (!AssetDatabase.IsValidFolder(folderAsset))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folderAsset }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("_Body.fbx", System.StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("_Head.fbx", System.StringComparison.OrdinalIgnoreCase))
                    SetHumanoid(path);
            }
        }

        static void RebindExistingTeacherVisuals()
        {
            RebindPrefab(TeacherNpcModelRegistry.RaanaPatrolPrefabAsset);
            RebindPrefab(TeacherNpcModelRegistry.TomoriPatrolPrefabAsset);
            RebindPrefab(TeacherNpcModelRegistry.AnonPatrolPrefabAsset);
            RebindPrefab(TeacherNpcModelRegistry.SoyoHeadTeacherPrefabAsset);
            RebindResourcesPrefab("Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/Raana_Patrol.prefab");
            RebindResourcesPrefab("Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/Tomori_Patrol.prefab");
            RebindResourcesPrefab("Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/Anon_Patrol.prefab");
            RebindResourcesPrefab("Assets/FleeNightStudy/Resources/FleeNightStudy/Teachers/Soyo_HeadTeacher.prefab");
        }

        static void RebindPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return;

            bool head = path.Contains("Head") || path.Contains("Soyo");
            ApplyLocomotionToVisualRoot(instance, head);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void RebindResourcesPrefab(string path) => RebindPrefab(path);

        static void CopyDesktopMixamoFiles()
        {
            CopyDesktopFile(DesktopIdle, IdleFile);
            CopyDesktopFile(DesktopWalk, WalkFile);
            CopyDesktopFile(DesktopRun, RunFile);
        }

        static void CopyDesktopFile(string sourceFullPath, string destFileName)
        {
            if (!File.Exists(sourceFullPath))
            {
                Debug.LogWarning($"[FleeNightStudy] 未找到: {sourceFullPath}");
                return;
            }

            var dest = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                MixamoFolder,
                destFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? string.Empty);
            File.Copy(sourceFullPath, dest, true);
            Debug.Log($"[FleeNightStudy] 已复制 Mixamo: {destFileName}");
        }

        public static void ConfigureMixamoAnimationImports()
        {
            var idlePath = MixamoAssetPath(IdleFile);
            var walkPath = MixamoAssetPath(WalkFile);
            var runPath = MixamoAssetPath(RunFile);

            SetMixamoClipImporter(idlePath, null, defineAvatar: true);

            var avatar = FindAvatar(idlePath)
                         ?? FindAvatar(TeacherNpcModelRegistry.AnonBodyFbxAsset);
            if (avatar == null)
            {
                Debug.LogWarning("[FleeNightStudy] 未找到 Humanoid Avatar，动画可能无法重定向到爱音/素世。");
                return;
            }

            SetMixamoClipImporter(walkPath, avatar, defineAvatar: false);
            SetMixamoClipImporter(runPath, avatar, defineAvatar: false);
        }

        static void SetMixamoClipImporter(string assetPath, Avatar sourceAvatar, bool defineAvatar)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            if (defineAvatar)
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            else
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                clips[i].lockRootRotation = true;
                clips[i].keepOriginalOrientation = true;
                clips[i].lockRootHeightY = true;
                clips[i].keepOriginalPositionY = true;
                clips[i].lockRootPositionXZ = true;
                clips[i].keepOriginalPositionXZ = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static void AssignModelAvatar(Animator animator, bool headTeacher)
        {
            if (animator == null)
                return;

            string modelPath = headTeacher ? HeadModelAssetPath() : PatrolModelAssetPath();

            var avatar = FindAvatar(modelPath);
            if (avatar != null)
                animator.avatar = avatar;
        }

        static string PatrolModelAssetPath()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TeacherNpcModelRegistry.AnonBodyFbxAsset) != null)
                return TeacherNpcModelRegistry.AnonBodyFbxAsset;
            return TeacherNpcModelRegistry.AnonBodyFbxAsset;
        }

        static string HeadModelAssetPath() => TeacherNpcModelRegistry.SoyoBodyFbxAsset;

        static Avatar FindAvatar(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Avatar avatar)
                    return avatar;
            }

            return null;
        }

        static string MixamoAssetPath(string fileName) => $"{MixamoFolder}/{fileName}";

        static AnimatorController BuildOrUpdateController()
        {
            var idle = LoadClip(MixamoAssetPath(IdleFile));
            var walk = LoadClip(MixamoAssetPath(WalkFile));
            var run = LoadClip(MixamoAssetPath(RunFile));

            if (idle == null && walk == null)
                return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var state = sm.AddState("Locomotion", new Vector3(300, 0, 0));
            sm.defaultState = state;

            var tree = new BlendTree
            {
                name = "LocomotionBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            if (idle != null)
                tree.AddChild(idle, 0f);
            if (walk != null)
                tree.AddChild(walk, 0.4f);
            if (run != null)
                tree.AddChild(run, 1f);
            else if (walk != null)
                tree.AddChild(walk, 1f);

            AssetDatabase.AddObjectToAsset(tree, controller);
            state.motion = tree;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimationClip LoadClip(string assetPath)
        {
            assetPath = assetPath?.Replace('\\', '/');
            if (string.IsNullOrEmpty(assetPath))
                return null;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }

        static Animator FindBestAnimatorTarget(GameObject visualRoot)
        {
            Animator best = null;
            var bestScore = 0;

            foreach (var smr in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var t = smr.transform;
                var anim = t.GetComponent<Animator>();
                if (anim == null)
                    anim = t.gameObject.AddComponent<Animator>();

                int score = smr.sharedMesh != null ? smr.sharedMesh.vertexCount : 0;
                if (score >= bestScore)
                {
                    bestScore = score;
                    best = anim;
                }
            }

            if (best != null)
                return best;

            var rootAnim = visualRoot.GetComponent<Animator>();
            return rootAnim != null ? rootAnim : visualRoot.AddComponent<Animator>();
        }

        public static void SetHumanoid(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
        }

        static void CopyControllerToResources(AnimatorController controller)
        {
            if (controller == null)
                return;

            var dest = $"{ResourcesAnimFolder}/TeacherLocomotion.controller";
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(dest) != null)
                AssetDatabase.DeleteAsset(dest);
            AssetDatabase.CopyAsset(ControllerPath, dest);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FleeNightStudy/Animations"))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy", "Animations");
            if (!AssetDatabase.IsValidFolder(MixamoFolder))
                AssetDatabase.CreateFolder(AnimationsFolder, "Mixamo");
            if (!AssetDatabase.IsValidFolder("Assets/FleeNightStudy/Resources/FleeNightStudy"))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy/Resources", "FleeNightStudy");
            if (!AssetDatabase.IsValidFolder(ResourcesAnimFolder))
                AssetDatabase.CreateFolder("Assets/FleeNightStudy/Resources/FleeNightStudy", "Animations");
        }
    }
}
#endif
