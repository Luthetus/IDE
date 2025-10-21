using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.TextEditor.RazorLib;

namespace Clair.Ide.RazorLib.Settings.Displays;

public partial class SettingsDisplay : ComponentBase
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    public static readonly Key<IDynamicViewModel> SettingsDialogKey = Key<IDynamicViewModel>.NewKey();
}
