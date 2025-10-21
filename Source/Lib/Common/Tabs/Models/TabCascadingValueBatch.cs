using Clair.Common.RazorLib.Dynamics.Models;

namespace Clair.Common.RazorLib.Tabs.Models;

public class TabCascadingValueBatch
{
    public bool ThinksLeftMouseButtonIsDown { get; set; }
    public Func<TabContextMenuEventArgs, Task>? HandleTabButtonOnContextMenu { get; set; }
    public Action<IDrag>? SubscribeToDragEventForScrolling { get; set; }
}
