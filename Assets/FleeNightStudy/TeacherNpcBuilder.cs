using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    /// <summary>用动漫角色视觉预制体 + NavMesh 逻辑组装老师 NPC（替代胶囊体）。</summary>
    public static class TeacherNpcBuilder
    {

        public static TeacherController Spawn(
            string npcName,
            bool headTeacher,
            Vector3 position,
            Transform parent,
            Transform player,
            int routeIndex,
            Transform patrolRoot)
        {
            var root = new GameObject(npcName);
            if (parent != null)
                root.transform.SetParent(parent, false);

            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;
            try { root.tag = "Teacher"; } catch { }

            var body = root.AddComponent<CapsuleCollider>();
            body.height = 1.85f;
            body.radius = 0.32f;
            body.center = new Vector3(0f, 0.92f, 0f);
            body.isTrigger = false;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = 1.85f;
            agent.radius = 0.32f;
            agent.baseOffset = 0f;
            agent.speed = headTeacher ? 5f : 3.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.35f;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            var controller = root.AddComponent<TeacherController>();
            controller.SetHeadTeacher(headTeacher);
            if (player != null)
                controller.SetPlayer(player);

            if (!headTeacher && patrolRoot != null)
                TeacherPatrolSetup.AssignPatrolRoute(controller, routeIndex, patrolRoot);
            else if (!headTeacher)
                controller.SetPatrolRouteIndex(routeIndex);

            AttachVisual(root.transform, headTeacher, headTeacher ? -1 : routeIndex);

            return controller;
        }

        /// <summary>把场景中旧的胶囊体老师换成动漫视觉模型。</summary>
        public static void UpgradeLegacyCapsuleTeacher(TeacherController controller)
        {
            if (controller == null || controller.transform.Find("Model") != null)
                return;

            var meshFilter = controller.GetComponent<MeshFilter>();
            if (meshFilter == null)
                return;

            Object.Destroy(meshFilter);
            Object.Destroy(controller.GetComponent<MeshRenderer>());

            var agent = controller.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.height = 1.85f;
                agent.radius = 0.32f;
                agent.baseOffset = 0f;
            }

            AttachVisual(controller.transform, controller.IsHeadTeacher, controller.PatrolRouteIndex);
        }

        static void AttachVisual(Transform root, bool headTeacher, int patrolRouteIndex)
        {
            var prefab = LoadVisualPrefab(headTeacher, patrolRouteIndex);
            if (prefab == null)
            {
                CreateFallbackCapsuleVisual(root, headTeacher);
                return;
            }

            var visual = Object.Instantiate(prefab, root);
            visual.name = "Model";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            var visualComponent = visual.GetComponent<TeacherNpcVisual>();
            if (visualComponent == null)
                visualComponent = visual.AddComponent<TeacherNpcVisual>();

            var locomotion = visual.GetComponent<TeacherNpcLocomotion>();
            if (locomotion == null)
                locomotion = visual.AddComponent<TeacherNpcLocomotion>();

            visualComponent.ApplyRole(headTeacher);
            if (Application.isPlaying)
                visualComponent.AlignFeetToGround();
            ConfigureAgentRotationForLocomotion(root.GetComponent<NavMeshAgent>(), locomotion);

            var headBinder = visual.GetComponent<TeacherAnonHeadBinder>();
            if (headBinder == null)
                headBinder = visual.GetComponentInChildren<TeacherAnonHeadBinder>(true);
            headBinder?.ResolveBones();

            foreach (var col in visual.GetComponentsInChildren<Collider>())
                Object.Destroy(col);
        }

        static void ConfigureAgentRotationForLocomotion(NavMeshAgent agent, TeacherNpcLocomotion locomotion)
        {
            if (agent == null)
                return;

            bool hasAnimator = locomotion != null &&
                               locomotion.GetComponentInChildren<Animator>(true) != null;
            if (hasAnimator)
            {
                agent.updateRotation = true;
                agent.angularSpeed = 240f;
            }
        }

        static GameObject LoadVisualPrefab(bool headTeacher, int patrolRouteIndex)
        {
            if (headTeacher)
            {
                return Resources.Load<GameObject>(TeacherNpcModelRegistry.SoyoHeadResource)
                       ?? Resources.Load<GameObject>(TeacherNpcModelRegistry.FallbackHeadResource);
            }

            string resource = TeacherNpcModelRegistry.GetPatrolResourceForRoute(patrolRouteIndex);
            return Resources.Load<GameObject>(resource)
                   ?? Resources.Load<GameObject>(TeacherNpcModelRegistry.AnonPatrolResource)
                   ?? Resources.Load<GameObject>(TeacherNpcModelRegistry.FallbackPatrolResource);
        }

        static void CreateFallbackCapsuleVisual(Transform root, bool headTeacher)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Model";
            visual.transform.SetParent(root, false);
            visual.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            visual.transform.localScale = new Vector3(0.65f, 0.92f, 0.65f);
            Object.Destroy(visual.GetComponent<CapsuleCollider>());

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = headTeacher
                    ? new Color(0.75f, 0.15f, 0.12f)
                    : new Color(0.35f, 0.35f, 0.45f);
            }
        }
    }
}
