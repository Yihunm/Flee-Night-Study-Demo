using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 进入 Play 时关闭 Asset Store 学校演示用的 <c>UnityTemplateProjects.SimpleCameraController</c>（飞行相机），
    /// 避免与 <see cref="FirstPersonWalker"/> 抢输入。挂在任意常驻物体上（如空物体 DemoBootstrap）。
    /// 不直接引用该类型，拷贝到无学校包工程也不会编译失败。
    /// </summary>
    public class SchoolFlyCameraDisabler : MonoBehaviour
    {
        const string FlyCamTypeName = "UnityTemplateProjects.SimpleCameraController";

        [SerializeField] bool disableInAwake = true;

        void Awake()
        {
            if (!disableInAwake) return;
            foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (mb.GetType().FullName == FlyCamTypeName)
                    mb.enabled = false;
            }
        }
    }
}
