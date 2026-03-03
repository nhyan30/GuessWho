using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Manages all network communication for multiplayer games.
    /// Handles room creation, joining, and game state synchronization.
    /// In production, replace simulation methods with actual network transport (Photon, Mirror, etc.)
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int codeLength = 6;
        [SerializeField] private float connectionTimeout = 30f;
        [SerializeField] private float heartbeatInterval = 5f;

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
        public event Action<List<string>> OnEliminationUpdate;
        public event Action<string> OnOpponentGuess;
        public event Action<bool, string> OnGameOver;

        // State
        private string currentRoomCode;
        private bool isHost = false;
        private bool isConnected = false;
        private bool isGameActive = false;
        private string localPlayerId;
        private string remotePlayerId;

        // Simulated network storage (replace with server in production)
        private static Dictionary<string, RoomData> activeRooms = new Dictionary<string, RoomData>();
        private static Dictionary<string, List<Action>> pendingMessages = new Dictionary<string, List<Action>>();

        #region Properties
        public bool IsHost => isHost;
        public bool IsConnected => isConnected;
        public string CurrentRoomCode => currentRoomCode;
        public string LocalPlayerId => localPlayerId;
        public string RemotePlayerId => remotePlayerId;
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
            }

            GenerateLocalPlayerId();
        }

        private void GenerateLocalPlayerId()
        {
            localPlayerId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(localPlayerId) || localPlayerId == "00000000000000000000000000000000")
            {
                localPlayerId = Guid.NewGuid().ToString();
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
        /// Creates a new game room and returns the room code.
        /// </summary>
        public void CreateRoom()
        {
            currentRoomCode = GenerateRoomCode();
            isHost = true;
            isConnected = true;
            isGameActive = false;

            RoomData room = new RoomData
            {
                roomCode = currentRoomCode,
                hostId = localPlayerId,
                hostReady = true,
                guestReady = false,
                hostCharacterId = null,
                guestCharacterId = null,
                createdAt = DateTime.UtcNow
            };

            activeRooms[currentRoomCode] = room;
            pendingMessages[localPlayerId] = new List<Action>();

            Debug.Log($"[Network] Room created: {currentRoomCode}");
            OnRoomCreated?.Invoke(currentRoomCode);

            StartCoroutine(WaitForPlayerToJoin());
        }

        /// <summary>
        /// Attempts to join an existing room.
        /// </summary>
        public void JoinRoom(string roomCode)
        {
            currentRoomCode = roomCode.ToUpper().Trim();

            if (!activeRooms.ContainsKey(currentRoomCode))
            {
                OnError?.Invoke("Room not found");
                OnRoomJoined?.Invoke(false);
                return;
            }

            RoomData room = activeRooms[currentRoomCode];

            if (room.guestReady)
            {
                OnError?.Invoke("Room is full");
                OnRoomJoined?.Invoke(false);
                return;
            }

            isHost = false;
            isConnected = true;
            isGameActive = false;
            remotePlayerId = room.hostId;
            room.guestId = localPlayerId;
            room.guestReady = true;
            room.joinedAt = DateTime.UtcNow;

            activeRooms[currentRoomCode] = room;
            pendingMessages[localPlayerId] = new List<Action>();

            Debug.Log($"[Network] Joined room: {currentRoomCode}");
            OnRoomJoined?.Invoke(true);

            // Notify host
            StartCoroutine(NotifyHostPlayerJoined(room));
        }

        /// <summary>
        /// Leaves the current room.
        /// </summary>
        public void LeaveRoom()
        {
            if (!string.IsNullOrEmpty(currentRoomCode))
            {
                if (activeRooms.ContainsKey(currentRoomCode))
                {
                    if (isHost)
                    {
                        activeRooms.Remove(currentRoomCode);
                    }
                    else
                    {
                        RoomData room = activeRooms[currentRoomCode];
                        room.guestReady = false;
                        room.guestId = null;
                        activeRooms[currentRoomCode] = room;
                    }
                }

                if (pendingMessages.ContainsKey(localPlayerId))
                {
                    pendingMessages.Remove(localPlayerId);
                }
            }

            currentRoomCode = null;
            isHost = false;
            isConnected = false;
            isGameActive = false;
            remotePlayerId = null;

            Debug.Log("[Network] Left room");
        }

        private string GenerateRoomCode()
        {
            const string chars = "0123456789";
            System.Random random = new System.Random();

            string code;
            int attempts = 0;
            do
            {
                code = "";
                for (int i = 0; i < codeLength; i++)
                {
                    code += chars[random.Next(chars.Length)];
                }
                attempts++;
            } while (activeRooms.ContainsKey(code) && attempts < 100);

            return code;
        }

        private IEnumerator WaitForPlayerToJoin()
        {
            float waitTime = 0f;
            while (waitTime < connectionTimeout && isConnected)
            {
                if (activeRooms.ContainsKey(currentRoomCode))
                {
                    RoomData room = activeRooms[currentRoomCode];
                    if (room.guestReady && !string.IsNullOrEmpty(room.guestId))
                    {
                        remotePlayerId = room.guestId;
                        Debug.Log("[Network] Player joined!");
                        OnPlayerJoined?.Invoke();

                        yield return new WaitForSeconds(0.3f);
                        isGameActive = true;
                        OnGameReady?.Invoke();
                        yield break;
                    }
                }

                waitTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (isConnected)
            {
                OnError?.Invoke("Connection timeout - no player joined");
                LeaveRoom();
            }
        }

        private IEnumerator NotifyHostPlayerJoined(RoomData room)
        {
            yield return new WaitForSeconds(0.2f);
            OnPlayerJoined?.Invoke();

            yield return new WaitForSeconds(0.2f);
            isGameActive = true;
            OnGameReady?.Invoke();
        }

        #endregion

        #region Send Methods

        /// <summary>
        /// Sends character selection to opponent.
        /// </summary>
        public void SendCharacterSelected(string characterId)
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomCode)) return;

            RoomData room = activeRooms[currentRoomCode];
            if (isHost)
                room.hostCharacterId = characterId;
            else
                room.guestCharacterId = characterId;
            activeRooms[currentRoomCode] = room;

            Debug.Log($"[Network] Character selected: {characterId}");

            // Simulate sending to opponent
            StartCoroutine(DelayedMessage(() =>
            {
                OnOpponentCharacterSelected?.Invoke(characterId);
            }, 0.1f));
        }

        /// <summary>
        /// Sends turn change notification.
        /// </summary>
        public void SendTurnChange(bool isMyTurnNow)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Turn change - my turn: {isMyTurnNow}");
            OnTurnChanged?.Invoke(isMyTurnNow);
        }

        /// <summary>
        /// Sends a question to opponent.
        /// </summary>
        public void SendQuestion(string questionId, bool expectedAnswer)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Question sent: {questionId}");

            StartCoroutine(DelayedMessage(() =>
            {
                OnQuestionReceived?.Invoke(questionId, expectedAnswer);
            }, 0.15f));
        }

        /// <summary>
        /// Sends answer to question.
        /// </summary>
        public void SendAnswer(bool answer)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Answer sent: {answer}");

            StartCoroutine(DelayedMessage(() =>
            {
                OnAnswerReceived?.Invoke(answer);
            }, 0.1f));
        }

        /// <summary>
        /// Sends elimination update.
        /// </summary>
        public void SendElimination(List<string> eliminatedIds)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Elimination: {eliminatedIds.Count} characters");

            StartCoroutine(DelayedMessage(() =>
            {
                OnEliminationUpdate?.Invoke(new List<string>(eliminatedIds));
            }, 0.1f));
        }

        /// <summary>
        /// Sends guess to opponent.
        /// </summary>
        public void SendGuess(string characterId)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Guess: {characterId}");

            StartCoroutine(DelayedMessage(() =>
            {
                OnOpponentGuess?.Invoke(characterId);
            }, 0.15f));
        }

        /// <summary>
        /// Sends game over notification.
        /// </summary>
        public void SendGameOver(bool iWon, string winnerCharacterId)
        {
            if (!isConnected) return;

            Debug.Log($"[Network] Game over - won: {iWon}");

            StartCoroutine(DelayedMessage(() =>
            {
                OnGameOver?.Invoke(iWon, winnerCharacterId);
            }, 0.2f));
        }

        private IEnumerator DelayedMessage(Action message, float delay)
        {
            yield return new WaitForSeconds(delay);
            message?.Invoke();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets opponent's selected character ID.
        /// </summary>
        public string GetOpponentCharacterId()
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomCode)) return null;

            RoomData room = activeRooms[currentRoomCode];
            return isHost ? room.guestCharacterId : room.hostCharacterId;
        }

        /// <summary>
        /// Checks if both players have selected characters.
        /// </summary>
        public bool AreBothCharactersSelected()
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomCode)) return false;

            RoomData room = activeRooms[currentRoomCode];
            return !string.IsNullOrEmpty(room.hostCharacterId) && 
                   !string.IsNullOrEmpty(room.guestCharacterId);
        }

        #endregion

        #region Data Structures

        [Serializable]
        private struct RoomData
        {
            public string roomCode;
            public string hostId;
            public string guestId;
            public bool hostReady;
            public bool guestReady;
            public string hostCharacterId;
            public string guestCharacterId;
            public DateTime createdAt;
            public DateTime joinedAt;
        }

        #endregion
    }
}
