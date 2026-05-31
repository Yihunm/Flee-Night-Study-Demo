using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 胜负已分时禁用第一人称相关控制：SimpleFPController（若存在）、<see cref="FirstPersonWalker"/>、<see cref="FlashlightController"/>。
    /// 拖到任意常驻物体（如 Managers）上；依赖场景内已有 <see cref="GameStateManager"/>。
    /// </summary>
    public class GameFlowDisableSimpleFp : MonoBehaviour
    {
        void OnEnable()
        {
            if (GameStateManager.Instance == null) return;
            GameStateManager.Instance.OnVictory += HandleEnd;
            GameStateManager.Instance.OnGameOver += HandleEnd;
        }

        void OnDisable()
        {
            if (GameStateManager.Instance == null) return;
            GameStateManager.Instance.OnVictory -= HandleEnd;
            GameStateManager.Instance.OnGameOver -= HandleEnd;
        }

        void HandleEnd()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && mb.GetType().Name == "SimpleFPController")
                    mb.enabled = false;
            }

            var walker = player.GetComponentInChildren<FirstPersonWalker>();
            if (walker != null) walker.enabled = false;

            foreach (var fl in player.GetComponentsInChildren<FlashlightController>(true))
                fl.enabled = false;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
