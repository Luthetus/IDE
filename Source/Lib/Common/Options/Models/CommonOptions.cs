namespace Clair.Common.RazorLib.Options.Models;

public struct CommonOptions
{
    public CommonOptions(
        int fontSizeInPixels,
        int iconSizeInPixels,
        int resizeHandleWidthInPixels,
        int resizeHandleHeightInPixels,
        int themeKey,
        string? fontFamily)
    {
        FontSizeInPixels = fontSizeInPixels;
        IconSizeInPixels = iconSizeInPixels;
        ResizeHandleWidthInPixels = resizeHandleWidthInPixels;
        ResizeHandleHeightInPixels = resizeHandleHeightInPixels;
        ThemeKey = themeKey;
        FontFamily = fontFamily;
    }

    public int FontSizeInPixels { get; set; }
    public int IconSizeInPixels { get; set; }
    public int ResizeHandleWidthInPixels { get; set; }
    public int ResizeHandleHeightInPixels { get; set; }
    public int ThemeKey { get; set; }
    public string? FontFamily { get; set; }
}
