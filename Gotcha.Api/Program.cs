using Gotcha.Api.Data;
using Gotcha.Api.Endpoints;
using Gotcha.Api.Models;
using Gotcha.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameSettings>(
    builder.Configuration.GetSection("GameSettings"));

builder.Services.AddSingleton<WordValidationService>();
builder.Services.AddSingleton<GuessEvaluationService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<GameRepository>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust the Docker web container and host nginx (both local)
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5001", "https://localhost:5001" })
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();
app.UseStaticFiles();

app.MapGameEndpoints();

app.Run();
