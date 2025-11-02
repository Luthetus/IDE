using Clair.Common.RazorLib.Menus.Models;

namespace Clair.TextEditor.RazorLib.Autocompletes.Models;

public readonly struct AutocompleteValue(
    string displayName,
    AutocompleteEntryKind autocompleteEntryKind,
    int absolutePathId,
    int startInclusiveIndex,
    int endExclusiveIndex)
{
    public string DisplayName { get; } = displayName;
    public AutocompleteEntryKind AutocompleteEntryKind { get; } = autocompleteEntryKind;
    public int AbsolutePathId { get; } = absolutePathId;
    public int StartInclusiveIndex { get; } = startInclusiveIndex;
    public int EndExclusiveIndex { get; } = endExclusiveIndex;
}
