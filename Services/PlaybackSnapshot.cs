using LyricPin.Models;

namespace LyricPin.Services;

public sealed record PlaybackSnapshot(Song Song, TimeSpan Position, bool IsPlaying);
