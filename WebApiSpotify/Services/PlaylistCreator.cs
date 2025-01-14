using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using WebApiSpotify.Models;

namespace WebApiSpotify.Services;

public class PlaylistCreator(HttpClient httpClient, IMemoryCache memoryCache)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IMemoryCache _memoryCache = memoryCache;

    public async Task<string> CreateAsync(string userId, CancellationToken ct = default)
    {
        var requestBody = new
        {
            name = $"Minha playlist com IA",
            description = "Playlist gerada com inteligencia artificial",
            @public = true
        };

        HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"users/{userId}/playlists", JsonContent.Create(requestBody));
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<PlaylistCreatedResponse>(ct);
        ArgumentNullException.ThrowIfNull(responseContent);

        return responseContent.Id;
    }

    private record PlaylistCreatedResponse(string Id);

    private async Task<IEnumerable<string?>> GetTracksUrisAsync(IEnumerable<SoundTrack> tracks, CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(10);
        
        var tasks = tracks.Select(async track =>
        { 
            try
            {
                await semaphore.WaitAsync();
                return await FetchTrackUriAsync(track, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var uris = await Task.WhenAll(tasks);

        return uris ?? throw new KeyNotFoundException("No tracks were found");
    }

    private record TracksFoundedResponse(TracksContent Tracks);
    private record TracksContent(IEnumerable<TrackContentItem> Items);
    private record TrackContentItem(string Uri);

    private async Task<string?> FetchTrackUriAsync(SoundTrack track, CancellationToken ct = default)
    {
        string url = QueryHelpers.AddQueryString("search", new Dictionary<string, string?>
        {
            {"q", $"track:{track.TrackName} artist:{track.ArtistName}"},
            {"type", "track" },
            {"limit", "1" }
        });

        HttpRequestMessage request = CreateRequest(HttpMethod.Get, url);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<TracksFoundedResponse>(ct);
        string? uri = responseContent?.Tracks?.Items?.FirstOrDefault()?.Uri;
        return uri; 
    }

    public async Task AddTracksAsync(string playlistId, IEnumerable<SoundTrack> tracks, CancellationToken ct = default)
    {
        var uris = (await GetTracksUrisAsync(tracks, ct))
            .Where(uri => !string.IsNullOrEmpty(uri));

        var requestBody = new
        {
            position = 0,
            uris
        };

        HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"playlists/{playlistId}/tracks", JsonContent.Create(requestBody));
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        string? accessToken = _memoryCache.Get<string>("access_token");

        return new HttpRequestMessage(method, url)
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
            },
            Content = content
        };
    }
}
