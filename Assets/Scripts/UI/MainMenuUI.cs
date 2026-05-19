using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    /// <summary>
    /// Main menu: New Game, Continue (if save exists), Quit.
    /// Attach to MainMenuPanel.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button newGameButton;
        public Button continueButton;
        public Button libraryButton;
        public Button lobbyButton;
        public Button quitButton;

        [Header("Title")]
        public Text titleText;

        private void Start()
        {
            if (titleText != null)
                titleText.text = "汉诺塔：轮回";

            if (newGameButton != null)
                newGameButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    GameManager.Instance.StartNewGame();
                });

            if (continueButton != null)
            {
                bool hasSave = SaveManager.SaveExists();
                continueButton.interactable = hasSave;
                continueButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    GameManager.Instance.ContinueGame();
                });
            }

            if (libraryButton != null)
            {
                // Find the LibraryPanel in Canvas children (even if inactive)
                var canvas = transform.parent; // MainMenuPanel -> Canvas
                var lib = canvas?.Find("LibraryPanel");
                libraryButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    if (lib != null) { lib.transform.SetAsLastSibling(); lib.gameObject.SetActive(true); }
                });
            }

            if (lobbyButton != null)
            {
                var canvas = transform.parent;
                var lobby = canvas?.Find("LobbyPanel");
                lobbyButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    if (lobby != null) { lobby.transform.SetAsLastSibling(); lobby.gameObject.SetActive(true); }
                });
            }

            if (quitButton != null)
                quitButton.onClick.AddListener(() =>
                {
                    SimpleAudio.Instance?.PlayClick();
                    GameManager.Instance.QuitGame();
                });
        }
    }
}
