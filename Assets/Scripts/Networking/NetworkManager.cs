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
        public event Action<string, bool> OnEliminationReceived; // character name, should eliminate
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
        private byte[] receiveBuffer = new byte[BUFFER_SIZE];

        // Message framing
        private StringBuilder messageBuilder = new StringBuilder();
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

        #endregion

        #region Room Management

        /// <summary>
        /// Creates a new game room and starts listening for connections.
        /// </summary>
        public void CreateRoom()
        {
            try
            {
                // Get local IP address
                string localIP = GetLocalIPAddress();
                HostIPAddress = localIP;

                // Start TCP listener
                tcpListener = new TcpListener(IPAddress.Any, defaultPort);
                tcpListener.Start();
                isRunning = true;

                // Generate room code from IP address
                currentRoomCode = EncodeIPToCode(localIP, defaultPort);
                isHost = true;
                isConnected = true;
                isGameActive = false;

                Debug.Log($"[Network] Room created: {currentRoomCode} (IP: {localIP}:{defaultPort})");
                OnRoomCreated?.Invoke(currentRoomCode);

                // Start listening for connections in background thread
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

        /// <summary>
        /// Attempts to join an existing room using the room code.
        /// </summary>
        public void JoinRoom(string roomCode)
        {
            try
            {
                currentRoomCode = roomCode.ToUpper().Trim();

                // Decode IP address from room code
                string hostIP = DecodeCodeToIP(currentRoomCode);

                if (string.IsNullOrEmpty(hostIP))
                {
                    OnError?.Invoke("Invalid room code");
                    OnRoomJoined?.Invoke(false);
                    return;
                }

                Debug.Log($"[Network] Connecting to {hostIP}:{defaultPort}...");

                // Connect to host
                tcpClient = new TcpClient();
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
                isGameActive = false;
                networkStream = tcpClient.GetStream();

                Debug.Log($"[Network] Connected to host!");
                OnRoomJoined?.Invoke(true);

                // Start receiving messages
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                // Notify game that connection is ready
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

        /// <summary>
        /// Leaves the current room and closes all connections.
        /// </summary>
        public void LeaveRoom()
        {
            isRunning = false;
            isConnected = false;
            isGameActive = false;

            // Close network stream
            if (networkStream != null)
            {
                try { networkStream.Close(); } catch { }
                networkStream = null;
            }

            // Close TCP client
            if (tcpClient != null)
            {
                try { tcpClient.Close(); } catch { }
                tcpClient = null;
            }

            // Stop TCP listener
            if (tcpListener != null)
            {
                try { tcpListener.Stop(); } catch { }
                tcpListener = null;
            }

            // Wait for threads to finish
            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(500);
            }
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(500);
            }

            currentRoomCode = null;
            isHost = false;

            Debug.Log("[Network] Left room and closed all connections");
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
                        networkStream = tcpClient.GetStream();

                        Debug.Log("[Network] Player connected!");

                        // Notify main thread
                        QueueMainThreadAction(() => {
                            OnPlayerJoined?.Invoke();
                            StartCoroutine(StartGameAfterConnection());
                        });

                        // Start receiving messages
                        receiveThread = new Thread(ReceiveMessages);
                        receiveThread.IsBackground = true;
                        receiveThread.Start();

                        break; // Only accept one connection
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    Debug.LogError($"[Network] Listener error: {ex.Message}");
                }
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[BUFFER_SIZE];

                while (isRunning && networkStream != null && tcpClient != null && tcpClient.Connected)
                {
                    if (networkStream.DataAvailable)
                    {
                        int bytesRead = networkStream.Read(buffer, 0, BUFFER_SIZE);
                        if (bytesRead > 0)
                        {
                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            ProcessReceivedData(data);
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
        }

        private void ProcessReceivedData(string data)
        {
            // Add to message builder
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
                    ProcessReceivedMessage(message);
                }
            }

            messageBuilder.Clear();
            messageBuilder.Append(accumulated);
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
            switch (msg.type)
            {
                case "character_selected":
                    Debug.Log($"[Network] Opponent selected: {msg.data}");
                    OnOpponentCharacterSelected?.Invoke(msg.data);
                    break;

                case "turn_change":
                    bool isMyTurn = msg.data == "true";
                    Debug.Log($"[Network] Turn change - my turn: {isMyTurn}");
                    OnTurnChanged?.Invoke(isMyTurn);
                    break;

                case "question":
                    try
                    {
                        var qData = JsonUtility.FromJson<QuestionData>(msg.data);
                        Debug.Log($"[Network] Question received: {qData.questionText}");
                        OnQuestionReceived?.Invoke(qData.questionText, qData.expectedAnswer);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Network] Failed to parse question: {ex.Message}");
                    }
                    break;

                case "answer":
                    bool answer = msg.data == "true";
                    Debug.Log($"[Network] Answer received: {answer}");
                    OnAnswerReceived?.Invoke(answer);
                    break;

                case "elimination":
                    try
                    {
                        var eData = JsonUtility.FromJson<EliminationData>(msg.data);
                        Debug.Log($"[Network] Elimination: {eData.characterName} - {eData.eliminated}");
                        OnEliminationReceived?.Invoke(eData.characterName, eData.eliminated);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Network] Failed to parse elimination: {ex.Message}");
                    }
                    break;

                case "guess":
                    Debug.Log($"[Network] Opponent guess: {msg.data}");
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
            if (!isConnected || networkStream == null) return;

            try
            {
                string json = JsonUtility.ToJson(msg);
                json += MESSAGE_END; // Add message end marker
                byte[] data = Encoding.UTF8.GetBytes(json);
                networkStream.Write(data, 0, data.Length);
                networkStream.Flush();
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

        /// <summary>
        /// Sends character selection to opponent.
        /// </summary>
        public void SendCharacterSelected(string characterId)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending character: {characterId}");
            SendMessage(new NetworkMessage
            {
                type = "character_selected",
                data = characterId
            });
        }

        /// <summary>
        /// Sends turn change notification.
        /// </summary>
        public void SendTurnChange(bool isMyTurnNow)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending turn change: {isMyTurnNow}");
            SendMessage(new NetworkMessage
            {
                type = "turn_change",
                data = isMyTurnNow ? "true" : "false"
            });
        }

        /// <summary>
        /// Sends a question to opponent.
        /// </summary>
        public void SendQuestion(string questionText, bool expectedAnswer)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending question: {questionText}");
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

        /// <summary>
        /// Sends answer to question.
        /// </summary>
        public void SendAnswer(bool answer)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending answer: {answer}");
            SendMessage(new NetworkMessage
            {
                type = "answer",
                data = answer ? "true" : "false"
            });
        }

        /// <summary>
        /// Sends elimination update for a single character.
        /// </summary>
        public void SendElimination(string characterName, bool eliminated)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending elimination: {characterName} - {eliminated}");
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

        /// <summary>
        /// Sends elimination update for multiple characters.
        /// </summary>
        public void SendEliminationList(List<string> eliminatedIds)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending elimination list: {eliminatedIds.Count} characters");
            foreach (var id in eliminatedIds)
            {
                SendElimination(id, true);
            }
        }

        /// <summary>
        /// Sends guess to opponent.
        /// </summary>
        public void SendGuess(string characterId)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending guess: {characterId}");
            SendMessage(new NetworkMessage
            {
                type = "guess",
                data = characterId
            });
        }

        /// <summary>
        /// Sends game over notification.
        /// </summary>
        public void SendGameOver(bool iWon, string winnerCharacterId)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Sending game over: {iWon}");
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
                    yield break; // Connection established
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

        /// <summary>
        /// Gets the local IP address of this device.
        /// </summary>
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

        /// <summary>
        /// Encodes an IP address and port to a 6-digit numeric code.
        /// </summary>
        private string EncodeIPToCode(string ip, int port)
        {
            // Parse IP octets
            string[] parts = ip.Split('.');
            if (parts.Length != 4)
            {
                Debug.LogError($"[Network] Invalid IP format: {ip}");
                return "000000";
            }

            // Use last two octets to create a 6-digit code
            // This works for most local networks (192.168.x.x)
            int third = int.Parse(parts[2]);
            int fourth = int.Parse(parts[3]);

            // Encode: third octet (3 digits) + fourth octet (3 digits)
            string code = third.ToString("000") + fourth.ToString("000");

            Debug.Log($"[Network] Encoded IP {ip} to code: {code}");
            return code;
        }

        /// <summary>
        /// Decodes a 6-digit code back to an IP address.
        /// Requires the host to be on the same subnet.
        /// </summary>
        private string DecodeCodeToIP(string code)
        {
            if (code.Length != 6)
            {
                Debug.LogError($"[Network] Invalid code length: {code}");
                return null;
            }

            try
            {
                // Parse the code
                int third = int.Parse(code.Substring(0, 3));
                int fourth = int.Parse(code.Substring(3, 3));

                // Get local subnet prefix
                string localIP = GetLocalIPAddress();
                string[] parts = localIP.Split('.');
                string subnetPrefix = parts[0] + "." + parts[1];

                // Reconstruct host IP
                string hostIP = $"{subnetPrefix}.{third}.{fourth}";

                Debug.Log($"[Network] Decoded code {code} to IP: {hostIP}");
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
