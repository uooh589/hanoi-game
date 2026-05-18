using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class EscMenuHandler : MonoBehaviour
    {
        public Button resumeBtn, checkBtn, logBtn, menuBtn, genshinBtn, quitBtn;
        public GameObject escPanel;
        public Text musicVolText, sfxVolText;
        public Button musicUpBtn, musicDownBtn, sfxUpBtn, sfxDnBtn;

        private float _musicVol = 0.3f, _sfxVol = 0.1f;

        void Start()
        {
            if (resumeBtn) resumeBtn.onClick.AddListener(Resume);
            if (checkBtn) checkBtn.onClick.AddListener(CheckUpdate);
            if (logBtn) logBtn.onClick.AddListener(SaveLog);
            if (menuBtn) menuBtn.onClick.AddListener(ReturnToMenu);
            if (genshinBtn) genshinBtn.onClick.AddListener(OpenGenshinSite);
            if (quitBtn) quitBtn.onClick.AddListener(Quit);
            if (musicUpBtn) musicUpBtn.onClick.AddListener(() => AdjustMusic(0.05f));
            if (musicDownBtn) musicDownBtn.onClick.AddListener(() => AdjustMusic(-0.05f));
            if (sfxUpBtn) sfxUpBtn.onClick.AddListener(() => AdjustSFX(0.05f));
            if (sfxDnBtn) sfxDnBtn.onClick.AddListener(() => AdjustSFX(-0.05f));
            UpdateLabels();
        }

        void AdjustMusic(float delta) { _musicVol = Mathf.Clamp01(_musicVol + delta); BGMPlayer.Instance?.SetVolume(_musicVol); UpdateLabels(); }
        void AdjustSFX(float delta) { _sfxVol = Mathf.Clamp01(_sfxVol + delta); SimpleAudio.Instance?.SetVolume(_sfxVol); UpdateLabels(); }
        void UpdateLabels()
        {
            if (musicVolText) musicVolText.text = $"BGM: {(int)(_musicVol * 100)}%";
            if (sfxVolText) sfxVolText.text = $"音效: {(int)(_sfxVol * 100)}%";
        }

        void Resume() => escPanel.SetActive(false);
        void CheckUpdate() { escPanel.SetActive(false); VersionManager.Instance?.CheckForUpdates(); }
        void SaveLog() { escPanel.SetActive(false); LogManager.Instance?.UploadLogs(); }
        void ReturnToMenu() { escPanel.SetActive(false); GameManager.Instance?.ReturnToMenu(); }
        void OpenGenshinSite() => Application.OpenURL("https://ys.mihoyo.com/");
        void Quit() => GameManager.Instance?.QuitGame();
    }
}
