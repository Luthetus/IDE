using Clair.Common.RazorLib.Keys.Models;
using Clair.Ide.RazorLib.Terminals.Models;

namespace Clair.Ide.RazorLib;

public partial class IdeService
{
    private TerminalGroupState _terminalGroupState = new();

    public TerminalGroupState GetTerminalGroupState() => _terminalGroupState;

    public void TerminalGroup_SetActiveTerminal(Key<ITerminal> terminalKey)
    {
        lock (_stateModificationLock)
        {
            _terminalGroupState = _terminalGroupState with
            {
                ActiveTerminalKey = terminalKey
            };
        }

        IdeStateChanged?.Invoke(IdeStateChangedKind.TerminalGroupStateChanged);
    }
}
