namespace Clair.TextEditor.RazorLib.Lexers.Models;

public record struct ResourceUri(string Value)
{
    public static readonly ResourceUri Empty = new(string.Empty);
    public const int NullAbsolutePathId = 0;
    public const int EmptyAbsolutePathId = 1;
}
