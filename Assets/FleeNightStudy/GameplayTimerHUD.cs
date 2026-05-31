using UnityEngine;
using TMPro;

namespace FleeNightStudy
{
    public class GameplayTimerHUD : MonoBehaviour
    {
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text messageText;
        GameCountdownTimer _timer;

        void Start()
        {
            _timer = FindObjectOfType<GameCountdownTimer>();
            if (!GameSessionData.HasCountdown)
                gameObject.SetActive(false);

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnGameMessage += ShowMessage;
        }

        public void Bind(TMP_Text timer, TMP_Text message)
        {
            timerText = timer;
            messageText = message;
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnGameMessage -= ShowMessage;
        }

        void Update()
        {
            if (timerText == null || _timer == null) return;
            float t = _timer.RemainingSeconds;
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            timerText.text = $"剩余时间 {m:00}:{s:00}";
            timerText.color = t < 60f ? new Color(1f, 0.45f, 0.35f) : Color.white;
        }

        void ShowMessage(string msg)
        {
            if (messageText == null) return;
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 4f);
        }

        void ClearMessage()
        {
            if (messageText != null) messageText.text = "";
        }
    }
}
