using UnityEngine;

namespace FleeNightStudy
{
    public class ExitDoor : MonoBehaviour
    {
        [SerializeField] Collider blockingCollider;
        [Tooltip("勾选则走进触发区即胜利；校门口按 E 胜利时请关闭")]
        [SerializeField] bool triggerVictoryOnEnter;
        bool _unlocked;

        void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnDoorUnlocked += HandleDoorUnlocked;
                if (GameStateManager.Instance.DoorUnlocked)
                    HandleDoorUnlocked();
            }
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnDoorUnlocked -= HandleDoorUnlocked;
        }

        void HandleDoorUnlocked()
        {
            _unlocked = true;
            if (blockingCollider != null)
                blockingCollider.enabled = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!triggerVictoryOnEnter) return;
            if (!_unlocked) return;
            if (!other.CompareTag("Player")) return;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.TriggerVictory();
        }
    }
}
