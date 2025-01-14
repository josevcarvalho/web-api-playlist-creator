using System.Text.Json;
using WebApiSpotify.Models;
using WebApiSpotify.Options;

namespace WebApiSpotify.Services;

public class IAClient(HttpClient httpClient, GeminiCredentials credentials)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly GeminiCredentials _credentials = credentials;

    public async Task<IEnumerable<SoundTrack>> GetTracksByPromptAsync(string prompt, CancellationToken ct)
    {
        var payload = new
        {
            system_instruction = new
            {
                parts = new 
                { 
                    text = "return 30 songs according to user prompt"
                }
            },
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new 
            {
                response_mime_type = "application/json",
                response_schema = new 
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new 
                        {
                            artist_name = new { type = "STRING"},
                            track_name = new {type = "STRING"}
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_credentials.Key}", payload, ct);

        GeminiPromptResponse? responseContent = await response.Content.ReadFromJsonAsync<GeminiPromptResponse>(ct);
        ArgumentNullException.ThrowIfNull(responseContent);

        var tracks = JsonSerializer.Deserialize<IEnumerable<SoundTrack>>(responseContent.Candidates[0].Content.Parts[0].Text);

        return tracks ?? [];
    }

    private record GeminiPromptResponse(IReadOnlyList<GeminiCandidate> Candidates);
    private record GeminiCandidate(GeminiCandidateContent Content);
    private record GeminiCandidateContent(IReadOnlyList<GeminiPart> Parts);
    private record GeminiPart(string Text);

}