using System.Text.Json.Serialization;

namespace WebApiSpotify.Dtos;

public sealed record SpotifyTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }
    [JsonPropertyName("scope")]
    public string Scope { get; init; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; }

    public SpotifyTokenResponse(string accessToken, string tokenType, string scope, int expiresIn, string refreshToken)
    {
        AccessToken = accessToken;
        TokenType = tokenType;
        Scope = scope;
        ExpiresIn = expiresIn;
        RefreshToken = refreshToken;
    }
}
