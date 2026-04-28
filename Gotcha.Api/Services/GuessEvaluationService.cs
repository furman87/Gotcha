using Gotcha.Api.Models;

namespace Gotcha.Api.Services;

public class GuessEvaluationService
{
    public List<LetterResult> EvaluateGuess(string targetWord, string guessWord)
    {
        var target = targetWord.ToLowerInvariant().ToCharArray();
        var guess = guessWord.ToLowerInvariant().ToCharArray();
        var result = new LetterResult[5];
        var targetRemaining = new int[26]; // counts of unmatched target letters

        // Pass 1: Mark greens
        for (int i = 0; i < 5; i++)
        {
            if (guess[i] == target[i])
            {
                result[i] = new LetterResult { Letter = guess[i], Status = "green" };
            }
            else
            {
                targetRemaining[target[i] - 'a']++;
            }
        }

        // Pass 2: Mark yellows and grays
        for (int i = 0; i < 5; i++)
        {
            if (result[i] != null) continue; // already green
            var c = guess[i] - 'a';
            if (targetRemaining[c] > 0)
            {
                result[i] = new LetterResult { Letter = guess[i], Status = "yellow" };
                targetRemaining[c]--;
            }
            else
            {
                result[i] = new LetterResult { Letter = guess[i], Status = "gray" };
            }
        }

        return result.ToList();
    }

    public List<List<LetterResult>> ReEvaluateGuesses(List<string> guessWords, string newWord)
    {
        return guessWords.Select(w => EvaluateGuess(newWord, w)).ToList();
    }
}
