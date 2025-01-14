using System.Text.Json.Serialization;

namespace WebApiSpotify.Models;

public record SoundTrack
{
    [JsonPropertyName("artist_name")] 
    public string ArtistName { get; init; }

    [JsonPropertyName("track_name")] 
    public string TrackName { get; init; }

    public SoundTrack(string artistName, string trackName)
    {
        ArtistName = artistName;
        TrackName = trackName;
    }
}
