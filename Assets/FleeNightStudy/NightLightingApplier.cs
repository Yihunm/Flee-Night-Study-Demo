using UnityEngine;
using UnityEngine.Rendering;

namespace FleeNightStudy
{
    /// <summary>
    /// 将黄昏主光调整为「夜晚」观感：太阳高度降低、光变弱、略偏冷色。
    /// 适用于任意管线下的 <see cref="Light"/>（HDRP 方向光仍用同一组件，强度单位随项目设置可能为 Lux，请在 Inspector 微调）。
    /// </summary>
    public class NightLightingApplier : MonoBehaviour
    {
        [Header("目标光源（空则自动查找名为 Directional Light 的物体）")]
        [SerializeField] Light directionalLight;

        [Header("夜晚参数")]
        [SerializeField] Vector3 nightEulerAngles = new Vector3(55f, -35f, 0f);
        [SerializeField] Color nightColor = new Color(0.55f, 0.62f, 0.95f, 1f);
        [SerializeField] float nightIntensity = 450f;

        [Tooltip("若为 true，进入场景 Awake 时自动应用")]
        [SerializeField] bool applyOnAwake = true;

        [Header("环境光（简单压暗）")]
        [SerializeField] bool adjustAmbient = true;
        [SerializeField] Color ambientColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        [SerializeField] float ambientIntensity = 0.35f;

        void Awake()
        {
            if (applyOnAwake)
                ApplyNight();
        }

        [ContextMenu("Apply Night Now")]
        public void ApplyNight()
        {
            if (directionalLight == null)
            {
                var go = GameObject.Find("Directional Light");
                if (go != null) directionalLight = go.GetComponent<Light>();
            }

            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(nightEulerAngles);
                directionalLight.color = nightColor;
                directionalLight.intensity = nightIntensity;
            }

            if (!adjustAmbient) return;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor * ambientIntensity;
        }
    }
}
