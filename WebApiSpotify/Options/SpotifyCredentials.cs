using System.Text;

namespace WebApiSpotify.Options;

public sealed record SpotifyCredentials
{
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }

    public string BasicAuthHeader =>
        $"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"))}";

    public SpotifyCredentials(string clientId, string clientSecret)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public SpotifyCredentials()
    {
        ClientId = string.Empty;
        ClientSecret = string.Empty;
    }
}
