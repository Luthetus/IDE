namespace Clair.Extensions.CompilerServices.Syntax.Enums;

/// <summary>
/// Explicit means the text actually exists on the file system somewhere.
/// Implicit would be a Blazor component, in which the class name is the filename (minus extension).
/// </summary>
public enum TextSourceKind
{
    Explicit,
    Implicit
}
