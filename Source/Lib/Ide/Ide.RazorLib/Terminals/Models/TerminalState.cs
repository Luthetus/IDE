namespace Clair.Ide.RazorLib.Terminals.Models;

public record struct TerminalState(ITerminal? ExecutionTerminal, ITerminal? GeneralTerminal);
