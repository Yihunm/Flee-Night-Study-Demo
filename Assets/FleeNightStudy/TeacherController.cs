using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class TeacherController : MonoBehaviour
    {
        const float NavMeshSampleRadius = 8f;

        [SerializeField] Transform player;
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] float viewDistance = 12f;
        [Header("视野锥形（巡查老师）")]
        [Tooltip("双眼朝前直视的半角（度），左右各一侧。")]
        [SerializeField] float binocularHalfAngle = 22f;
        [Tooltip("在直视范围外侧每侧再加宽（度），略宽于双眼直线视场。")]
        [SerializeField] float viewConeExtraHalfAngle = 10f;
        [SerializeField] LayerMask obstacleMask;
        [SerializeField] float chaseLoseDistance = 18f;
        [Tooltip("玩家与本层巡逻高度差在此范围内才追击（米）。")]
        [SerializeField] float maxChaseFloorHeightDelta = 3.5f;
        [Tooltip("巡查老师开始追击后，多久才允许贴身判失败（秒）。")]
        [SerializeField] float patrolCatchDelayAfterChaseStart = 0.35f;
        [SerializeField] bool isHeadTeacher;
        [SerializeField] float headTeacherSpeed = 5f;
        [SerializeField] float headTeacherRepathInterval = 0.25f;
        [Header("班主任：玩家不可达时随机游走")]
        [SerializeField] float headTeacherWanderRadius = 22f;
        [SerializeField] float headTeacherWanderPickInterval = 2.5f;

        NavMeshAgent _agent;
        int _patrolIndex;
        bool _chasing;
        bool _stunned;
        bool _blinded;
        bool _luring;
        Vector3 _lureTarget;
        float _lureUntil;
        Coroutine _stunCoroutine;
        Coroutine _blindCoroutine;
        Coroutine _patrolStartCoroutine;
        bool _navMeshWarningLogged;
        float _nextRepathTime;
        int _homeFloorIndex = -1;
        bool _hasHomeFloorY;
        float _homeFloorY;
        bool _stationaryAlert;
        float _patrolChaseStartTime = -1f;
        float _nextChaseRepathTime;
        Vector3 _lastChaseSamplePos;
        float _nextHeadTeacherWanderPickTime;
        bool _headTeacherWandering;
        [SerializeField] int patrolRouteIndex;

        public bool IsHeadTeacher => isHeadTeacher;

        /// <summary>对应 <see cref="TeacherPatrolConfig.Routes"/> 索引（0=三楼 …）。</summary>
        public int PatrolRouteIndex => patrolRouteIndex;

        /// <summary>巡查老师是否处于追击。</summary>
        public bool IsPatrolChasing => !isHeadTeacher && _chasing;

        /// <summary>看得见但无法追击（不同层 / 隔墙隔窗），站定警戒。</summary>
        public bool IsStationaryAlert => !isHeadTeacher && _stationaryAlert;

        /// <summary>是否允许判定玩家被抓（贴脸/碰撞）。</summary>
        public bool CanCapturePlayer =>
            isHeadTeacher ||
            (_chasing && _patrolChaseStartTime >= 0f &&
             Time.time - _patrolChaseStartTime >= patrolCatchDelayAfterChaseStart);

        float PatrolViewHalfAngle => binocularHalfAngle + viewConeExtraHalfAngle;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }
        }

        void Start()
        {
            if (isHeadTeacher)
            {
                _agent.speed = headTeacherSpeed;
                _chasing = true;
                _patrolStartCoroutine = StartCoroutine(BeginHeadTeacherHuntWhenReady());
                return;
            }

            _patrolStartCoroutine = StartCoroutine(BeginPatrolWhenReady());
        }

        void OnDisable()
        {
            if (_patrolStartCoroutine != null)
            {
                StopCoroutine(_patrolStartCoroutine);
                _patrolStartCoroutine = null;
            }
        }

        IEnumerator BeginPatrolWhenReady()
        {
            const float timeout = 8f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                if (TryEnsureOnNavMesh())
                {
                    GoNextPatrol();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!TryEnsureOnNavMesh() && !_navMeshWarningLogged)
            {
                _navMeshWarningLogged = true;
                Debug.LogWarning(
                    $"[FleeNightStudy] {name} 未落在 NavMesh 上，巡逻/追击已暂停。请执行「FleeNightStudy → 烘焙 NavMesh（可选）」或把老师挪到可走地面。",
                    gameObject);
            }
        }

        void Update()
        {
            if (GameplayPauseUI.IsPaused)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                if (_agent.isOnNavMesh)
                    _agent.isStopped = true;
                return;
            }

            if (_stunned || _blinded)
            {
                if (_agent.isOnNavMesh)
                    _agent.isStopped = true;
                return;
            }

            if (!TryEnsureOnNavMesh())
                return;

            if (_luring && Time.time < _lureUntil)
            {
                _agent.isStopped = false;
                SafeSetDestination(_lureTarget);
                if (Vector3.Distance(transform.position, _lureTarget) < 1.2f)
                    _luring = false;
                return;
            }

            if (isHeadTeacher)
            {
                ChasePlayerLocked();
                return;
            }

            if (player == null) return;

            UpdatePatrolTeacher();
        }

        void UpdatePatrolTeacher()
        {
            bool canSee = CanSeePlayer();
            bool sameFloor = IsPlayerOnHomeFloor();
            bool canReach = CanReachPlayerOnNavMesh();

            // 追击过程中玩家落在 NavMesh 不可达处 → 目标丢失，恢复巡逻
            if (_chasing && !canReach)
            {
                EndPatrolChase(resumePatrol: true);
                return;
            }

            // 看得见但不同层：站定警戒，不切追击
            if (canSee && !sameFloor)
            {
                if (_chasing)
                    EndPatrolChase(resumePatrol: false);
                _chasing = false;
                _patrolChaseStartTime = -1f;
                HoldStationaryAlert();
                return;
            }

            // 看得见但 NavMesh 够不到（隔窗/不可达地块）：目标丢失，按原巡逻走
            if (canSee && !canReach)
            {
                _chasing = false;
                _patrolChaseStartTime = -1f;
                _stationaryAlert = false;
                ContinuePatrol();
                return;
            }

            _stationaryAlert = false;

            if (canSee && sameFloor && canReach)
            {
                if (!_chasing)
                    _patrolChaseStartTime = Time.time;

                _chasing = true;
                _agent.isStopped = false;
                SetPatrolChaseDestination(player.position);
                return;
            }

            if (_chasing)
            {
                if (!canReach)
                {
                    EndPatrolChase(resumePatrol: true);
                    return;
                }

                if (HasTeacherLeftHomeFloor() ||
                    Vector3.Distance(transform.position, player.position) > chaseLoseDistance)
                {
                    EndPatrolChase(resumePatrol: true);
                    return;
                }

                _agent.isStopped = false;
                SetPatrolChaseDestination(player.position);
                return;
            }

            _patrolChaseStartTime = -1f;
            ContinuePatrol();
        }

        void HoldStationaryAlert()
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            if (!_stationaryAlert)
            {
                _stationaryAlert = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }

            _agent.isStopped = true;
        }

        bool CanReachPlayerOnNavMesh()
        {
            if (player == null || _agent == null || !_agent.isOnNavMesh)
                return false;

            var target = player.position;
            if (!isHeadTeacher)
                target.y = HomeFloorY;

            float sampleRadius = isHeadTeacher ? 16f : 12f;
            if (!NavMesh.SamplePosition(target, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas) &&
                !NavMesh.SamplePosition(target, out hit, sampleRadius + 12f, NavMesh.AllAreas))
                return false;

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                return false;

            return path.status == NavMeshPathStatus.PathComplete;
        }

        void ContinuePatrol()
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = false;
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                GoNextPatrol();
        }

        void EndPatrolChase(bool resumePatrol)
        {
            _chasing = false;
            _patrolChaseStartTime = -1f;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }

            if (resumePatrol)
            {
                if (HasTeacherLeftHomeFloor() &&
                    patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
                {
                    _agent?.Warp(TeacherPatrolConfig.SnapToNavMeshOnFloor(
                        patrolPoints[0].position, HomeFloorY));
                }

                GoNextPatrol();
            }
        }

        public void Stun(float duration)
        {
            if (_stunCoroutine != null) StopCoroutine(_stunCoroutine);
            _stunCoroutine = StartCoroutine(StunRoutine(duration));
        }

        public void SetBlinded(float duration)
        {
            if (_blindCoroutine != null) StopCoroutine(_blindCoroutine);
            _blindCoroutine = StartCoroutine(BlindRoutine(duration));
        }

        public void LureTo(Vector3 worldPos, float duration)
        {
            _lureTarget = worldPos;
            _lureUntil = Time.time + duration;
            _luring = true;
            _chasing = false;
        }

        IEnumerator StunRoutine(float duration)
        {
            _stunned = true;
            if (_agent.isOnNavMesh)
                _agent.isStopped = true;
            yield return new WaitForSeconds(duration);
            _stunned = false;
            if (_agent.isOnNavMesh)
                _agent.isStopped = false;
            _stunCoroutine = null;
            if (!_chasing) GoNextPatrol();
        }

        IEnumerator BlindRoutine(float duration)
        {
            _blinded = true;
            viewDistance *= 0.15f;
            yield return new WaitForSeconds(duration);
            viewDistance /= 0.15f;
            _blinded = false;
            _blindCoroutine = null;
        }

        public void SetHomeFloorIndex(int floorIndex)
        {
            _homeFloorIndex = floorIndex;
            _homeFloorY = TeacherPatrolConfig.GetFloorHeight(floorIndex);
            _hasHomeFloorY = true;
        }

        public void SetHomeFloorY(float worldY)
        {
            _homeFloorY = worldY;
            _homeFloorIndex = TeacherPatrolConfig.GetFloorIndex(worldY);
            _hasHomeFloorY = true;
        }

        float HomeFloorY
        {
            get
            {
                if (!_hasHomeFloorY)
                    CaptureHomeFloorY();
                return _homeFloorY;
            }
        }

        void CaptureHomeFloorY()
        {
            if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
                _homeFloorY = patrolPoints[0].position.y;
            else
                _homeFloorY = transform.position.y;

            _homeFloorIndex = TeacherPatrolConfig.GetFloorIndex(_homeFloorY);
            _hasHomeFloorY = true;
        }

        bool IsPlayerOnHomeFloor()
        {
            if (player == null)
                return false;

            int homeFloor = TeacherPatrolConfig.GetFloorIndex(HomeFloorY);
            int playerFloor = TeacherPatrolConfig.GetFloorIndex(player.position.y);
            if (playerFloor != homeFloor)
                return false;

            return Mathf.Abs(player.position.y - HomeFloorY) <= maxChaseFloorHeightDelta;
        }

        bool HasTeacherLeftHomeFloor()
        {
            return Mathf.Abs(transform.position.y - HomeFloorY) > maxChaseFloorHeightDelta + 0.75f;
        }

        void SetPatrolChaseDestination(Vector3 playerWorldPos)
        {
            var onFloor = playerWorldPos;
            onFloor.y = HomeFloorY;

            if (!NavMesh.SamplePosition(onFloor, out NavMeshHit hit, 12f, NavMesh.AllAreas) &&
                !NavMesh.SamplePosition(onFloor, out hit, 24f, NavMesh.AllAreas))
            {
                SafeSetDestination(onFloor);
                return;
            }

            if (Time.time < _nextChaseRepathTime &&
                Vector3.Distance(_lastChaseSamplePos, hit.position) < 0.6f)
                return;

            _nextChaseRepathTime = Time.time + 0.35f;
            _lastChaseSamplePos = hit.position;
            SafeSetDestination(hit.position);
        }

        bool CanSeePlayer()
        {
            if (_blinded) return false;
            var stealth = player != null ? player.GetComponentInParent<PlayerStealth>() : null;
            if (stealth != null && stealth.IsStealthActive) return false;

            Vector3 to = player.position - transform.position;
            float dist = to.magnitude;
            if (dist > viewDistance) return false;

            Vector3 toFlat = to;
            toFlat.y = 0f;
            if (toFlat.sqrMagnitude < 0.01f) return true;
            toFlat.Normalize();

            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) return false;
            fwd.Normalize();

            if (Vector3.Angle(fwd, toFlat) > PatrolViewHalfAngle)
                return false;

            Vector3 eye = transform.position + Vector3.up * 1.5f;
            Vector3 target = player.position + Vector3.up * 1f;
            Vector3 dir = (target - eye).normalized;
            float rayLen = Mathf.Min(dist, viewDistance);

            if (Physics.Raycast(eye, dir, out RaycastHit hit, rayLen, obstacleMask, QueryTriggerInteraction.Ignore))
                return hit.collider != null && hit.collider.transform.root == player.root;
            return true;
        }

        void GoNextPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            if (patrolPoints[_patrolIndex] != null)
                SafeSetDestination(patrolPoints[_patrolIndex].position);
        }

        void SafeSetDestination(Vector3 destination)
        {
            if (!TryEnsureOnNavMesh())
                return;

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        bool TryEnsureOnNavMesh()
        {
            if (_agent == null || !_agent.enabled)
                return false;

            if (_agent.isOnNavMesh)
                return true;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                return _agent.isOnNavMesh;
            }

            return false;
        }

        public void SetPatrolPoints(Transform[] points) => patrolPoints = points;

        public void SetPatrolRouteIndex(int routeIndex) => patrolRouteIndex = routeIndex;

        public void SetPlayer(Transform p) => player = p;

        public void SetHeadTeacher(bool value) => isHeadTeacher = value;

        /// <summary>普通难度：班主任登场后立即锁定并持续追击玩家。</summary>
        public void BeginHuntPlayer()
        {
            isHeadTeacher = true;
            _chasing = true;
            _agent.speed = headTeacherSpeed;
            if (TryEnsureOnNavMesh() && player != null)
                SafeSetDestination(player.position);
        }

        void ChasePlayerLocked()
        {
            if (player == null)
                return;

            if (!CanReachPlayerOnNavMesh())
            {
                _chasing = false;
                HeadTeacherRandomWander();
                return;
            }

            _headTeacherWandering = false;
            _chasing = true;
            _agent.isStopped = false;

            if (Time.time < _nextRepathTime)
                return;

            _nextRepathTime = Time.time + headTeacherRepathInterval;
            SafeSetDestination(player.position);
        }

        void HeadTeacherRandomWander()
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = false;

            bool needNewTarget = !_headTeacherWandering ||
                                 Time.time >= _nextHeadTeacherWanderPickTime ||
                                 (!_agent.pathPending &&
                                  _agent.remainingDistance <= _agent.stoppingDistance + 0.35f);

            if (!needNewTarget)
                return;

            if (!TryPickRandomWanderPoint(out Vector3 dest))
                return;

            _headTeacherWandering = true;
            _nextHeadTeacherWanderPickTime = Time.time + headTeacherWanderPickInterval;
            SafeSetDestination(dest);
        }

        bool TryPickRandomWanderPoint(out Vector3 destination)
        {
            const int attempts = 10;
            Vector3 origin = transform.position;
            for (int i = 0; i < attempts; i++)
            {
                Vector2 rnd = Random.insideUnitCircle * headTeacherWanderRadius;
                var tryPos = origin + new Vector3(rnd.x, 0f, rnd.y);
                if (NavMesh.SamplePosition(tryPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(origin, hit.position) < 2f)
                        continue;
                    destination = hit.position;
                    return true;
                }
            }

            destination = default;
            return false;
        }

        IEnumerator BeginHeadTeacherHuntWhenReady()
        {
            const float timeout = 8f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                if (TryEnsureOnNavMesh())
                {
                    BeginHuntPlayer();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!TryEnsureOnNavMesh() && !_navMeshWarningLogged)
            {
                _navMeshWarningLogged = true;
                Debug.LogWarning(
                    $"[FleeNightStudy] {name} 未落在 NavMesh 上，班主任追击已暂停。请烘焙 NavMesh 或调整出生点。",
                    gameObject);
            }
        }
    }
}
