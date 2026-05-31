using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    /// <summary>老师角色外观：缩放、配色，并按移动方向转向。</summary>
    public class TeacherNpcVisual : MonoBehaviour
    {
        [SerializeField] float patrolScale = 1f;
        [SerializeField] float headTeacherScale = 1.08f;
        /// <summary>导入时按身高算出的 Model 子节点缩放（勿在 ApplyRole 里覆盖为 1）。</summary>
        [SerializeField] float modelHeightScale = 1f;
        [Tooltip("脚底相对地面的额外下沉（米），正值=往下贴地。")]
        [SerializeField] float feetGroundOffset = 0.02f;
        [Tooltip("向下射线找可见地板；仅当命中点不高于 NavMesh 时才采用（避免打到天花板导致上浮）。")]
        [SerializeField] bool snapFeetToFloorRaycast = false;
        [SerializeField] float floorRaycastHeight = 1.5f;
        [SerializeField] float floorRaycastDistance = 4f;
        [SerializeField] Color patrolTint = new Color(0.92f, 0.92f, 0.98f);
        [SerializeField] Color headTeacherTint = new Color(1f, 0.55f, 0.5f);
        [SerializeField] bool faceMovement = true;
        [SerializeField] float turnSpeed = 10f;

        Transform _modelRoot;
        Transform _npcRoot;
        NavMeshAgent _agent;
        TeacherNpcLocomotion _locomotion;
        bool _isHeadTeacher;

        public void SetModelHeightScale(float scale) => modelHeightScale = Mathf.Max(0.01f, scale);

        public void ApplyRole(bool headTeacher)
        {
            _isHeadTeacher = headTeacher;
            _agent = GetComponentInParent<NavMeshAgent>();
            _npcRoot = _agent != null ? _agent.transform : transform.parent;
            _locomotion = GetComponent<TeacherNpcLocomotion>();

            var modelChild = transform.Find("Model");
            _modelRoot = modelChild != null ? modelChild : transform;

            float roleMul = headTeacher ? headTeacherScale : patrolScale;
            _modelRoot.localScale = Vector3.one * (modelHeightScale * roleMul);

            if (Application.isPlaying)
            {
                ApplyTint(headTeacher ? headTeacherTint : patrolTint);
                AlignFeetToGround();
            }
        }

        void Start()
        {
            if (!Application.isPlaying)
                return;

            StartCoroutine(AlignFeetAfterSkinnedMeshReady());
        }

        IEnumerator AlignFeetAfterSkinnedMeshReady()
        {
            yield return null;
            AlignFeetToGround();
            yield return new WaitForEndOfFrame();
            AlignFeetToGround();
        }

        /// <summary>把模型脚底贴到 NPC 根节点地面（或射线命中的地板）。</summary>
        public void AlignFeetToGround()
        {
            if (_modelRoot == null)
            {
                var modelChild = transform.Find("Model");
                _modelRoot = modelChild != null ? modelChild : transform;
            }

            if (_npcRoot == null)
            {
                _agent = GetComponentInParent<NavMeshAgent>();
                _npcRoot = _agent != null ? _agent.transform : transform.parent;
            }

            if (_modelRoot == null || _npcRoot == null)
                return;

            if (!TryGetFeetBounds(out Bounds bounds))
                return;

            float targetGroundY = _npcRoot.position.y;
            if (snapFeetToFloorRaycast &&
                Physics.Raycast(
                    _npcRoot.position + Vector3.up * floorRaycastHeight,
                    Vector3.down,
                    out RaycastHit hit,
                    floorRaycastHeight + floorRaycastDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore) &&
                hit.point.y <= _npcRoot.position.y + 0.2f)
            {
                targetGroundY = hit.point.y;
            }

            float dy = targetGroundY - bounds.min.y - feetGroundOffset;
            if (Mathf.Abs(dy) < 0.0005f)
                return;

            var alignParent = _modelRoot.parent != null ? _modelRoot.parent : _npcRoot;
            _modelRoot.localPosition += alignParent.InverseTransformDirection(new Vector3(0f, dy, 0f));
        }

        bool TryGetFeetBounds(out Bounds bounds)
        {
            bounds = default;
            var renderers = _modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size.sqrMagnitude > 0.0001f;
        }

        void Update()
        {
            // 有骨骼走路动画时由 NavMeshAgent 转向，不再整体拧模型
            if (_locomotion != null && _locomotion.HasAnimator)
                return;

            if (!faceMovement || _agent == null || _modelRoot == null)
                return;

            Vector3 vel = _agent.velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude < 0.04f)
                return;

            var target = Quaternion.LookRotation(vel.normalized, Vector3.up);
            _modelRoot.rotation = Quaternion.Slerp(_modelRoot.rotation, target, turnSpeed * Time.deltaTime);
        }

        void ApplyTint(Color tint)
        {
            float strength = _isHeadTeacher ? 0.12f : 0.06f;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                {
                    if (mat == null)
                        continue;

                    if (mat.HasProperty("_BaseColorMap") && mat.GetTexture("_BaseColorMap") != null)
                        continue;

                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", Color.Lerp(c, tint, strength));
                        continue;
                    }

                    if (mat.HasProperty("_Color"))
                        mat.color = Color.Lerp(mat.color, tint, strength);
                }
            }
        }
    }
}
