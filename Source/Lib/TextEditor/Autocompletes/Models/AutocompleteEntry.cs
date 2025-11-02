using Clair.Common.RazorLib.Menus.Models;

namespace Clair.TextEditor.RazorLib.Autocompletes.Models;

public struct AutocompleteEntry
{
    public AutocompleteEntry(
        string displayName,
        AutocompleteEntryKind autocompleteEntryKind,
        int startInclusiveIndex)
    {
        DisplayName = displayName;
        AutocompleteEntryKind = autocompleteEntryKind;
        StartInclusiveIndex = startInclusiveIndex;
    }

    public string DisplayName { get; }
    public AutocompleteEntryKind AutocompleteEntryKind { get; }
    public int StartInclusiveIndex { get; }
}
