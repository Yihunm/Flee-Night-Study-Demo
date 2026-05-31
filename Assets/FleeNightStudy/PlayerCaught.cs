using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 被老师碰到即失败。除碰撞回调外，每帧检测与老师的距离（玩家站着不动、老师顶上来时也能判定）。
    /// </summary>
    public class PlayerCaught : MonoBehaviour
    {
        [SerializeField] string teacherTag = "Teacher";
        [Tooltip("在自动半径基础上额外放大的抓捕距离（米）。")]
        [SerializeField] float catchRadiusPadding = 0.12f;

        CharacterController _controller;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        void OnControllerColliderHit(ControllerColliderHit hit) => TryCatch(hit.collider);

        void OnCollisionEnter(Collision collision) => TryCatch(collision.collider);

        void OnTriggerEnter(Collider other) => TryCatch(other);

        void LateUpdate()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;

            var stealth = GetComponent<PlayerStealth>();
            if (stealth != null && stealth.IsStealthActive)
                return;

            CheckProximityToTeachers();
        }

        void CheckProximityToTeachers()
        {
            var teachers = GameObject.FindGameObjectsWithTag(teacherTag);
            if (teachers == null || teachers.Length == 0)
                return;

            float playerRadius = _controller != null ? _controller.radius : 0.35f;
            var playerFlat = new Vector3(transform.position.x, 0f, transform.position.z);

            foreach (var teacherGo in teachers)
            {
                if (teacherGo == null || !teacherGo.activeInHierarchy)
                    continue;

                var teacher = teacherGo.GetComponent<TeacherController>();
                if (teacher != null && !teacher.CanCapturePlayer)
                    continue;

                float teacherRadius = 0.32f;
                var cap = teacherGo.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    float scale = Mathf.Max(teacherGo.transform.lossyScale.x, teacherGo.transform.lossyScale.z);
                    teacherRadius = cap.radius * scale;
                }

                float catchDist = playerRadius + teacherRadius + catchRadiusPadding;
                var teacherFlat = new Vector3(teacherGo.transform.position.x, 0f, teacherGo.transform.position.z);
                if (Vector3.Distance(playerFlat, teacherFlat) <= catchDist)
                {
                    GameStateManager.Instance?.TriggerGameOver();
                    return;
                }
            }
        }

        void TryCatch(Collider col)
        {
            if (col == null || !col.CompareTag(teacherTag))
                return;

            var teacher = col.GetComponentInParent<TeacherController>();
            if (teacher != null && !teacher.CanCapturePlayer)
                return;

            var stealth = GetComponent<PlayerStealth>();
            if (stealth != null && stealth.IsStealthActive)
                return;

            GameStateManager.Instance?.TriggerGameOver();
        }
    }
}
