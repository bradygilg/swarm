using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Swarm
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "Game";

        [SerializeField] Button startGameButton;
        [SerializeField] Button exitGameButton;

        void Awake()
        {
            if (startGameButton != null)
                startGameButton.onClick.AddListener(StartGame);
            if (exitGameButton != null)
                exitGameButton.onClick.AddListener(ExitGame);
        }

        public void StartGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
