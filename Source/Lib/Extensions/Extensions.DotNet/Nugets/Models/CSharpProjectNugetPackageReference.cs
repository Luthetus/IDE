namespace Clair.Extensions.DotNet.Nugets.Models;

public record CSharpProjectNugetPackageReference(
    string CSharpProjectAbsolutePathString,
    LightWeightNugetPackageRecord LightWeightNugetPackageRecord);
