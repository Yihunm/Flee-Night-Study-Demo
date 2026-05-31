using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 挂在始终激活的 Managers 上，统一响应胜负并驱动 UI / 音效（避免 UI 在 inactive 面板上收不到事件）。
    /// </summary>
    public class GameResultController : MonoBehaviour
    {
        [SerializeField] GameOverUI gameOverUi;
        [SerializeField] GameResultAudio gameResultAudio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoEnsureOnManagers()
        {
            GameResultUiBootstrap.FixOverlayCanvases();
            GameResultUiBootstrap.Ensure();

            if (FindObjectOfType<GameResultController>() != null)
                return;

            var managers = GameObject.Find("Managers");
            if (managers == null)
                return;

            managers.AddComponent<GameResultController>();
        }

        void Start()
        {
            GameResultUiBootstrap.Ensure();
            ResolveReferences();
            if (gameOverUi != null)
                GameResultUiBootstrap.WireAllResultButtons(gameOverUi);
            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                UnityEngine.Debug.LogWarning("[GameResultController] 未找到 GameStateManager。", this);
                return;
            }

            gsm.OnVictory += HandleVictory;
            gsm.OnGameOver += HandleGameOver;
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance == null) return;
            GameStateManager.Instance.OnVictory -= HandleVictory;
            GameStateManager.Instance.OnGameOver -= HandleGameOver;
        }

        void HandleVictory()
        {
            ResolveReferences();
            gameResultAudio?.PlayVictory();
            gameOverUi?.ShowVictory();
        }

        void HandleGameOver()
        {
            ResolveReferences();
            gameResultAudio?.PlayDefeat();
            gameOverUi?.ShowDefeat();
        }

        void ResolveReferences()
        {
            GameResultUiBootstrap.Ensure();

            if (gameOverUi == null || !GameResultUiBootstrap.HasValidPanels(gameOverUi))
                gameOverUi = GameResultUiBootstrap.GetPrimaryUi();

            if (gameOverUi == null)
            {
                GameResultUiBootstrap.Ensure();
                gameOverUi = GameResultUiBootstrap.GetPrimaryUi();
            }
            if (gameResultAudio == null)
                gameResultAudio = FindObjectOfType<GameResultAudio>(true);
            if (gameResultAudio == null && gameOverUi != null)
                gameResultAudio = gameOverUi.gameObject.AddComponent<GameResultAudio>();
        }
    }
}
