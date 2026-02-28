public enum GameState
{
    CharacterSelection, // Player and AI Choose a character
    PlayerTurn,         // PLayer asks his question
    AiTurn,             // AI Asks a question
    Guessing,           // AI or Player do their guess Or Final Guess
    GameOver            // Game has eneded (Loss or Win)
}