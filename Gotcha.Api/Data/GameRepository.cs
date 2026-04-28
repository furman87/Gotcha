using System.Data;
using Dapper;
using Gotcha.Api.Models;
using Npgsql;
using System.Text.Json;

namespace Gotcha.Api.Data;

public class GameRepository
{
    private readonly string _connectionString;

    public GameRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task<Game> CreateGameAsync(Guid id, int guessesAllowed, int bonusGuesses)
    {
        using var conn = CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            "INSERT INTO games (id, word, status, guesses_allowed, bonus_guesses) VALUES (@Id, '', 'waiting', @GuessesAllowed, @BonusGuesses)",
            new { Id = id, GuessesAllowed = guessesAllowed, BonusGuesses = bonusGuesses });

        return new Game
        {
            Id = id,
            Word = "",
            Status = "waiting",
            GuessesAllowed = guessesAllowed,
            BonusGuesses = bonusGuesses,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<Game?> GetGameAsync(Guid id)
    {
        using var conn = CreateConnection();
        conn.Open();

        var game = await conn.QueryFirstOrDefaultAsync<Game>(
            "SELECT id, word, previous_word as PreviousWord, status, guesses_allowed as GuessesAllowed, bonus_guesses as BonusGuesses, swap_used as SwapUsed, swap_occurred_after_guess as SwapOccurredAfterGuess, created_at as CreatedAt, ended_at as EndedAt FROM games WHERE id = @Id",
            new { Id = id });

        if (game == null) return null;

        var guessRows = await conn.QueryAsync<GuessRow>(
            "SELECT id, game_id AS GameId, guess_number AS GuessNumber, word, result::text AS Result, submitted_at AS SubmittedAt FROM guesses WHERE game_id = @GameId ORDER BY guess_number",
            new { GameId = id });

        game.Guesses = guessRows.Select(g => new Guess
        {
            Id = g.Id,
            GameId = g.GameId,
            GuessNumber = g.GuessNumber,
            Word = g.Word,
            Result = JsonSerializer.Deserialize<List<LetterResult>>(g.Result) ?? new List<LetterResult>(),
            SubmittedAt = g.SubmittedAt
        }).ToList();

        return game;
    }

    private class GuessRow
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public int GuessNumber { get; set; }
        public string Word { get; set; } = "";
        public string Result { get; set; } = "[]";
        public DateTime SubmittedAt { get; set; }
    }

    public async Task SetWordAsync(Guid gameId, string word, int bonusGuesses)
    {
        using var conn = CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            "UPDATE games SET word = @Word, status = 'active', bonus_guesses = @BonusGuesses WHERE id = @Id AND status = 'waiting'",
            new { Word = word, BonusGuesses = bonusGuesses, Id = gameId });
    }

    public async Task AddGuessAsync(Guid gameId, int guessNumber, string word, List<LetterResult> result)
    {
        using var conn = CreateConnection();
        conn.Open();
        var resultJson = JsonSerializer.Serialize(result);
        await conn.ExecuteAsync(
            "INSERT INTO guesses (id, game_id, guess_number, word, result) VALUES (gen_random_uuid(), @GameId, @GuessNumber, @Word, @Result::jsonb)",
            new { GameId = gameId, GuessNumber = guessNumber, Word = word, Result = resultJson });
    }

    public async Task UpdateGuessResultAsync(Guid gameId, int guessNumber, List<LetterResult> result)
    {
        using var conn = CreateConnection();
        conn.Open();
        var resultJson = JsonSerializer.Serialize(result);
        await conn.ExecuteAsync(
            "UPDATE guesses SET result = @Result::jsonb WHERE game_id = @GameId AND guess_number = @GuessNumber",
            new { GameId = gameId, GuessNumber = guessNumber, Result = resultJson });
    }

    public async Task EndGameAsync(Guid gameId, string status)
    {
        using var conn = CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            "UPDATE games SET status = @Status, ended_at = now() WHERE id = @Id",
            new { Status = status, Id = gameId });
    }

    public async Task SwapWordAsync(Guid gameId, string newWord, string previousWord, int afterGuessNumber)
    {
        using var conn = CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            "UPDATE games SET word = @NewWord, previous_word = @PreviousWord, swap_used = TRUE, swap_occurred_after_guess = @AfterGuessNumber WHERE id = @Id",
            new { NewWord = newWord, PreviousWord = previousWord, AfterGuessNumber = afterGuessNumber, Id = gameId });
    }
}
