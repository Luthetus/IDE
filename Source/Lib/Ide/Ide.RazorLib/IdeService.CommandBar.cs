using Clair.Ide.RazorLib.CommandBars.Models;

namespace Clair.Ide.RazorLib;

public partial class IdeService
{
    private CommandBarState _commandBarState = new();

    public CommandBarState GetCommandBarState() => _commandBarState;
}
