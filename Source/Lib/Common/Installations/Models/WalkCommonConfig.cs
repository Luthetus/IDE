using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.Installations.Models;

/// <remarks>
/// This class is an exception to the naming convention, "don't use the word 'Clair' in class names".
/// 
/// Reason for this exception: when one first starts interacting with this project,
///     this type might be one of the first types they interact with. So, the redundancy of namespace
///     and type containing 'Clair' feels reasonable here.
/// </remarks>
public record struct ClairCommonConfig
{
    public ClairCommonConfig()
    {
    }

    /// <summary>The <see cref="Key{ThemeRecord}"/> to be used when the application starts</summary>
    public int InitialThemeKey { get; init; } = CommonFacts.VisualStudioDarkThemeClone.Key;
    public string IsMaximizedStyleCssString { get; init; } = "width: 100vw; height: 100vh; left: 0; top: 0;";
}
