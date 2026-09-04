using LyricPin.Models;

namespace LyricPin.Services;

public static class LyricSyncService
{
    public static LyricLine? FindCurrentLine(
        IReadOnlyList<LyricLine> lyrics,
        TimeSpan position)
    {
        var index = FindCurrentLineIndex(lyrics, position);
        return index >= 0 ? lyrics[index] : null;
    }

    public static int FindCurrentLineIndex(
        IReadOnlyList<LyricLine> lyrics,
        TimeSpan position)
    {
        var low = 0;
        var high = lyrics.Count - 1;
        var result = -1;

        while (low <= high)
        {
            var middle = low + (high - low) / 2;
            if (lyrics[middle].Time <= position)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return result;
    }
}
