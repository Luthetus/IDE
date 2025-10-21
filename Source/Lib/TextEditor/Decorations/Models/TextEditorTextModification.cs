using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.TextEditor.RazorLib.Decorations.Models;

public record struct TextEditorTextModification(bool WasInsertion, TextEditorTextSpan TextEditorTextSpan);
