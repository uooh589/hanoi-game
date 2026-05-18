using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class EscMenuHandler : MonoBehaviour
    {
        public Button resumeBtn, checkBtn, logBtn, menuBtn, genshinBtn, quitBtn;
        public GameObject escPanel;

        void Start()
        {
            if (resumeBtn) resumeBtn.onClick.AddListener(Resume);
            if (checkBtn) checkBtn.onClick.AddListener(CheckUpdate);
            if (logBtn) logBtn.onClick.AddListener(SaveLog);
            if (menuBtn) menuBtn.onClick.AddListener(ReturnToMenu);
            if (genshinBtn) genshinBtn.onClick.AddListener(OpenGenshinSite);
            if (quitBtn) quitBtn.onClick.AddListener(Quit);
        }

        void Resume() => escPanel.SetActive(false);
        void CheckUpdate() { escPanel.SetActive(false); VersionManager.Instance?.CheckForUpdates(); }
        void SaveLog() { escPanel.SetActive(false); LogManager.Instance?.UploadLogs(); }
        void ReturnToMenu() { escPanel.SetActive(false); GameManager.Instance?.ReturnToMenu(); }
        void OpenGenshinSite() => Application.OpenURL("https://ys.mihoyo.com/");
        void Quit() => GameManager.Instance?.QuitGame();
    }
}
