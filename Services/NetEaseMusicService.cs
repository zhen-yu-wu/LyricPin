using System.Diagnostics;
using System.IO;
using System.Text.Json;
using LyricPin.Models;
using Windows.Media.Control;

namespace LyricPin.Services;

public sealed class NetEaseMusicService
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private string? _resolvedMediaKey;
    private Song? _resolvedSong;
    private string? _clockSongKey;
    private TimeSpan _estimatedPosition;
    private DateTimeOffset _clockUpdatedAt;
    private bool _wasPlaying;

    public async Task<PlaybackSnapshot?> GetPlaybackAsync()
    {
        try
        {
            _sessionManager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            var session = _sessionManager.GetSessions().FirstOrDefault(IsNetEaseSession);
            if (session is null)
            {
                return null;
            }

            var media = await session.TryGetMediaPropertiesAsync();
            if (media is null || string.IsNullOrWhiteSpace(media.Title))
            {
                return null;
            }

            var timeline = session.GetTimelineProperties();
            var playbackInfo = session.GetPlaybackInfo();
            var isPlaying = playbackInfo.PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var mediaSong = new Song
            {
                Name = media.Title.Trim(),
                Artist = media.Artist?.Trim() ?? string.Empty,
                Duration = timeline.EndTime
            };

            var mediaKey = $"{mediaSong.Name}\n{mediaSong.Artist}";
            if (!string.Equals(mediaKey, _resolvedMediaKey, StringComparison.Ordinal))
            {
                _resolvedMediaKey = mediaKey;
                _resolvedSong = await TryResolveLocalSongAsync(mediaSong) ?? mediaSong;
            }

            var song = _resolvedSong ?? mediaSong;
            var position = GetPosition(timeline, mediaKey, isPlaying);
            if (song.Duration > TimeSpan.Zero && position > song.Duration)
            {
                position = song.Duration;
            }

            return new PlaybackSnapshot(song, position, isPlaying);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsClientRunning()
    {
        var processes = Process.GetProcessesByName("cloudmusic");
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private TimeSpan GetPosition(
        GlobalSystemMediaTransportControlsSessionTimelineProperties timeline,
        string songKey,
        bool isPlaying)
    {
        var now = DateTimeOffset.Now;
        var hasSystemTimeline = timeline.LastUpdatedTime.Year > 1900 &&
            (timeline.Position > TimeSpan.Zero || timeline.EndTime > TimeSpan.Zero);

        if (hasSystemTimeline)
        {
            var position = timeline.Position;
            var elapsed = now - timeline.LastUpdatedTime;
            if (isPlaying && elapsed > TimeSpan.Zero)
            {
                position += elapsed;
            }

            _clockSongKey = songKey;
            _estimatedPosition = position;
            _clockUpdatedAt = now;
            _wasPlaying = isPlaying;
            return position;
        }

        if (!string.Equals(songKey, _clockSongKey, StringComparison.Ordinal))
        {
            _clockSongKey = songKey;
            _estimatedPosition = TimeSpan.Zero;
        }
        else if (_wasPlaying)
        {
            _estimatedPosition += now - _clockUpdatedAt;
        }

        _clockUpdatedAt = now;
        _wasPlaying = isPlaying;
        return _estimatedPosition;
    }

    private static async Task<Song?> TryResolveLocalSongAsync(Song mediaSong)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(
            localAppData,
            "Netease",
            "CloudMusic",
            "webdata",
            "file",
            "playingList");

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var document = await JsonDocument.ParseAsync(stream);

            if (!document.RootElement.TryGetProperty("list", out var list))
            {
                return null;
            }

            var expectedTitle = Normalize(mediaSong.Name);
            var expectedArtist = Normalize(mediaSong.Artist);

            foreach (var item in list.EnumerateArray())
            {
                if (!item.TryGetProperty("track", out var track) ||
                    !track.TryGetProperty("name", out var nameElement))
                {
                    continue;
                }

                var name = nameElement.GetString() ?? string.Empty;
                var artists = ReadArtists(track);
                if (!string.Equals(Normalize(name), expectedTitle, StringComparison.Ordinal) ||
                    !ArtistMatches(artists, expectedArtist))
                {
                    continue;
                }

                if (!TryReadId(item, track, out var id))
                {
                    continue;
                }

                var duration = track.TryGetProperty("duration", out var durationElement) &&
                               durationElement.TryGetInt64(out var durationMs)
                    ? TimeSpan.FromMilliseconds(durationMs)
                    : TimeSpan.Zero;

                return new Song
                {
                    Id = id,
                    Name = name,
                    Artist = string.Join(" / ", artists),
                    Duration = duration
                };
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static List<string> ReadArtists(JsonElement track)
    {
        var artists = new List<string>();
        if (!track.TryGetProperty("artists", out var artistArray))
        {
            return artists;
        }

        foreach (var artist in artistArray.EnumerateArray())
        {
            if (artist.TryGetProperty("name", out var name) &&
                !string.IsNullOrWhiteSpace(name.GetString()))
            {
                artists.Add(name.GetString()!);
            }
        }

        return artists;
    }

    private static bool ArtistMatches(IEnumerable<string> artists, string expectedArtist)
    {
        if (string.IsNullOrEmpty(expectedArtist))
        {
            return true;
        }

        var combined = Normalize(string.Join(string.Empty, artists));
        return combined.Contains(expectedArtist, StringComparison.Ordinal) ||
               expectedArtist.Contains(combined, StringComparison.Ordinal);
    }

    private static bool TryReadId(JsonElement item, JsonElement track, out long id)
    {
        foreach (var element in new[] { item, track })
        {
            if (!element.TryGetProperty("id", out var idElement))
            {
                continue;
            }

            if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt64(out id))
            {
                return true;
            }

            if (idElement.ValueKind == JsonValueKind.String &&
                long.TryParse(idElement.GetString(), out id))
            {
                return true;
            }
        }

        id = 0;
        return false;
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static bool IsNetEaseSession(GlobalSystemMediaTransportControlsSession session)
    {
        var source = session.SourceAppUserModelId;
        return source.Contains("cloudmusic", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("netease", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("网易云", StringComparison.OrdinalIgnoreCase);
    }
}
