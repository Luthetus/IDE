using Clair.Common.RazorLib.Installations.Models;
using Clair.Ide.RazorLib.Terminals.Models;

namespace Clair.Ide.RazorLib;

public partial class IdeService
{
    private readonly object _stateModificationLock = new();

    private TerminalState _terminalState = new();

    public TerminalState GetTerminalState() => _terminalState;
    
    private void AddTerminals()
    {
        ITerminal executionTerminal;
        ITerminal generalTerminal;
        
        if (CommonService.ClairHostingInformation.ClairHostingKind == ClairHostingKind.Wasm ||
            CommonService.ClairHostingInformation.ClairHostingKind == ClairHostingKind.ServerSide)
        {
            executionTerminal = new TerminalWebsite(
                "Execution",
                CommonService,
                TextEditorService)
            {
                Key = IdeFacts.EXECUTION_KEY,
                TextEditorViewModelKey = TextEditor.RazorLib.TextEditorService.__ReserveViewModelKey(1),
            };
            
            generalTerminal = new TerminalWebsite(
                "General",
                CommonService,
                TextEditorService)
            {
                Key = IdeFacts.GENERAL_KEY,
                TextEditorViewModelKey = TextEditor.RazorLib.TextEditorService.__ReserveViewModelKey(2),
            };
        }
        else
        {
            executionTerminal = new Terminal(
                "Execution",
                this)
            {
                Key = IdeFacts.EXECUTION_KEY,
                TextEditorViewModelKey = TextEditor.RazorLib.TextEditorService.__ReserveViewModelKey(1),
            };
            
            generalTerminal = new Terminal(
                "General",
                this)
            {
                Key = IdeFacts.GENERAL_KEY,
                TextEditorViewModelKey = TextEditor.RazorLib.TextEditorService.__ReserveViewModelKey(2),
            };
        }
    
        _terminalState = _terminalState with
        {
            ExecutionTerminal = executionTerminal,
            GeneralTerminal = generalTerminal
        };
    }

    public void Terminal_HasExecutingProcess_StateHasChanged()
    {
        IdeStateChanged?.Invoke(IdeStateChangedKind.TerminalHasExecutingProcessStateChanged);
    }
}
