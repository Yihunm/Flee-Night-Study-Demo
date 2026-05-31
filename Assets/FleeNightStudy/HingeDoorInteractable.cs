using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 门绕本地轴平滑旋转开/关。将本脚本挂在「门扇」或父物体上，并指定实际旋转的 <see cref="hinge"/>（一般为带 MeshCollider 的门板）。
    /// 在本地空间于闭合姿态基础上叠加 <see cref="openEulerDegrees"/>。
    /// </summary>
    public class HingeDoorInteractable : MonoBehaviour
    {
        [Tooltip("绕其本地旋转的物体；为空则用本物体 Transform")]
        [SerializeField] Transform hinge;

        [Tooltip("相对闭合姿态的本地欧拉角（常见为 Y 轴 ±90）")]
        [SerializeField] Vector3 openEulerDegrees = new Vector3(0f, 90f, 0f);

        [SerializeField] float smoothSpeed = 6f;

        Quaternion _closedLocal;
        Quaternion _openLocal;
        bool _isOpen;
        Quaternion _targetLocal;

        void Awake()
        {
            if (hinge == null) hinge = transform;
            _closedLocal = hinge.localRotation;
            _openLocal = _closedLocal * Quaternion.Euler(openEulerDegrees);
            _targetLocal = _closedLocal;
        }

        void Update()
        {
            hinge.localRotation = Quaternion.Slerp(hinge.localRotation, _targetLocal, Time.deltaTime * smoothSpeed);
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _targetLocal = _isOpen ? _openLocal : _closedLocal;
        }

        public void SetOpen(bool open)
        {
            _isOpen = open;
            _targetLocal = _isOpen ? _openLocal : _closedLocal;
        }
    }
}
