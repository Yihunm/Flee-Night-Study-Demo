using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// MyGO 分体头（Head.fbx，爱音/素世等）的 meteor_CH 骨架需与身体 Head 骨骼对齐，否则头会留在原地。
    /// </summary>
    [DisallowMultipleComponent]
    public class TeacherAnonHeadBinder : MonoBehaviour
    {
        [SerializeField] bool bindInLateUpdate = true;

        Transform _bodyHead;
        Transform _headMeteor;

        void Awake() => ResolveBones();

        void LateUpdate()
        {
            if (!bindInLateUpdate)
                return;

            if (_bodyHead == null || _headMeteor == null)
                ResolveBones();

            if (_bodyHead == null || _headMeteor == null)
                return;

            AlignMeteorToBodyHead(_headMeteor, _bodyHead);
        }

        public void ResolveBones()
        {
            _bodyHead = FindNeckHeadBone(transform);
            if (_bodyHead == null)
                return;

            _headMeteor = _bodyHead.Find("meteor_CH");
            if (_headMeteor != null)
                return;

            var legacy = FindDeepChild(transform, "HeadMeshes");
            _headMeteor = legacy != null ? legacy.Find("meteor_CH") : null;
        }

        public static void AlignMeteorToBodyHead(Transform meteor, Transform bodyHead)
        {
            if (meteor == null || bodyHead == null)
                return;

            var internalHead = FindBoneInSubtree(meteor, "Head");
            if (internalHead == null)
            {
                meteor.SetPositionAndRotation(bodyHead.position, bodyHead.rotation);
                return;
            }

            var posDelta = bodyHead.position - internalHead.position;
            meteor.position += posDelta;

            var rotDelta = bodyHead.rotation * Quaternion.Inverse(internalHead.rotation);
            meteor.rotation = rotDelta * meteor.rotation;
        }

        static Transform FindNeckHeadBone(Transform root)
        {
            Transform fallback = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
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

        static Transform FindBoneInSubtree(Transform root, string boneName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == boneName)
                    return t;
            }

            return null;
        }

        static Transform FindDeepChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }

            return null;
        }
    }
}
