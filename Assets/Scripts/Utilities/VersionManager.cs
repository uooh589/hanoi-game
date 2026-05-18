using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace HanoiGame
{
    public class VersionManager : MonoBehaviour
    {
        public static string CurrentVersion = "v1.0.0";
        public static string UpdateCheckUrl = ""; // set in config file or inspector
        public Text statusText;

        public static VersionManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadConfig();
        }

        void LoadConfig()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.txt");
            try
            {
                if (System.IO.File.Exists(path))
                {
                    foreach (var line in System.IO.File.ReadAllLines(path))
                    {
                        if (line.StartsWith("update_url="))
                            UpdateCheckUrl = line.Substring(11).Trim();
                        if (line.StartsWith("log_url="))
                            LogManager.UploadUrl = line.Substring(8).Trim();
                    }
                }
            }
            catch { /* ignore */ }
        }

        public void CheckForUpdates()
        {
            if (string.IsNullOrEmpty(UpdateCheckUrl))
            {
                if (statusText) statusText.text = "当前版本: " + CurrentVersion;
                return;
            }
            StartCoroutine(CheckCoroutine());
        }

        IEnumerator CheckCoroutine()
        {
            using var req = UnityWebRequest.Get(UpdateCheckUrl);
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var latest = req.downloadHandler.text.Trim();
                if (latest != CurrentVersion)
                {
                    if (statusText) statusText.text = "新版本: " + latest + " | 当前: " + CurrentVersion;
                }
                else
                {
                    if (statusText) statusText.text = "已是最新: " + CurrentVersion;
                }
            }
            else
            {
                if (statusText) statusText.text = "当前版本: " + CurrentVersion;
            }
        }
    }
}
