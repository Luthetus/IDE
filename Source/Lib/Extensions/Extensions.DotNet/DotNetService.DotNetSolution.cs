using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Models;

namespace Clair.Extensions.DotNet;

public partial class DotNetService
{
    private DotNetSolutionState _dotNetSolutionState = new();

    public DotNetSolutionState GetDotNetSolutionState() => _dotNetSolutionState;

    public void SetDotNetSolution(DotNetSolutionModel dotNetSolutionModel)
    {
        var inState = GetDotNetSolutionState();

        _dotNetSolutionState = inState with
        {
            DotNetSolutionModel = dotNetSolutionModel
        };

        DotNetStateChanged?.Invoke(DotNetStateChangedKind.SolutionStateChanged);
    }
}
