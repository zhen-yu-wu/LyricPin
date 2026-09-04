using System.Net.Http;
using System.Text.Json;
using LyricPin.Models;

namespace LyricPin.Services;

public sealed class LyricService : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public LyricService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 LyricPin/0.1");
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://music.163.com/");
    }

    public async Task<(Song Song, string Lyrics)?> GetLyricsAsync(
        Song currentSong,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var song = currentSong.Id > 0
                ? currentSong
                : await FindSongAsync(currentSong, cancellationToken);
            if (song is null)
            {
                return null;
            }

            var url = $"https://music.163.com/api/song/lyric?id={song.Id}&lv=1&kv=1&tv=-1";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("lrc", out var lrc) ||
                !lrc.TryGetProperty("lyric", out var lyric))
            {
                return null;
            }

            var text = lyric.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : (song, text);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<Song?> FindSongAsync(Song currentSong, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"{currentSong.Name} {currentSong.Artist}");
        var url = $"https://music.163.com/api/search/get/web?s={query}&type=1&offset=0&total=true&limit=10";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("result", out var result) ||
            !result.TryGetProperty("songs", out var songs))
        {
            return null;
        }

        Song? bestCandidate = null;
        var bestScore = int.MinValue;
        foreach (var item in songs.EnumerateArray())
        {
            var candidate = ReadSong(item);
            if (candidate is null)
            {
                continue;
            }

            var score = CalculateMatchScore(candidate, currentSong);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        return bestScore >= 70 ? bestCandidate : null;
    }

    private static Song? ReadSong(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var id) ||
            !item.TryGetProperty("name", out var nameElement))
        {
            return null;
        }

        var name = nameElement.GetString() ?? string.Empty;
        var artists = new List<string>();
        if (item.TryGetProperty("artists", out var artistArray))
        {
            foreach (var artist in artistArray.EnumerateArray())
            {
                if (artist.TryGetProperty("name", out var artistName) &&
                    !string.IsNullOrWhiteSpace(artistName.GetString()))
                {
                    artists.Add(artistName.GetString()!);
                }
            }
        }

        var duration = item.TryGetProperty("duration", out var durationElement) &&
                       durationElement.TryGetInt64(out var durationMs)
            ? TimeSpan.FromMilliseconds(durationMs)
            : TimeSpan.Zero;

        return new Song
        {
            Id = id.GetInt64(),
            Name = name,
            Artist = string.Join(" / ", artists),
            Duration = duration
        };
    }

    private static int CalculateMatchScore(Song candidate, Song current)
    {
        var expectedTitle = Normalize(current.Name);
        var candidateTitle = Normalize(candidate.Name);
        var expectedArtist = Normalize(current.Artist);
        var candidateArtist = Normalize(candidate.Artist);

        var score = candidateTitle == expectedTitle
            ? 60
            : candidateTitle.Contains(expectedTitle) || expectedTitle.Contains(candidateTitle)
                ? 35
                : 0;

        score += string.IsNullOrEmpty(expectedArtist)
            ? 10
            : candidateArtist == expectedArtist
                ? 30
                : candidateArtist.Contains(expectedArtist) || expectedArtist.Contains(candidateArtist)
                    ? 20
                    : 0;

        if (current.Duration > TimeSpan.Zero && candidate.Duration > TimeSpan.Zero)
        {
            var difference = Math.Abs((candidate.Duration - current.Duration).TotalSeconds);
            score += difference <= 2.5 ? 20 : difference <= 8 ? 10 : difference > 30 ? -15 : 0;
        }

        if (!current.Name.Contains("伴奏", StringComparison.OrdinalIgnoreCase) &&
            candidate.Name.Contains("伴奏", StringComparison.OrdinalIgnoreCase))
        {
            score -= 50;
        }

        return score;
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
