namespace LyricPin.Models;

public sealed class Song
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public TimeSpan Duration { get; init; }
}
