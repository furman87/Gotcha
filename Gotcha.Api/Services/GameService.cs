using Gotcha.Api.Models;

namespace Gotcha.Api.Services;

public class GameService
{
    private readonly GuessEvaluationService _evaluator;

    public GameService(GuessEvaluationService evaluator)
    {
        _evaluator = evaluator;
    }

    public (bool valid, string? error) ValidateHardMode(List<Guess> previousGuesses, string newGuessWord)
    {
        var guess = newGuessWord.ToLowerInvariant();

        // Collect green constraints: position -> letter
        var greenConstraints = new Dictionary<int, char>();
        // Collect yellow constraints: letter -> required count
        var yellowLetters = new HashSet<char>();

        foreach (var prev in previousGuesses)
        {
            for (int i = 0; i < prev.Result.Count; i++)
            {
                var r = prev.Result[i];
                if (r.Status == "green")
                    greenConstraints[i] = r.Letter;
                else if (r.Status == "yellow")
                    yellowLetters.Add(r.Letter);
            }
        }

        foreach (var (pos, letter) in greenConstraints)
        {
            if (guess[pos] != letter)
                return (false, $"Position {pos + 1} must be '{char.ToUpper(letter)}'");
        }

        foreach (var letter in yellowLetters)
        {
            if (!guess.Contains(letter))
                return (false, $"Guess must contain '{char.ToUpper(letter)}'");
        }

        return (true, null);
    }

    public (bool valid, string? error) ValidateSwap(List<Guess> previousGuesses, string newWord)
    {
        var word = newWord.ToLowerInvariant();

        // Same constraints as hard mode but applied to the new TARGET word
        var greenConstraints = new Dictionary<int, char>();
        var yellowLetters = new HashSet<char>();

        foreach (var prev in previousGuesses)
        {
            for (int i = 0; i < prev.Result.Count; i++)
            {
                var r = prev.Result[i];
                if (r.Status == "green")
                    greenConstraints[i] = r.Letter;
                else if (r.Status == "yellow")
                    yellowLetters.Add(r.Letter);
            }
        }

        foreach (var (pos, letter) in greenConstraints)
        {
            if (word[pos] != letter)
                return (false, $"New word must have '{char.ToUpper(letter)}' at position {pos + 1}");
        }

        foreach (var letter in yellowLetters)
        {
            if (!word.Contains(letter))
                return (false, $"New word must contain '{char.ToUpper(letter)}'");
        }

        return (true, null);
    }

    public GameStateResponse BuildGameState(Game game, bool setter = false)
    {
        var isOver = game.Status is "won" or "lost";

        // Total guesses available = original + bonus if swap happened
        var totalGuessesAllowed = game.GuessesAllowed + (game.SwapUsed ? game.BonusGuesses : 0);

        return new GameStateResponse
        {
            GameId = game.Id,
            Status = game.Status,
            GuessesAllowed = totalGuessesAllowed,
            BonusGuesses = game.BonusGuesses,
            SwapUsed = game.SwapUsed,
            SwapOccurredAfterGuess = game.SwapOccurredAfterGuess,
            Guesses = game.Guesses.Select(g => new GuessResponse
            {
                GuessNumber = g.GuessNumber,
                Word = g.Word,
                Result = g.Result
            }).ToList(),
            WordLength = 5,
            IsOver = isOver,
            Winner = isOver ? game.Status == "won" ? "guesser" : "setter" : null,
            Answer = isOver ? game.Word : null,
            SetterWord = setter ? game.Word : null
        };
    }
}
