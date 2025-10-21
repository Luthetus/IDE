using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Commands.Models;

namespace Clair.Extensions.DotNet.Outputs.Displays.Internals;

public partial class OutputContextMenu : ComponentBase
{
    [Parameter, EditorRequired]
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; }

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();
}
