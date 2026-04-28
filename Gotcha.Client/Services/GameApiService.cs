using System.Net.Http.Json;
using Gotcha.Client.Models;

namespace Gotcha.Client.Services;

public class GameApiService
{
    private readonly HttpClient _http;

    public GameApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<CreateGameResponse?> CreateGameAsync()
    {
        var resp = await _http.PostAsync("/api/games", null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<CreateGameResponse>();
    }

    public async Task<GameStateResponse?> GetGameAsync(Guid gameId, bool setter = false)
    {
        var url = setter ? $"/api/games/{gameId}?setter=true" : $"/api/games/{gameId}";
        return await _http.GetFromJsonAsync<GameStateResponse>(url);
    }

    public async Task<(bool success, string? error)> SetWordAsync(Guid gameId, string word, int bonusGuesses)
    {
        var resp = await _http.PostAsJsonAsync($"/api/games/{gameId}/word", new { word, bonusGuesses });
        if (resp.IsSuccessStatusCode) return (true, null);
        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        return (false, err?.Error ?? "Unknown error");
    }

    public async Task<(GameStateResponse? state, string? error)> SubmitGuessAsync(Guid gameId, string word)
    {
        var resp = await _http.PostAsJsonAsync($"/api/games/{gameId}/guess", new { word });
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<GameStateResponse>(), null);
        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        return (null, err?.Error ?? "Unknown error");
    }

    public async Task<(GameStateResponse? state, string? error)> SwapWordAsync(Guid gameId, string word)
    {
        var resp = await _http.PostAsJsonAsync($"/api/games/{gameId}/swap", new { word });
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<GameStateResponse>(), null);
        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        return (null, err?.Error ?? "Unknown error");
    }
}
