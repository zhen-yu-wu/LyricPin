using Media = System.Windows.Media;
using Forms = System.Windows.Forms;

namespace LyricPin;

public partial class MainWindow
{
    private void AddFontFamilyItem(string label, string familyName, bool isDefault = false)
    {
        var item = new Forms.ToolStripMenuItem(label)
        {
            Tag = familyName,
            Checked = isDefault
        };
        item.Click += (_, _) => SetLyricFontFamily(familyName, isDefault ? "默认" : label);
        _fontFamilyMenuItem.DropDownItems.Add(item);
    }

    private void SetLyricFontFamily(string familyName, string label)
    {
        var family = new Media.FontFamily(familyName);
        PreviousLyricText.FontFamily = family;
        CurrentLyricBaseText.FontFamily = family;
        CurrentLyricText.FontFamily = family;
        NextLyricText.FontFamily = family;
        OutgoingPreviousLyricText.FontFamily = family;
        OutgoingCurrentLyricText.FontFamily = family;
        OutgoingNextLyricText.FontFamily = family;

        foreach (Forms.ToolStripMenuItem item in _fontFamilyMenuItem.DropDownItems)
        {
            item.Checked = string.Equals(item.Tag as string, familyName, StringComparison.Ordinal);
        }
        _fontFamilyMenuItem.ShortcutKeyDisplayString = label;

        // Both lyric layers and the transition text must use the same font metrics.
        UpdateLyricsContentWidth();
        LyricsViewport.UpdateLayout();
        UpdateCurrentLyricClip();
        ScheduleHorizontalMarquee();
    }
}
