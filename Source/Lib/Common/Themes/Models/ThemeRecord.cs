namespace Clair.Common.RazorLib.Themes.Models;

public readonly struct ThemeRecord(
    int Key,
    string DisplayName,
    string CssClassString,
    ThemeContrastKind ThemeContrastKind,
    ThemeColorKind ThemeColorKind,
    bool IncludeScopeApp,
    bool IncludeScopeTextEditor)
{
    public int Key { get; } = Key;
    public string DisplayName { get; } = DisplayName;
    public string CssClassString { get; } = CssClassString;
    public ThemeContrastKind ThemeContrastKind { get; } = ThemeContrastKind;
    public ThemeColorKind ThemeColorKind { get; } = ThemeColorKind;
    public bool IncludeScopeApp { get; } = IncludeScopeApp;
    public bool IncludeScopeTextEditor { get; } = IncludeScopeTextEditor;

    public readonly bool IsDefault()
    {
        return DisplayName is null &&
               CssClassString is null;
    }
}
