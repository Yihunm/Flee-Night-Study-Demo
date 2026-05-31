using UnityEngine;
using UnityEngine.SceneManagement;

namespace FleeNightStudy
{
    /// <summary>
    /// 地面第一人称：鼠标视角 + WASD（相对水平朝向）+ 重力。
    /// 用于「学校展示场景」替代飞行相机。需 <see cref="CharacterController"/>（胶囊碰撞体，非球体）。
    /// 若 Hierarchy 里用 Sphere 仅作占位外观，与绿色线框胶囊无关：可在 Inspector 调胶囊参数，或换/隐藏球体模型。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonWalker : MonoBehaviour
    {
        [Header("碰撞胶囊（Character Controller）")]
        [Tooltip("勾选后在进入场景时写入下方数值，避免默认胶囊过矮过胖像球体")]
        [SerializeField] bool applyCapsuleDimensionsOnStart = true;
        [SerializeField] float capsuleHeight = 1.85f;
        [SerializeField] float capsuleRadius = 0.28f;
        [SerializeField] Vector3 capsuleCenter = new Vector3(0f, 0.92f, 0f);

        [Header("相机")]
        [SerializeField] Transform cameraTransform;
        [SerializeField] float mouseSensitivityX = 2f;
        [SerializeField] float mouseSensitivityY = 2f;
        [SerializeField] Vector2 pitchClamp = new Vector2(-85f, 85f);

        [Header("移动")]
        [SerializeField] float walkSpeed = 3.2f;
        [SerializeField] float sprintSpeed = 5.5f;
        [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] float gravity = -25f;

        [Header("光标")]
        [SerializeField] bool lockCursorOnPlay = true;

        CharacterController _cc;
        float _pitch;
        float _yaw;
        Vector3 _velocity;

        /// <summary>能量饮料等可临时倍率。</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        void Reset()
        {
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
        }

        void Start()
        {
            _cc = GetComponent<CharacterController>();
            if (applyCapsuleDimensionsOnStart)
            {
                _cc.height = capsuleHeight;
                _cc.radius = capsuleRadius;
                _cc.center = capsuleCenter;
            }

            Vector3 e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = 0f;

            if (lockCursorOnPlay && !IsMenuScene())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            if (GameplayPauseUI.IsPaused)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;

            if (cameraTransform == null) return;

            if (lockCursorOnPlay && !IsMenuScene() && Input.GetMouseButtonDown(0) &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            float mx = Input.GetAxis("Mouse X") * mouseSensitivityX * 10f * Time.deltaTime;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivityY * 10f * Time.deltaTime;
            _yaw += mx;
            _pitch -= my;
            _pitch = Mathf.Clamp(_pitch, pitchClamp.x, pitchClamp.y);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = (transform.right * h + transform.forward * v).normalized;
            float speed = Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed;
            speed *= SpeedMultiplier;
            Vector3 move = dir * speed * Time.deltaTime;

            if (_cc.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;
            _velocity.y += gravity * Time.deltaTime;
            move += Vector3.up * _velocity.y * Time.deltaTime;

            _cc.Move(move);
        }

        static bool IsMenuScene()
        {
            var name = SceneManager.GetActiveScene().name;
            return name.Contains("MainMenu") || name.Contains("Menu");
        }
    }
}
