namespace Gotcha.Client.Models;

public class LetterResult
{
    public char Letter { get; set; }
    public string Status { get; set; } = "gray";
}

public class GuessResponse
{
    public int GuessNumber { get; set; }
    public string Word { get; set; } = "";
    public List<LetterResult> Result { get; set; } = new();
}

public class GameStateResponse
{
    public Guid GameId { get; set; }
    public string Status { get; set; } = "";
    public int GuessesAllowed { get; set; }
    public int BonusGuesses { get; set; }
    public bool SwapUsed { get; set; }
    public int? SwapOccurredAfterGuess { get; set; }
    public List<GuessResponse> Guesses { get; set; } = new();
    public int WordLength { get; set; } = 5;
    public bool IsOver { get; set; }
    public string? Winner { get; set; }
    public string? Answer { get; set; }
    public string? SetterWord { get; set; }
}

public class CreateGameResponse
{
    public Guid GameId { get; set; }
    public string SetUrl { get; set; } = "";
    public string GuessUrl { get; set; } = "";
}

public class ErrorResponse
{
    public string Error { get; set; } = "";
}
