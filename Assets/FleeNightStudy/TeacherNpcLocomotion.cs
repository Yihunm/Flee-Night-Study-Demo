using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    /// <summary>
    /// 根据 NavMeshAgent 速度驱动 Humanoid 走路/跑步动画（Idle / Walk / Run）。
    /// </summary>
    [DisallowMultipleComponent]
    public class TeacherNpcLocomotion : MonoBehaviour
    {
        public static readonly int SpeedParam = Animator.StringToHash("Speed");
        public static readonly int IsMovingParam = Animator.StringToHash("IsMoving");

        const string ControllerResource = "FleeNightStudy/Animations/TeacherLocomotion";

        [SerializeField] float speedDamp = 10f;
        [SerializeField] float animSpeedDamp = 8f;
        [SerializeField] float movingThreshold = 0.08f;
        [SerializeField] float walkMetersPerSecond = 1.35f;
        [SerializeField] float runMetersPerSecond = 3.6f;

        Animator _animator;
        NavMeshAgent _agent;
        float _smoothedSpeed;
        float _smoothedAnimSpeed = 1f;

        public bool HasAnimator => _animator != null;

        void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            EnsureAnimator();
            if (_agent != null && _animator != null)
            {
                _agent.updateRotation = true;
                _agent.angularSpeed = Mathf.Max(_agent.angularSpeed, 240f);
            }
        }

        void EnsureAnimator()
        {
            if (_animator != null)
                return;

            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
                return;

            if (_animator.runtimeAnimatorController == null)
            {
                var controller = Resources.Load<RuntimeAnimatorController>(ControllerResource);
                if (controller != null)
                    _animator.runtimeAnimatorController = controller;
            }

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        void Update()
        {
            if (_animator == null || _agent == null)
                return;

            var teacher = GetComponentInParent<TeacherController>();
            if (teacher != null && !teacher.IsHeadTeacher && teacher.IsStationaryAlert)
            {
                _smoothedSpeed = 0f;
                _animator.SetFloat(SpeedParam, 0f);
                _animator.SetBool(IsMovingParam, false);
                return;
            }

            // desiredVelocity 比 velocity 更稳，减少拐弯时动画忽快忽慢
            var move = _agent.desiredVelocity;
            move.y = 0f;
            float magnitude = move.magnitude;
            if (magnitude < movingThreshold)
            {
                var vel = _agent.velocity;
                vel.y = 0f;
                magnitude = vel.magnitude;
            }

            float agentSpeed = Mathf.Max(_agent.speed, 0.01f);
            float normalized = Mathf.Clamp01(magnitude / agentSpeed);

            // 避免 Idle↔Walk 中间混合成「弯腿定格」
            float target;
            if (magnitude < movingThreshold)
                target = 0f;
            else if (normalized < 0.55f)
                target = 0.4f;
            else
                target = 1f;

            if (magnitude < movingThreshold)
            {
                _smoothedSpeed = Mathf.MoveTowards(_smoothedSpeed, 0f, speedDamp * Time.deltaTime);
                _smoothedAnimSpeed = Mathf.Lerp(_smoothedAnimSpeed, 1f, animSpeedDamp * Time.deltaTime);
            }
            else
            {
                _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, target, speedDamp * Time.deltaTime);
                float refSpeed = agentSpeed > 4f ? runMetersPerSecond : walkMetersPerSecond;
                float targetAnimSpeed = Mathf.Clamp(magnitude / Mathf.Max(refSpeed, 0.01f), 0.75f, 1.25f);
                _smoothedAnimSpeed = Mathf.Lerp(_smoothedAnimSpeed, targetAnimSpeed, animSpeedDamp * Time.deltaTime);
            }

            _animator.speed = _smoothedAnimSpeed;

            _animator.SetFloat(SpeedParam, _smoothedSpeed);
            _animator.SetBool(IsMovingParam, magnitude > movingThreshold);
        }

        public void BindAnimator(Animator animator)
        {
            _animator = animator;
            if (_animator != null)
                _animator.applyRootMotion = false;
        }
    }
}
