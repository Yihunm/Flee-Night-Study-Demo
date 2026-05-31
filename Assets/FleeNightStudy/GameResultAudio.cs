using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 胜负音效：通常由 <see cref="GameResultController"/> 调用；也可挂 Animation Event。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GameResultAudio : MonoBehaviour
    {
        [SerializeField] AudioClip victoryClip;
        [SerializeField] AudioClip defeatClip;
        [SerializeField] [Range(0f, 1f)] float volume = 0.85f;
        [Tooltip("无 GameResultController 时自行监听 GameStateManager（旧场景兼容）")]
        [SerializeField] bool playOnGameStateEvents;

        AudioSource _source;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            if (victoryClip == null)
                victoryClip = Resources.Load<AudioClip>("FleeNightStudy/Victory");
            if (defeatClip == null)
                defeatClip = Resources.Load<AudioClip>("FleeNightStudy/Defeat");
        }

        void OnEnable()
        {
            if (!playOnGameStateEvents || FindObjectOfType<GameResultController>() != null)
                return;
            if (GameStateManager.Instance == null) return;
            GameStateManager.Instance.OnVictory += HandleVictory;
            GameStateManager.Instance.OnGameOver += HandleGameOver;
        }

        void OnDisable()
        {
            if (!playOnGameStateEvents || GameStateManager.Instance == null) return;
            GameStateManager.Instance.OnVictory -= HandleVictory;
            GameStateManager.Instance.OnGameOver -= HandleGameOver;
        }

        void HandleVictory() => PlayVictory();

        void HandleGameOver() => PlayDefeat();

        public void PlayVictory()
        {
            if (victoryClip != null && _source != null)
                _source.PlayOneShot(victoryClip, volume);
        }

        public void PlayDefeat()
        {
            if (defeatClip != null && _source != null)
                _source.PlayOneShot(defeatClip, volume);
        }

        public void OnAnimationPlayVictorySound() => PlayVictory();

        public void OnAnimationPlayDefeatSound() => PlayDefeat();
    }
}
