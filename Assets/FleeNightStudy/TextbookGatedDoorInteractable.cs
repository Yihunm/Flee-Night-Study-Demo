using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 挂在与 <see cref="SlidingDoorInteractable"/> 同一物体（或父物体）上：
    /// 课本未集齐时按 E 不会开门，<see cref="DoorInteractor"/> 会显示锁定提示。
    /// </summary>
    [DisallowMultipleComponent]
    public class TextbookGatedDoorInteractable : MonoBehaviour
    {
        [Tooltip("要控制的推拉门；留空则在本物体及子物体上查找")]
        [SerializeField] SlidingDoorInteractable door;

        [Tooltip("0 表示使用 GameStateManager 的 Textbooks Required")]
        [SerializeField] int textbooksRequiredOverride;

        [Tooltip("{0} 为还差几本")]
        [SerializeField] string lockedHintFormat = "再收集{0}本课本以解锁大门（还剩{0}本）";

        public bool IsUnlocked()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null)
                return false;

            int required = textbooksRequiredOverride > 0
                ? textbooksRequiredOverride
                : gsm.TextbooksRequired;

            return gsm.TextbooksCollected >= required;
        }

        public int GetRemaining()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null)
                return 0;

            int required = textbooksRequiredOverride > 0
                ? textbooksRequiredOverride
                : gsm.TextbooksRequired;

            return Mathf.Max(0, required - gsm.TextbooksCollected);
        }

        public string GetLockedHint()
        {
            return string.Format(lockedHintFormat, GetRemaining());
        }

        /// <summary>课本足够则 Toggle 门扇并返回 true。</summary>
        public bool TryToggle()
        {
            if (!IsUnlocked())
                return false;

            if (door == null)
                door = GetComponent<SlidingDoorInteractable>();
            if (door == null)
                door = GetComponentInParent<SlidingDoorInteractable>();
            if (door == null)
                door = GetComponentInChildren<SlidingDoorInteractable>();

            if (door == null)
            {
                UnityEngine.Debug.LogWarning($"[TextbookGatedDoorInteractable] {name} 未找到 SlidingDoorInteractable。", this);
                return false;
            }

            door.Toggle();
            return true;
        }

        void Reset()
        {
            if (door == null)
                door = GetComponent<SlidingDoorInteractable>();
        }
    }
}
