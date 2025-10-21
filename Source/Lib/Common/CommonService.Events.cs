namespace Clair.Common.RazorLib;

public partial class CommonService
{
    public event Action<CommonUiEventKind>? CommonUiStateChanged;
}
