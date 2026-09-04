namespace LyricPin.Models;

public sealed record CloudMusicLyricSnapshot(
    long SongId,
    string SongName,
    string Artist,
    TimeSpan Duration,
    bool IsPlaying,
    int LineIndex,
    string PreviousText,
    string Text,
    string NextText,
    double LineProgress);
