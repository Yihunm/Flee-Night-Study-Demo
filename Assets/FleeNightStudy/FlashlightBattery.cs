using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 可选：手电筒开启时耗电，耗尽自动关灯。
    /// </summary>
    public class FlashlightBattery : MonoBehaviour
    {
        [SerializeField] Light spotLight;
        [SerializeField] float maxBattery = 100f;
        [SerializeField] float drainPerSecond = 8f;

        float _current;

        void Awake()
        {
            if (spotLight == null)
                spotLight = GetComponentInChildren<Light>();
            _current = maxBattery;
        }

        void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;
            if (spotLight == null || !spotLight.enabled) return;

            _current -= drainPerSecond * Time.deltaTime;
            if (_current <= 0f)
            {
                _current = 0f;
                spotLight.enabled = false;
            }
        }

        public void Recharge(float amount)
        {
            _current = Mathf.Min(maxBattery, _current + amount);
        }
    }
}
