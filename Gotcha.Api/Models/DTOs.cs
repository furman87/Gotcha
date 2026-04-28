namespace Gotcha.Api.Models;

public record CreateGameResponse(Guid GameId, string SetUrl, string GuessUrl);

public record SetWordRequest(string Word, int BonusGuesses = 3);

public record SubmitGuessRequest(string Word);

public record SwapWordRequest(string Word);

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
    public string? Winner { get; set; } // "guesser" | "setter" | null
    public string? Answer { get; set; } // only when IsOver = true
    public string? SetterWord { get; set; } // only populated when setter=true
}

public class GuessResponse
{
    public int GuessNumber { get; set; }
    public string Word { get; set; } = "";
    public List<LetterResult> Result { get; set; } = new();
}

public record ErrorResponse(string Error);
