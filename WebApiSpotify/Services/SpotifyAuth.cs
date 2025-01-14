using System.Net.Http.Headers;
using WebApiSpotify.Dtos;

namespace WebApiSpotify.Services;

public class SpotifyAuth(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<SpotifyTokenResponse> RequestTokenAsync(string authCode, string redirectUrl, CancellationToken ct = default)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "api/token")
        {
            Content = new FormUrlEncodedContent(
            [
                new("grant_type", "authorization_code"),
                new("code", authCode),
                new("redirect_uri", redirectUrl)
            ])
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
                }
            }
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        SpotifyTokenResponse? responseContent = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(ct);
        ArgumentNullException.ThrowIfNull(responseContent);

        return responseContent;
    }

    public async Task<SpotifyTokenResponse> RefreshTokenAsync(string refreshToken, string clientId, CancellationToken ct = default)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "api/token")
        {
            Content = new FormUrlEncodedContent(
            [
                new("grant_type", "refresh_token"),
                new("refresh_token", refreshToken),
                new("client_id", clientId)
            ])
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
                }
            }
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        SpotifyTokenResponse? responseContent = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(ct);
        ArgumentNullException.ThrowIfNull(responseContent);

        return responseContent;
    }

    public static string GetCallbackRedirectUrl(HttpContext context)
    {
        return $"{context.Request.Scheme}://{context.Request.Host}/spotify/callback";
    }
}
