using Clair.Common.RazorLib.Menus.Models;

namespace Clair.TextEditor.RazorLib.Autocompletes.Models;

public readonly struct AutocompleteValue(
    string displayName,
    AutocompleteEntryKind autocompleteEntryKind)
{    public string DisplayName { get; } = displayName;
    public AutocompleteEntryKind AutocompleteEntryKind { get; } = autocompleteEntryKind;}
