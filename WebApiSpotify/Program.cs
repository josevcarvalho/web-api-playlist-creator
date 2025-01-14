using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using WebApiSpotify.Models;
using WebApiSpotify.Options;
using WebApiSpotify.Services;

const string SpotifyAuthUrl = "https://accounts.spotify.com";

var builder = WebApplication.CreateBuilder(args);

SpotifyCredentials spotifyCredentials = new();
builder.Configuration.GetSection(nameof(SpotifyCredentials)).Bind(spotifyCredentials);

GeminiCredentials geminiCredentials = new();
builder.Configuration.GetSection(nameof(GeminiCredentials)).Bind(geminiCredentials);

builder.Services.AddOpenApi()
    .AddSingleton<SpotifyCredentials>()
    .AddMemoryCache()
    .AddProblemDetails();

builder.Services
    .AddHttpClient<SpotifyAuth>(client =>
    {
        client.BaseAddress = new Uri(SpotifyAuthUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", spotifyCredentials.BasicAuthHeader);
    });

builder.Services
    .AddHttpClient<PlaylistCreator>(client => 
    {
        client.BaseAddress = new Uri("https://api.spotify.com/v1/");
    });

builder.Services
    .AddHttpClient<IAClient>();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseExceptionHandler(exceptionHandlerApp => 
    exceptionHandlerApp.Run(async context =>
        await Results.Problem()
            .ExecuteAsync(context)));

app.MapGet("/spotify/authorization", (HttpContext context) =>
{ 
    string callbackRedirectUrl = SpotifyAuth.GetCallbackRedirectUrl(context);

    string spotifyUrl = QueryHelpers.AddQueryString($"{SpotifyAuthUrl}/authorize", new Dictionary<string, string?>
    {
        { "client_id", spotifyCredentials.ClientId },
        { "response_type", "code" },
        { "redirect_uri", callbackRedirectUrl },
        { "scope", "playlist-modify-public" }
    });
    
    return Results.Redirect(spotifyUrl);
});

app.MapGet("/spotify/callback", async ([FromQuery] string? code, [FromQuery] string? error, SpotifyAuth spotifyAuth, HttpContext context, IMemoryCache memoryCache, CancellationToken ct = default) =>
{
    if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        return Results.BadRequest(new { Error = error});

    string callbackRedirectUrl = SpotifyAuth.GetCallbackRedirectUrl(context);

    var response = await spotifyAuth.RequestTokenAsync(code, callbackRedirectUrl, ct);

    memoryCache.Set("access_token", response.AccessToken, TimeSpan.FromSeconds(response.ExpiresIn));

    return Results.Ok();
});

app.MapGet("/playlist", async ([FromQuery] string prompt, [FromQuery] string? playlistId, PlaylistCreator playlistCreator, IAClient iaClient, CancellationToken ct = default) =>
{
    playlistId ??= await playlistCreator.CreateAsync(userId: "wrcytba5d162kfx434vb4l6i9", ct);

    IEnumerable<SoundTrack> tracks = await iaClient.GetTracksByPromptAsync(prompt, ct);

    await playlistCreator.AddTracksAsync(playlistId, tracks, ct);

    return Results.Created();
});

await app.RunAsync();