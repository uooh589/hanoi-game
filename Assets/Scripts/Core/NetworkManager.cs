using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace HanoiGame
{
    /// <summary>Simple UDP-based LAN multiplayer for co-op battles.</summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public bool IsHost, IsConnected;
        public string RemoteIP = "127.0.0.1";
        public int Port = 7777;

        private UdpClient _socket;
        private Thread _recvThread;
        private readonly Queue<Action> _mainThreadActions = new();
        private IPEndPoint _remoteEP;

        public event Action<string, string> OnMessage; // (type, data)
        public event Action OnConnected;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Host()
        {
            try
            {
                _socket = new UdpClient(Port);
                _recvThread = new Thread(ReceiveLoop) { IsBackground = true };
                _recvThread.Start();
                IsHost = true;
                IsConnected = true;
                Debug.Log($"[Network] Hosting on port {Port}");
                OnConnected?.Invoke();
            }
            catch (Exception e) { Debug.LogError($"[Network] Host failed: {e.Message}"); }
        }

        public void Join(string ip)
        {
            try
            {
                RemoteIP = ip;
                _socket = new UdpClient(0); // any port
                _remoteEP = new IPEndPoint(IPAddress.Parse(ip), Port);
                _recvThread = new Thread(ReceiveLoop) { IsBackground = true };
                _recvThread.Start();
                IsHost = false;
                Send("join", "hello");
                IsConnected = true;
                Debug.Log($"[Network] Joining {ip}:{Port}");
                OnConnected?.Invoke();
            }
            catch (Exception e) { Debug.LogError($"[Network] Join failed: {e.Message}"); }
        }

        public void Send(string type, string data)
        {
            if (_socket == null || !IsConnected) return;
            try
            {
                string msg = $"{type}|{data}";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);
                if (IsHost && _remoteEP != null)
                    _socket.Send(bytes, bytes.Length, _remoteEP);
                else if (!IsHost)
                    _socket.Send(bytes, bytes.Length, RemoteIP, Port);
            }
            catch (Exception e) { Debug.LogError($"[Network] Send error: {e.Message}"); }
        }

        void ReceiveLoop()
        {
            while (_socket != null)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _socket.Receive(ref ep);
                    string msg = Encoding.UTF8.GetString(data);
                    if (IsHost) _remoteEP = ep; // remember client address

                    int sep = msg.IndexOf('|');
                    if (sep > 0)
                    {
                        string type = msg.Substring(0, sep);
                        string payload = msg.Substring(sep + 1);
                        lock (_mainThreadActions)
                            _mainThreadActions.Enqueue(() => OnMessage?.Invoke(type, payload));
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (SocketException) { break; }
                catch (Exception e) { Debug.LogError($"[Network] Recv error: {e.Message}"); break; }
            }
        }

        void Update()
        {
            lock (_mainThreadActions)
                while (_mainThreadActions.Count > 0)
                    _mainThreadActions.Dequeue()?.Invoke();
        }

        public void Stop()
        {
            IsConnected = false;
            _recvThread?.Abort();
            _socket?.Close();
            _socket = null;
        }

        void OnDestroy() { Stop(); }
        void OnApplicationQuit() { Stop(); }
    }
}
