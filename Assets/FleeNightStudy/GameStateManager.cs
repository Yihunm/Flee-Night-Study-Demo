using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FleeNightStudy
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public const string DefaultMainMenuSceneName = "MainMenu";
        public const string DefaultGameplaySceneName = "MergeV2";

        [SerializeField] int textbooksRequired = 8;
        [SerializeField] string nextSceneOrReload = "";
        [SerializeField] string mainMenuSceneName = DefaultMainMenuSceneName;

        float _elapsed;
        bool _firstTextbookRegistered;

        public int TextbooksCollected { get; private set; }
        public int TextbooksRequired => textbooksRequired;
        public int TextbooksRemaining => Mathf.Max(0, textbooksRequired - TextbooksCollected);
        public bool HasCollectedEnoughTextbooks => TextbooksCollected >= textbooksRequired;
        public bool DoorUnlocked { get; private set; }
        public bool GameEnded { get; private set; }
        public float ElapsedSeconds => _elapsed;
        public GameDifficulty Difficulty => GameSessionData.Difficulty;
        public string PlayerName => GameSessionData.PlayerName;

        public event Action<int, int> OnTextbookCountChanged;
        public event Action OnDoorUnlocked;
        public event Action OnVictory;
        public event Action OnGameOver;
        public event Action OnFirstTextbookCollected;
        public event Action<string> OnGameMessage;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (GetComponent<GameplayBootstrap>() == null)
                gameObject.AddComponent<GameplayBootstrap>();
        }

        void Update()
        {
            if (GameEnded) return;
            _elapsed += Time.deltaTime;
        }

        public void RegisterTextbookPickup()
        {
            if (GameEnded) return;

            TextbooksCollected++;
            if (!_firstTextbookRegistered)
            {
                _firstTextbookRegistered = true;
                OnFirstTextbookCollected?.Invoke();
            }

            OnTextbookCountChanged?.Invoke(TextbooksCollected, textbooksRequired);

            if (!DoorUnlocked && TextbooksCollected >= textbooksRequired)
            {
                DoorUnlocked = true;
                OnDoorUnlocked?.Invoke();
            }
        }

        public void ShowMessage(string msg) => OnGameMessage?.Invoke(msg);

        public void TriggerVictory()
        {
            if (GameEnded) return;
            GameEnded = true;
            GameResultStats.RecordVictory(TextbooksCollected, _elapsed, Difficulty, PlayerName);
            GameplayAudioManager.Instance?.PlayClassDismissalBell();
            OnVictory?.Invoke();
            Time.timeScale = 0f;
        }

        public void TriggerGameOver(string reason = "失败：被老师抓住。")
        {
            if (GameEnded) return;
            GameEnded = true;
            GameResultStats.RecordDefeat(TextbooksCollected, _elapsed, Difficulty, PlayerName, reason);
            OnGameOver?.Invoke();
            Time.timeScale = 0f;
        }

        public void TriggerTimeout()
        {
            TriggerGameOver("失败：时间到，未能逃出学校。");
        }

        public void ReloadScene()
        {
            Time.timeScale = 1f;
            MenuCursorPolicy.Unlock();

            string sceneName = !string.IsNullOrWhiteSpace(nextSceneOrReload)
                ? nextSceneOrReload.Trim()
                : SceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(sceneName) || !IsSceneInBuildSettings(sceneName))
                sceneName = DefaultGameplaySceneName;

            SceneManager.LoadScene(sceneName);
        }

        static bool IsSceneInBuildSettings(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return true;
            }

            return false;
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            MenuCursorPolicy.Unlock();
            if (!string.IsNullOrEmpty(mainMenuSceneName))
                SceneManager.LoadScene(mainMenuSceneName);
            else
                SceneManager.LoadScene(0);
        }

        public static void QuitApplication()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
