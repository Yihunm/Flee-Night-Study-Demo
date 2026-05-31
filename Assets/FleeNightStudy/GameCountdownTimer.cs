using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>普通难度 8 分钟倒计时。</summary>
    public class GameCountdownTimer : MonoBehaviour
    {
        float _remaining;
        bool _active;

        public float RemainingSeconds => _remaining;

        void Start()
        {
            if (!GameSessionData.HasCountdown)
            {
                enabled = false;
                return;
            }
            _remaining = GameSessionData.CountdownSeconds;
            _active = true;
            GameplayAudioManager.Instance?.PlayClassStartBell();
        }

        void Update()
        {
            if (!_active || GameStateManager.Instance == null || GameStateManager.Instance.GameEnded)
                return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _active = false;
                GameStateManager.Instance.TriggerTimeout();
            }
        }
    }
}
