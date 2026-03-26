using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Manages all network communication for multiplayer games.
    /// Uses TCP sockets for reliable communication between devices.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int defaultPort = 7777;
        [SerializeField] private float connectionTimeout = 30f;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;
        [SerializeField] private bool showTraffic = true;

        // Events - Connection
        public event Action<string> OnRoomCreated;
        public event Action<bool> OnRoomJoined;
        public event Action OnPlayerJoined;
        public event Action OnPlayerLeft;
        public event Action OnGameReady;
        public event Action<string> OnError;

        // Events - Game
        public event Action<bool> OnTurnChanged;
        public event Action<string> OnOpponentCharacterSelected;
        public event Action<string, bool> OnQuestionReceived;
        public event Action<bool> OnAnswerReceived;
        public event Action<string, bool> OnEliminationReceived;
        public event Action<string> OnOpponentGuess;
        public event Action<bool, string> OnGameOver;

        // Network state
        private string currentRoomCode;
        private bool isHost = false;
        private bool isConnected = false;
        private bool isGameActive = false;
        private string localPlayerId;

        // TCP networking
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private Thread listenerThread;
        private Thread receiveThread;
        private bool isRunning = false;

        // Message handling
        private readonly object lockObject = new object();
        private Queue<Action> mainThreadActions = new Queue<Action>();
        private const int BUFFER_SIZE = 8192;

        // Message framing
        private const string MESSAGE_END = "<EOM>";

        #region Properties
        public bool IsHost => isHost;
        public bool IsConnected => isConnected;
        public string CurrentRoomCode => currentRoomCode;
        public string LocalPlayerId => localPlayerId;
        public string HostIPAddress { get; private set; }
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            GenerateLocalPlayerId();
        }

        private void GenerateLocalPlayerId()
        {
            localPlayerId = Guid.NewGuid().ToString();
        }

        private void Update()
        {
            // Process main thread actions from network threads
            lock (lockObject)
            {
                while (mainThreadActions.Count > 0)
                {
                    var action = mainThreadActions.Dequeue();
                    action?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            LeaveRoom();
        }

        private void OnApplicationQuit()
        {
            LeaveRoom();
        }

        private void LogDebug(string message)
        {
            if (debugLogging)
                Debug.Log($"[Network] {message}");
        }

        private void LogTraffic(string direction, string message)
        {
            if (showTraffic)
                Debug.Log($"[Network Traffic {direction}] {message}");
        }

        #endregion

        #region Room Management

        public void CreateRoom()
        {
            try
            {
                string localIP = GetLocalIPAddress();
                HostIPAddress = localIP;

                tcpListener = new TcpListener(IPAddress.Any, defaultPort);
                tcpListener.Start();
                isRunning = true;

                currentRoomCode = EncodeIPToCode(localIP, defaultPort);
                isHost = true;
                isConnected = true;
                isGameActive = false;

                LogDebug($"Room created: {currentRoomCode} (IP: {localIP}:{defaultPort})");
                OnRoomCreated?.Invoke(currentRoomCode);

                listenerThread = new Thread(ListenForConnections);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                StartCoroutine(WaitForPlayerToJoin());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Failed to create room: {ex.Message}");
                OnError?.Invoke("Failed to create room: " + ex.Message);
            }
        }

        public void JoinRoom(string roomCode)
        {
            try
            {
                currentRoomCode = roomCode.ToUpper().Trim();
                string hostIP = DecodeCodeToIP(currentRoomCode);

                if (string.IsNullOrEmpty(hostIP))
                {
                    OnError?.Invoke("Invalid room code");
                    OnRoomJoined?.Invoke(false);
                    return;
                }

                LogDebug($"Connecting to {hostIP}:{defaultPort}...");

                tcpClient = new TcpClient();

                // Use synchronous connect with timeout
                var connectResult = tcpClient.BeginConnect(hostIP, defaultPort, null, null);

                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    OnError?.Invoke("Connection timeout - host not found");
                    OnRoomJoined?.Invoke(false);
                    return;
                }

                tcpClient.EndConnect(connectResult);

                isHost = false;
                isConnected = true;
                isRunning = true; // IMPORTANT: Must be set before starting receive thread!
                isGameActive = false;
                networkStream = tcpClient.GetStream();

                // Disable Nagle's algorithm for faster message delivery
                tcpClient.NoDelay = true;

                LogDebug("Connected to host!");
                OnRoomJoined?.Invoke(true);

                // Start receiving messages
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                StartCoroutine(NotifyConnectionReady());
            }
            catch (SocketException ex)
            {
                Debug.LogError($"[Network] Connection failed: {ex.Message}");
                OnError?.Invoke("Could not connect to host. Check if host is running and IP is correct.");
                OnRoomJoined?.Invoke(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Join failed: {ex.Message}");
                OnError?.Invoke("Failed to join room: " + ex.Message);
                OnRoomJoined?.Invoke(false);
            }
        }

        public void LeaveRoom()
        {
            isRunning = false;
            isConnected = false;
            isGameActive = false;

            if (networkStream != null)
            {
                try { networkStream.Close(); } catch { }
                networkStream = null;
            }

            if (tcpClient != null)
            {
                try { tcpClient.Close(); } catch { }
                tcpClient = null;
            }

            if (tcpListener != null)
            {
                try { tcpListener.Stop(); } catch { }
                tcpListener = null;
            }

            if (listenerThread != null && listenerThread.IsAlive)
                listenerThread.Join(500);

            if (receiveThread != null && receiveThread.IsAlive)
                receiveThread.Join(500);

            currentRoomCode = null;
            isHost = false;

            LogDebug("Left room and closed all connections");
        }

        #endregion

        #region Network Communication

        private void ListenForConnections()
        {
            try
            {
                while (isRunning)
                {
                    if (tcpListener.Pending())
                    {
                        tcpClient = tcpListener.AcceptTcpClient();

                        // Disable Nagle's algorithm
                        tcpClient.NoDelay = true;

                        networkStream = tcpClient.GetStream();

                        LogDebug("Player connected!");

                        QueueMainThreadAction(() => {
                            OnPlayerJoined?.Invoke();
                            StartCoroutine(StartGameAfterConnection());
                        });

                        // Start receiving messages immediately
                        receiveThread = new Thread(ReceiveMessages);
                        receiveThread.IsBackground = true;
                        receiveThread.Start();

                        break;
                    }
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                    Debug.LogError($"[Network] Listener error: {ex.Message}");
            }
        }

        private void ReceiveMessages()
        {
            LogDebug("Receive thread started");

            byte[] buffer = new byte[BUFFER_SIZE];
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                while (isRunning && networkStream != null && tcpClient != null && tcpClient.Connected)
                {
                    if (networkStream.DataAvailable)
                    {
                        int bytesRead = networkStream.Read(buffer, 0, BUFFER_SIZE);
                        if (bytesRead > 0)
                        {
                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            LogTraffic("RECV", $"Raw data ({bytesRead} bytes): {data}");

                            messageBuilder.Append(data);

                            // Process complete messages
                            string accumulated = messageBuilder.ToString();
                            int endIndex;

                            while ((endIndex = accumulated.IndexOf(MESSAGE_END)) != -1)
                            {
                                string message = accumulated.Substring(0, endIndex);
                                accumulated = accumulated.Substring(endIndex + MESSAGE_END.Length);

                                if (!string.IsNullOrEmpty(message))
                                {
                                    LogTraffic("RECV", $"Message: {message}");
                                    ProcessReceivedMessage(message);
                                }
                            }

                            messageBuilder.Clear();
                            messageBuilder.Append(accumulated);
                        }
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    Debug.LogError($"[Network] Receive error: {ex.Message}");
                    QueueMainThreadAction(() => {
                        OnError?.Invoke("Connection lost");
                        OnPlayerLeft?.Invoke();
                    });
                }
            }

            LogDebug("Receive thread ended");
        }

        private void ProcessReceivedMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                var msg = JsonUtility.FromJson<NetworkMessage>(message);

                QueueMainThreadAction(() => {
                    HandleMessage(msg);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Failed to parse message: {ex.Message}\nMessage: {message}");
            }
        }

        private void HandleMessage(NetworkMessage msg)
        {
            LogDebug($"Handling message type: {msg.type}");

            switch (msg.type)
            {
                case "character_selected":
                    LogDebug($"Opponent selected: {msg.data}");
                    OnOpponentCharacterSelected?.Invoke(msg.data);
                    break;

                case "turn_change":
                    bool isMyTurn = msg.data == "true";
                    LogDebug($"Turn change - my turn: {isMyTurn}");
                    OnTurnChanged?.Invoke(isMyTurn);
                    break;

                case "question":
                    try
                    {
                        var qData = JsonUtility.FromJson<QuestionData>(msg.data);
                        LogDebug($"Question received: {qData.questionText}");
                        OnQuestionReceived?.Invoke(qData.questionText, qData.expectedAnswer);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Network] Failed to parse question: {ex.Message}");
                    }
                    break;

                case "answer":
                    bool answer = msg.data == "true";
                    LogDebug($"Answer received: {answer}");
                    OnAnswerReceived?.Invoke(answer);
                    break;

                case "elimination":
                    try
                    {
                        var eData = JsonUtility.FromJson<EliminationData>(msg.data);
                        LogDebug($"Elimination: {eData.characterName} - {eData.eliminated}");
                        OnEliminationReceived?.Invoke(eData.characterName, eData.eliminated);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Network] Failed to parse elimination: {ex.Message}");
                    }
                    break;

                case "guess":
                    LogDebug($"Opponent guess: {msg.data}");
                    OnOpponentGuess?.Invoke(msg.data);
                    break;

                case "game_over":
                    try
                    {
                        var goData = JsonUtility.FromJson<GameOverData>(msg.data);
                        OnGameOver?.Invoke(goData.iWon, goData.characterId);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Network] Failed to parse game over: {ex.Message}");
                    }
                    break;
            }
        }

        private void SendMessage(NetworkMessage msg)
        {
            if (!isConnected || networkStream == null)
            {
                LogDebug("Cannot send - not connected");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(msg);
                string fullMessage = json + MESSAGE_END;
                byte[] data = Encoding.UTF8.GetBytes(fullMessage);

                LogTraffic("SEND", fullMessage);

                networkStream.Write(data, 0, data.Length);
                networkStream.Flush();

                LogDebug($"Sent message type: {msg.type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Send failed: {ex.Message}");
            }
        }

        private void QueueMainThreadAction(Action action)
        {
            lock (lockObject)
            {
                mainThreadActions.Enqueue(action);
            }
        }

        #endregion

        #region Send Methods

        public void SendCharacterSelected(string characterId)
        {
            if (!isConnected) return;

            LogDebug($"Sending character: {characterId}");
            SendMessage(new NetworkMessage
            {
                type = "character_selected",
                data = characterId
            });
        }

        public void SendTurnChange(bool isMyTurnNow)
        {
            if (!isConnected) return;

            LogDebug($"Sending turn change: {isMyTurnNow}");
            SendMessage(new NetworkMessage
            {
                type = "turn_change",
                data = isMyTurnNow ? "true" : "false"
            });
        }

        public void SendQuestion(string questionText, bool expectedAnswer)
        {
            if (!isConnected) return;

            LogDebug($"Sending question: {questionText}");
            SendMessage(new NetworkMessage
            {
                type = "question",
                data = JsonUtility.ToJson(new QuestionData
                {
                    questionText = questionText,
                    expectedAnswer = expectedAnswer
                })
            });
        }

        public void SendAnswer(bool answer)
        {
            if (!isConnected) return;

            LogDebug($"Sending answer: {answer}");
            SendMessage(new NetworkMessage
            {
                type = "answer",
                data = answer ? "true" : "false"
            });
        }

        public void SendElimination(string characterName, bool eliminated)
        {
            if (!isConnected) return;

            LogDebug($"Sending elimination: {characterName} - {eliminated}");
            SendMessage(new NetworkMessage
            {
                type = "elimination",
                data = JsonUtility.ToJson(new EliminationData
                {
                    characterName = characterName,
                    eliminated = eliminated
                })
            });
        }

        public void SendEliminationList(List<string> eliminatedIds)
        {
            if (!isConnected) return;

            LogDebug($"Sending elimination list: {eliminatedIds.Count} characters");
            foreach (var id in eliminatedIds)
            {
                SendElimination(id, true);
            }
        }

        public void SendGuess(string characterId)
        {
            if (!isConnected) return;

            LogDebug($"Sending guess: {characterId}");
            SendMessage(new NetworkMessage
            {
                type = "guess",
                data = characterId
            });
        }

        public void SendGameOver(bool iWon, string winnerCharacterId)
        {
            if (!isConnected) return;

            LogDebug($"Sending game over: {iWon}");
            SendMessage(new NetworkMessage
            {
                type = "game_over",
                data = JsonUtility.ToJson(new GameOverData
                {
                    iWon = iWon,
                    characterId = winnerCharacterId
                })
            });
        }

        #endregion

        #region Coroutines

        private IEnumerator WaitForPlayerToJoin()
        {
            float waitTime = 0f;
            while (waitTime < connectionTimeout && isConnected)
            {
                if (networkStream != null && tcpClient != null && tcpClient.Connected)
                {
                    yield break;
                }
                waitTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (isConnected && networkStream == null)
            {
                OnError?.Invoke("Connection timeout - no player joined");
                LeaveRoom();
            }
        }

        private IEnumerator NotifyConnectionReady()
        {
            yield return new WaitForSeconds(0.3f);
            OnPlayerJoined?.Invoke();
            isGameActive = true;
            OnGameReady?.Invoke();
        }

        private IEnumerator StartGameAfterConnection()
        {
            yield return new WaitForSeconds(0.3f);
            isGameActive = true;
            OnGameReady?.Invoke();
        }

        #endregion

        #region IP Encoding

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        private string EncodeIPToCode(string ip, int port)
        {
            string[] parts = ip.Split('.');
            if (parts.Length != 4)
            {
                Debug.LogError($"[Network] Invalid IP format: {ip}");
                return "000000";
            }

            int third = int.Parse(parts[2]);
            int fourth = int.Parse(parts[3]);

            string code = third.ToString("000") + fourth.ToString("000");

            LogDebug($"Encoded IP {ip} to code: {code}");
            return code;
        }

        private string DecodeCodeToIP(string code)
        {
            if (code.Length != 6)
            {
                Debug.LogError($"[Network] Invalid code length: {code}");
                return null;
            }

            try
            {
                int third = int.Parse(code.Substring(0, 3));
                int fourth = int.Parse(code.Substring(3, 3));

                string localIP = GetLocalIPAddress();
                string[] parts = localIP.Split('.');
                string subnetPrefix = parts[0] + "." + parts[1];

                string hostIP = $"{subnetPrefix}.{third}.{fourth}";

                LogDebug($"Decoded code {code} to IP: {hostIP}");
                return hostIP;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Failed to decode: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Data Structures

        [Serializable]
        private class NetworkMessage
        {
            public string type;
            public string data;
        }

        [Serializable]
        private class QuestionData
        {
            public string questionText;
            public bool expectedAnswer;
        }

        [Serializable]
        private class EliminationData
        {
            public string characterName;
            public bool eliminated;
        }

        [Serializable]
        private class GameOverData
        {
            public bool iWon;
            public string characterId;
        }

        #endregion
    }
}
