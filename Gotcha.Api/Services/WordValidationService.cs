using Gotcha.Api.Models;
using Microsoft.Extensions.Options;

namespace Gotcha.Api.Services;

public class WordValidationService
{
    private readonly HashSet<string> _answers;
    private readonly HashSet<string> _validGuesses;
    private readonly GameSettings _settings;

    public WordValidationService(IOptions<GameSettings> settings, IWebHostEnvironment env)
    {
        _settings = settings.Value;
        _answers = LoadWordList(Path.Combine(env.WebRootPath, "wordlists", "answers.txt"));
        _validGuesses = LoadWordList(Path.Combine(env.WebRootPath, "wordlists", "guesses.txt"));
        // Valid guesses includes all answers too
        foreach (var w in _answers) _validGuesses.Add(w);
    }

    private static HashSet<string> LoadWordList(string path)
    {
        if (!File.Exists(path)) return new HashSet<string>();
        return File.ReadAllLines(path)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length == 5 && w.All(char.IsLetter))
            .ToHashSet();
    }

    public bool IsValidAnswer(string word) => _answers.Contains(word.ToLowerInvariant());
    public bool IsValidGuess(string word) => _validGuesses.Contains(word.ToLowerInvariant());

    public HashSet<string> GetAnswers() => _answers;
    public HashSet<string> GetValidGuesses() => _validGuesses;
}
