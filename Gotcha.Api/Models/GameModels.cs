namespace Gotcha.Api.Models;

public class Game
{
    public Guid Id { get; set; }
    public string Word { get; set; } = "";
    public string? PreviousWord { get; set; }
    public string Status { get; set; } = "waiting"; // waiting | active | won | lost
    public int GuessesAllowed { get; set; } = 6;
    public int BonusGuesses { get; set; } = 3;
    public bool SwapUsed { get; set; }
    public int? SwapOccurredAfterGuess { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public List<Guess> Guesses { get; set; } = new();
}

public class Guess
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public int GuessNumber { get; set; }
    public string Word { get; set; } = "";
    public List<LetterResult> Result { get; set; } = new();
    public DateTime SubmittedAt { get; set; }
}

public class LetterResult
{
    public char Letter { get; set; }
    public string Status { get; set; } = "gray"; // green | yellow | gray
}
