using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Gotcha.Client;
using Gotcha.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// In production, ApiBaseUrl is not set — the API is on the same domain via nginx proxy.
// In development, ApiBaseUrl in appsettings.Development.json points to the local API.
var apiBase = builder.Configuration["ApiBaseUrl"];
var baseAddress = string.IsNullOrEmpty(apiBase)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(apiBase);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = baseAddress });
builder.Services.AddScoped<GameApiService>();

await builder.Build().RunAsync();
