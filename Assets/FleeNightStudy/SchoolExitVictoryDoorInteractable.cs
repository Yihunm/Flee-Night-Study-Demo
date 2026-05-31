using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 校门口胜利门：课本集齐后按 E 开门并触发胜利；未集齐则提示还差几本。
    /// 挂在 <see cref="Door_01"/> / <see cref="Door_02_Snaps014"/> 等校门物体上，与 <see cref="SlidingDoorInteractable"/> 或 <see cref="HingeDoorInteractable"/> 配合。
    /// </summary>
    [DisallowMultipleComponent]
    public class SchoolExitVictoryDoorInteractable : MonoBehaviour
    {
        [SerializeField] SlidingDoorInteractable slidingDoor;
        [SerializeField] HingeDoorInteractable hingeDoor;

        [SerializeField] string lockedHintFormat = "再收集{0}本课本才能离开学校（还剩{0}本）";

        public bool IsReady()
        {
            var gsm = GameStateManager.Instance;
            return gsm != null && gsm.HasCollectedEnoughTextbooks && !gsm.GameEnded;
        }

        public int GetRemaining()
        {
            var gsm = GameStateManager.Instance;
            return gsm != null ? gsm.TextbooksRemaining : 0;
        }

        public string GetLockedHint()
        {
            return string.Format(lockedHintFormat, GetRemaining());
        }

        /// <summary>课本足够则开门并胜利。</summary>
        public bool TryToggle()
        {
            if (!IsReady())
                return false;

            OpenDoor();

            var gsm = GameStateManager.Instance;
            if (gsm != null && !gsm.GameEnded)
                gsm.TriggerVictory();

            return true;
        }

        void OpenDoor()
        {
            if (slidingDoor == null)
                slidingDoor = GetComponent<SlidingDoorInteractable>();
            if (hingeDoor == null)
                hingeDoor = GetComponent<HingeDoorInteractable>();

            if (slidingDoor != null)
                slidingDoor.Toggle();
            else if (hingeDoor != null)
                hingeDoor.Toggle();
        }

        void Reset()
        {
            if (slidingDoor == null)
                slidingDoor = GetComponent<SlidingDoorInteractable>();
            if (hingeDoor == null)
                hingeDoor = GetComponent<HingeDoorInteractable>();
        }
    }
}
