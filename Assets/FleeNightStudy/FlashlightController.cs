using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 手电筒开关（默认 F）。与资源包内全局命名 <c>Flashlight</c> 不冲突。
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        [Header("目标灯光")]
        [SerializeField] Light targetLight;

        [Header("按键")]
        [SerializeField] KeyCode toggleKey = KeyCode.F;

        [Tooltip("若勾选，Time.timeScale 为 0（暂停）时仍可开关")]
        [SerializeField] bool allowToggleWhenPaused;

        [Tooltip("进入场景时灯是否打开")]
        [SerializeField] bool onByDefault = true;

        [Tooltip("胜负已分时禁止开关（依赖 GameStateManager）")]
        [SerializeField] bool lockWhenGameEnded = true;

        void Reset()
        {
            targetLight = GetComponent<Light>();
            if (targetLight == null)
                targetLight = GetComponentInChildren<Light>();
        }

        void Start()
        {
            if (targetLight != null)
                targetLight.enabled = onByDefault;
        }

        void Update()
        {
            if (!allowToggleWhenPaused && Time.timeScale <= 0f)
                return;

            if (lockWhenGameEnded && GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;

            if (Input.GetKeyDown(toggleKey))
                Toggle();
        }

        public void Toggle()
        {
            if (targetLight == null) return;
            targetLight.enabled = !targetLight.enabled;
        }

        public void SetFlashlight(bool on)
        {
            if (targetLight == null) return;
            targetLight.enabled = on;
        }

        public bool IsOn => targetLight != null && targetLight.enabled;
    }
}
