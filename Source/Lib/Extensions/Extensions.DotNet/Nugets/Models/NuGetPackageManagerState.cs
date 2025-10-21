using Clair.CompilerServices.DotNetSolution.Models.Project;

namespace Clair.Extensions.DotNet.Nugets.Models;

public record struct NuGetPackageManagerState(
    IDotNetProject? SelectedProjectToModify,
    string NugetQuery,
    bool IncludePrerelease,
    List<NugetPackageRecord> QueryResultList)
{
    public NuGetPackageManagerState() : this(null, string.Empty, false, new())
    {

    }
}
