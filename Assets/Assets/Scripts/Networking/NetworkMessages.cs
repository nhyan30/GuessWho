using System;
using System.Collections.Generic;

namespace Networking
{
    /// <summary>
    /// Defines all message types for network communication.
    /// </summary>
    public enum MessageType : byte
    {
        // Connection
        PlayerJoined = 1,
        PlayerLeft = 2,
        GameReady = 3,

        // Character Selection
        CharacterSelected = 10,
        CharacterConfirmed = 11,

        // Game Flow
        TurnChanged = 20,
        QuestionAsked = 21,
        QuestionAnswered = 22,
        CharacterEliminated = 23,

        // Guessing
        GuessMade = 30,
        GuessResult = 31,

        // Game Over
        GameOver = 40,

        // Room
        RoomCreated = 50,
        RoomJoined = 51,
        RoomNotFound = 52,
        RoomFull = 53
    }

    /// <summary>
    /// Base class for all network messages.
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public MessageType type;
        public int senderId;
        public long timestamp;

        public NetworkMessage(MessageType type)
        {
            this.type = type;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Message for room creation/joining.
    /// </summary>
    [Serializable]
    public class RoomMessage : NetworkMessage
    {
        public string roomCode;
        public string playerName;

        public RoomMessage(MessageType type, string roomCode, string playerName = "") : base(type)
        {
            this.roomCode = roomCode;
            this.playerName = playerName;
        }
    }

    /// <summary>
    /// Message for character selection.
    /// </summary>
    [Serializable]
    public class CharacterMessage : NetworkMessage
    {
        public string characterId;

        public CharacterMessage(MessageType type, string characterId) : base(type)
        {
            this.characterId = characterId;
        }
    }

    /// <summary>
    /// Message for questions.
    /// </summary>
    [Serializable]
    public class QuestionMessage : NetworkMessage
    {
        public string questionId;
        public bool answer;

        public QuestionMessage(MessageType type, string questionId, bool answer = false) : base(type)
        {
            this.questionId = questionId;
            this.answer = answer;
        }
    }

    /// <summary>
    /// Message for elimination updates.
    /// </summary>
    [Serializable]
    public class EliminationMessage : NetworkMessage
    {
        public List<string> eliminatedCharacterIds;
        public string questionId;
        public bool answer;

        public EliminationMessage(List<string> eliminatedIds, string questionId, bool answer) : base(MessageType.CharacterEliminated)
        {
            this.eliminatedCharacterIds = eliminatedIds;
            this.questionId = questionId;
            this.answer = answer;
        }
    }

    /// <summary>
    /// Message for guesses.
    /// </summary>
    [Serializable]
    public class GuessMessage : NetworkMessage
    {
        public string characterId;
        public bool isCorrect;

        public GuessMessage(MessageType type, string characterId, bool isCorrect = false) : base(type)
        {
            this.characterId = characterId;
            this.isCorrect = isCorrect;
        }
    }

    /// <summary>
    /// Message for turn changes.
    /// </summary>
    [Serializable]
    public class TurnMessage : NetworkMessage
    {
        public bool isMyTurn;

        public TurnMessage(bool isMyTurn) : base(MessageType.TurnChanged)
        {
            this.isMyTurn = isMyTurn;
        }
    }

    /// <summary>
    /// Message for game over.
    /// </summary>
    [Serializable]
    public class GameOverMessage : NetworkMessage
    {
        public bool iWon;
        public string winnerCharacterId;

        public GameOverMessage(bool iWon, string winnerCharacterId) : base(MessageType.GameOver)
        {
            this.iWon = iWon;
            this.winnerCharacterId = winnerCharacterId;
        }
    }
}
