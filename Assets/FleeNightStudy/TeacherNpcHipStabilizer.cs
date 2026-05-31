using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// MMD 人形 + Mixamo 走路时，髋部常有左右摆动；将 Hips 的 XZ 锁在角色中轴上，只保留上下起伏。
    /// </summary>
    [DisallowMultipleComponent]
    public class TeacherNpcHipStabilizer : MonoBehaviour
    {
        [SerializeField] bool stabilizeHips = true;

        Animator _animator;
        Transform _anchor;

        void Awake()
        {
            var body = transform.Find("Model/Body");
            _animator = body != null ? body.GetComponent<Animator>() : null;
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            _anchor = transform.parent != null ? transform.parent : transform;
        }

        void LateUpdate()
        {
            if (!stabilizeHips || _animator == null || _anchor == null)
                return;

            var hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            var local = _anchor.InverseTransformPoint(hips.position);
            hips.position = _anchor.TransformPoint(new Vector3(0f, local.y, 0f));
        }
    }
}
