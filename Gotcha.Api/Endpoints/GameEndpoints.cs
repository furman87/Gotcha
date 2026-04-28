using Gotcha.Api.Data;
using Gotcha.Api.Models;
using Gotcha.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Gotcha.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/games");

        // POST /api/games - Create a new game
        api.MapPost("/", async (
            [FromServices] GameRepository repo,
            [FromServices] IOptions<GameSettings> settings,
            IConfiguration config,
            HttpRequest request) =>
        {
            var gameId = Guid.NewGuid();
            var s = settings.Value;
            await repo.CreateGameAsync(gameId, s.DefaultGuessesAllowed, s.DefaultBonusGuessesOnSwap);

            var clientBase = config["ClientBaseUrl"]?.TrimEnd('/')
                ?? $"{request.Scheme}://{request.Host}";
            return Results.Ok(new CreateGameResponse(
                gameId,
                $"{clientBase}/game/{gameId}/set",
                $"{clientBase}/game/{gameId}/guess"
            ));
        });

        // GET /api/games/{gameId} - Get game state
        api.MapGet("/{gameId:guid}", async (
            Guid gameId,
            bool? setter,
            [FromServices] GameRepository repo,
            [FromServices] GameService gameSvc) =>
        {
            var game = await repo.GetGameAsync(gameId);
            if (game == null) return Results.NotFound(new ErrorResponse("Game not found"));
            return Results.Ok(gameSvc.BuildGameState(game, setter == true));
        });

        // POST /api/games/{gameId}/word - Setter sets initial word
        api.MapPost("/{gameId:guid}/word", async (
            Guid gameId,
            [FromBody] SetWordRequest req,
            [FromServices] GameRepository repo,
            [FromServices] WordValidationService wordSvc) =>
        {
            var game = await repo.GetGameAsync(gameId);
            if (game == null) return Results.NotFound(new ErrorResponse("Game not found"));
            if (game.Status != "waiting") return Results.Conflict(new ErrorResponse("Word already set"));

            var word = req.Word.Trim().ToLowerInvariant();
            if (word.Length != 5) return Results.BadRequest(new ErrorResponse("Word must be 5 letters"));
            if (!wordSvc.IsValidAnswer(word)) return Results.BadRequest(new ErrorResponse("Not a valid word"));

            if (req.BonusGuesses < 0 || req.BonusGuesses > 9)
                return Results.BadRequest(new ErrorResponse("BonusGuesses must be between 0 and 9"));

            await repo.SetWordAsync(gameId, word, req.BonusGuesses);
            return Results.Ok();
        });

        // POST /api/games/{gameId}/guess - Guesser submits a guess
        api.MapPost("/{gameId:guid}/guess", async (
            Guid gameId,
            [FromBody] SubmitGuessRequest req,
            [FromServices] GameRepository repo,
            [FromServices] WordValidationService wordSvc,
            [FromServices] GuessEvaluationService evalSvc,
            [FromServices] GameService gameSvc) =>
        {
            var game = await repo.GetGameAsync(gameId);
            if (game == null) return Results.NotFound(new ErrorResponse("Game not found"));
            if (game.Status is "won" or "lost") return Results.Conflict(new ErrorResponse("Game is over"));
            if (game.Status == "waiting") return Results.BadRequest(new ErrorResponse("Setter hasn't set a word yet"));

            var word = req.Word.Trim().ToLowerInvariant();
            if (word.Length != 5) return Results.BadRequest(new ErrorResponse("Word must be 5 letters"));
            if (!wordSvc.IsValidGuess(word)) return Results.UnprocessableEntity(new ErrorResponse("Not a valid word"));

            // Hard mode validation
            if (game.Guesses.Count > 0)
            {
                var (valid, error) = gameSvc.ValidateHardMode(game.Guesses, word);
                if (!valid) return Results.UnprocessableEntity(new ErrorResponse(error!));
            }

            var totalAllowed = game.GuessesAllowed + (game.SwapUsed ? game.BonusGuesses : 0);
            var guessNumber = game.Guesses.Count + 1;
            if (guessNumber > totalAllowed) return Results.Conflict(new ErrorResponse("No guesses remaining"));

            var result = evalSvc.EvaluateGuess(game.Word, word);
            await repo.AddGuessAsync(gameId, guessNumber, word, result);

            // Check win/loss
            if (word == game.Word)
            {
                await repo.EndGameAsync(gameId, "won");
            }
            else if (guessNumber >= totalAllowed)
            {
                await repo.EndGameAsync(gameId, "lost");
            }

            var updatedGame = await repo.GetGameAsync(gameId);
            return Results.Ok(gameSvc.BuildGameState(updatedGame!));
        });

        // POST /api/games/{gameId}/swap - Setter swaps the word
        api.MapPost("/{gameId:guid}/swap", async (
            Guid gameId,
            [FromBody] SwapWordRequest req,
            [FromServices] GameRepository repo,
            [FromServices] WordValidationService wordSvc,
            [FromServices] GuessEvaluationService evalSvc,
            [FromServices] GameService gameSvc) =>
        {
            var game = await repo.GetGameAsync(gameId);
            if (game == null) return Results.NotFound(new ErrorResponse("Game not found"));
            if (game.Status is "won" or "lost") return Results.Conflict(new ErrorResponse("Game is over"));
            if (game.Status == "waiting") return Results.BadRequest(new ErrorResponse("Word not set yet"));
            if (game.SwapUsed) return Results.Conflict(new ErrorResponse("Swap already used"));
            if (game.Guesses.Count == 0) return Results.BadRequest(new ErrorResponse("No guesses made yet"));

            var newWord = req.Word.Trim().ToLowerInvariant();
            if (newWord.Length != 5) return Results.BadRequest(new ErrorResponse("Word must be 5 letters"));
            if (!wordSvc.IsValidAnswer(newWord)) return Results.BadRequest(new ErrorResponse("Not a valid word"));
            if (newWord == game.Word) return Results.BadRequest(new ErrorResponse("New word must differ from current word"));

            // Prevent swapping to a word already guessed (would create an undetected win)
            if (game.Guesses.Any(g => g.Word == newWord))
                return Results.BadRequest(new ErrorResponse("Cannot swap to a word that has already been guessed"));

            var (valid, error) = gameSvc.ValidateSwap(game.Guesses, newWord);
            if (!valid) return Results.BadRequest(new ErrorResponse(error!));

            var previousWord = game.Word;
            var afterGuessNumber = game.Guesses.Count;
            await repo.SwapWordAsync(gameId, newWord, previousWord, afterGuessNumber);

            // Re-evaluate all previous guesses against new word
            foreach (var guess in game.Guesses)
            {
                var newResult = evalSvc.EvaluateGuess(newWord, guess.Word);
                await repo.UpdateGuessResultAsync(gameId, guess.GuessNumber, newResult);
            }

            var updatedGame = await repo.GetGameAsync(gameId);
            return Results.Ok(gameSvc.BuildGameState(updatedGame!));
        });
    }
}
