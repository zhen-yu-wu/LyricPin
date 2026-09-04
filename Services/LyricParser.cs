using System.Globalization;
using System.Text.RegularExpressions;
using LyricPin.Models;

namespace LyricPin.Services;

public static partial class LyricParser
{
    public static IReadOnlyList<LyricLine> Parse(string lrc)
    {
        var lines = new List<LyricLine>();

        foreach (var sourceLine in lrc.Split('\n'))
        {
            var text = TimestampRegex().Replace(sourceLine, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (Match match in TimestampRegex().Matches(sourceLine))
            {
                if (!int.TryParse(match.Groups["minutes"].Value, out var minutes) ||
                    !double.TryParse(
                        match.Groups["seconds"].Value,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out var seconds))
                {
                    continue;
                }

                lines.Add(new LyricLine
                {
                    Time = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds),
                    Text = text
                });
            }
        }

        return lines.OrderBy(line => line.Time).ToArray();
    }

    [GeneratedRegex(@"\[(?<minutes>\d{1,3}):(?<seconds>\d{1,2}(?:\.\d{1,3})?)\]")]
    private static partial Regex TimestampRegex();
}
