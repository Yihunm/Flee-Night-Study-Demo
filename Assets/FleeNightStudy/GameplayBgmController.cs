using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 简单：任一巡查老师进入追击 → 追逐乐；追击结束/甩开 → 探索乐（循环）。
    /// 普通：班主任水平距离 ≤ 半径 → 追逐乐，否则探索乐。
    /// </summary>
    public class GameplayBgmController : MonoBehaviour
    {
        const string ExploreResource = "FleeNightStudy/ExploreBgm";
        const string ChaseResource = "FleeNightStudy/ChaseBgm";

        [SerializeField] float normalModeHeadTeacherChaseRadius = 10f;
        [SerializeField] float teacherScanInterval = 0.15f;
        [SerializeField] [Range(0f, 1f)] float volume = 0.75f;

        AudioSource _source;
        AudioClip _explore;
        AudioClip _chase;
        bool _playingChase;
        Transform _player;
        float _nextTeacherScan;
        TeacherController[] _teachers = System.Array.Empty<TeacherController>();

        void Awake()
        {
            _explore = Resources.Load<AudioClip>(ExploreResource);
            _chase = Resources.Load<AudioClip>(ChaseResource);
            if (_explore == null || _chase == null)
            {
                Debug.LogWarning(
                    "[GameplayBgm] 缺少 BGM：请将 ExploreBgm.mp3、ChaseBgm.mp3 放在 Assets/FleeNightStudy/Resources/FleeNightStudy/");
            }

            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = volume;
            _source.ignoreListenerPause = false;
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                _player = playerGo.transform;
            SetChaseMusic(false);
        }

        void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                if (_source.isPlaying)
                    _source.Stop();
                return;
            }

            if (Time.time >= _nextTeacherScan)
            {
                _nextTeacherScan = Time.time + teacherScanInterval;
                _teachers = FindObjectsOfType<TeacherController>();
            }

            bool wantChase = ShouldPlayChaseMusic();
            if (wantChase != _playingChase)
                SetChaseMusic(wantChase);
        }

        bool ShouldPlayChaseMusic()
        {
            if (GameSessionData.Difficulty == GameDifficulty.Normal)
                return IsHeadTeacherWithinRadius();
            return AnyPatrolTeacherChasing();
        }

        static bool AnyPatrolTeacherChasing(TeacherController[] teachers)
        {
            for (int i = 0; i < teachers.Length; i++)
            {
                var t = teachers[i];
                if (t != null && t.IsPatrolChasing)
                    return true;
            }
            return false;
        }

        bool AnyPatrolTeacherChasing() => AnyPatrolTeacherChasing(_teachers);

        bool IsHeadTeacherWithinRadius()
        {
            if (_player == null)
                return false;

            float r2 = normalModeHeadTeacherChaseRadius * normalModeHeadTeacherChaseRadius;
            Vector3 p = _player.position;
            for (int i = 0; i < _teachers.Length; i++)
            {
                var t = _teachers[i];
                if (t == null || !t.IsHeadTeacher)
                    continue;
                Vector3 tp = t.transform.position;
                float dx = tp.x - p.x;
                float dz = tp.z - p.z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }
            return false;
        }

        void SetChaseMusic(bool chase)
        {
            _playingChase = chase;
            var clip = chase ? _chase : _explore;
            if (clip == null)
                return;
            if (_source.clip == clip && _source.isPlaying)
                return;
            _source.clip = clip;
            _source.Play();
        }
    }
}
