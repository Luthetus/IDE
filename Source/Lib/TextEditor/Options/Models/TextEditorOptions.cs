using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Options.Models;
using Clair.TextEditor.RazorLib.JavaScriptObjects.Models;

namespace Clair.TextEditor.RazorLib.Options.Models;

/// <param name="TabKeyBehavior">Tab key inserts:(true:tab, false:spaces):</param>
public struct TextEditorOptions
{
    public TextEditorOptions(
        CommonOptions commonOptions,
        bool showWhitespace,
        bool showNewlines,
        bool tabKeyBehavior,
        int tabWidth,
        double cursorWidthInPixels,
        CharAndLineMeasurements charAndLineMeasurements)
    {
        CommonOptions = commonOptions;
        ShowWhitespace = showWhitespace;
        ShowNewlines = showNewlines;
        TabKeyBehavior = tabKeyBehavior;
        TabWidth = tabWidth;
        CursorWidthInPixels = cursorWidthInPixels;
        CharAndLineMeasurements = charAndLineMeasurements;

        RenderStateKey = Key<TextEditorOptions>.NewKey();
    }

    public CommonOptions CommonOptions { get; set; }
    public bool ShowWhitespace { get; set; }
    public bool ShowNewlines { get; set; }
    public bool TabKeyBehavior { get; set; }
    public int TabWidth { get; set; }
    public double CursorWidthInPixels { get; set; }
    public CharAndLineMeasurements CharAndLineMeasurements { get; set; }

    public Key<TextEditorOptions> RenderStateKey { get; init; }
}
