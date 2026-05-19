using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public class LobbyUI : MonoBehaviour
    {
        public Button hostBtn, joinBtn, backBtn;
        public InputField ipInput;
        public Text statusText;

        void Start()
        {
            hostBtn?.onClick.AddListener(() =>
            {
                SimpleAudio.Instance?.PlayClick();
                NetworkManager.Instance.Host();
                statusText.text = "已创建房间，等待玩家连接...";
                hostBtn.interactable = false;
                joinBtn.interactable = false;
            });

            joinBtn?.onClick.AddListener(() =>
            {
                SimpleAudio.Instance?.PlayClick();
                string ip = ipInput != null ? ipInput.text : "127.0.0.1";
                NetworkManager.Instance.Join(ip);
                statusText.text = $"正在连接 {ip}...";
                hostBtn.interactable = false;
                joinBtn.interactable = false;
            });

            backBtn?.onClick.AddListener(() => { SimpleAudio.Instance?.PlayClick(); gameObject.SetActive(false); });

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnected += () =>
                {
                    statusText.text = NetworkManager.Instance.IsHost ? "已创建房间" : "已连接到房间！";
                    // Start co-op game
                    if (NetworkManager.Instance.IsHost)
                        GameManager.Instance?.StartCoopGame();
                };

                NetworkManager.Instance.OnMessage += (type, data) =>
                {
                    if (type == "join")
                    {
                        statusText.text = "玩家已加入！";
                        GameManager.Instance?.StartCoopGame();
                    }
                };
            }
        }

        void OnEnable()
        {
            if (ipInput != null) ipInput.text = "127.0.0.1";
            if (statusText != null) statusText.text = "LAN联机模式\n一台电脑创建房间，另一台输入IP加入";
        }
    }
}
