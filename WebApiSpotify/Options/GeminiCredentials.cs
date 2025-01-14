namespace WebApiSpotify.Options;

public sealed record GeminiCredentials
{
    public string Key { get; init; }

    public GeminiCredentials(string key)
    {
        Key = key;
    }

    public GeminiCredentials()
    {
        Key = string.Empty;
    }
}
