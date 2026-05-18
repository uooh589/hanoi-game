using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace HanoiGame
{
    public class LogManager : MonoBehaviour
    {
        public static string UploadUrl = ""; // set via config file
        public Text statusText;

        public static LogManager Instance { get; private set; }
        private static readonly List<string> _logs = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += OnUnityLog;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnUnityLog;
        }

        void OnUnityLog(string msg, string stack, LogType type)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] [{type}] {msg}";
            if (type == LogType.Error || type == LogType.Exception)
                entry += "\n" + stack;
            _logs.Add(entry);
            if (_logs.Count > 500) _logs.RemoveAt(0);
        }

        public static void Log(string msg)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] [Game] {msg}";
            _logs.Add(entry);
            if (_logs.Count > 500) _logs.RemoveAt(0);
        }

        public string GetLogs(int maxLines = 200)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Hanoi Game Log (v{VersionManager.CurrentVersion}) ===");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
            sb.AppendLine($"Platform: {Application.platform}");
            sb.AppendLine("---");
            int start = Mathf.Max(0, _logs.Count - maxLines);
            for (int i = start; i < _logs.Count; i++)
                sb.AppendLine(_logs[i]);
            return sb.ToString();
        }

        public string SaveToFile()
        {
            string path = Path.Combine(Application.persistentDataPath,
                $"hanoi_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            try
            {
                File.WriteAllText(path, GetLogs());
                Debug.Log("[LogManager] Saved log to: " + path);
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError("[LogManager] Failed to save log: " + e.Message);
                return null;
            }
        }

        public void UploadLogs()
        {
            if (string.IsNullOrEmpty(UploadUrl))
            {
                string path = SaveToFile();
                if (statusText && !string.IsNullOrEmpty(path))
                    statusText.text = "日志已保存: " + Path.GetFileName(path);
                return;
            }
            StartCoroutine(UploadCoroutine());
        }

        IEnumerator UploadCoroutine()
        {
            string logData = GetLogs();
            byte[] body = Encoding.UTF8.GetBytes(logData);

            using var req = new UnityWebRequest(UploadUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "text/plain");
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                if (statusText) statusText.text = "日志已上传";
                Debug.Log("[LogManager] Log uploaded successfully");
            }
            else
            {
                // Fall back to local save
                string path = SaveToFile();
                if (statusText) statusText.text = "上传失败，已存本地: " + (path != null ? Path.GetFileName(path) : "");
            }
        }

        public string GetLogPath() => Path.Combine(Application.persistentDataPath, "hanoi_logs/");
    }
}
