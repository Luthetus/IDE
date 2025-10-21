using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib.Terminals.Models;
using Clair.Extensions.DotNet.TestExplorers.Models;
using Clair.Extensions.DotNet.Nugets.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Models;
using Clair.Extensions.DotNet.Namespaces.Models;
using Clair.Extensions.DotNet.CSharpProjects.Models;

namespace Clair.Extensions.DotNet;

public class DotNetWorkArgs
{
    public DotNetWorkKind WorkKind { get; set; }
    
    /* Start DotNetBackgroundTaskApiWorkArgs */
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; }
    public TreeViewStringFragment TreeViewStringFragment { get; set; }
    public TreeViewProjectTestModel TreeViewProjectTestModel { get; set; }
    public string FullyQualifiedName { get; set; }
    public INugetPackageManagerQuery NugetPackageManagerQuery { get; set; }
    public Key<DotNetSolutionModel> DotNetSolutionModelKey { get; set; }
    public string ProjectTemplateShortName { get; set; }
    public string CSharpProjectName { get; set; }
    public AbsolutePath CSharpProjectAbsolutePath { get; set; }
    public AbsolutePath DotNetSolutionAbsolutePath { get; set; }
    /* End DotNetBackgroundTaskApiWorkArgs */

    /* Start Menu */
    public TreeViewSolution TreeViewSolution { get; set; }
    public TreeViewNamespacePath ProjectNode { get; set; }
    public ITerminal Terminal { get; set; }
    public Func<Task> OnAfterCompletion { get; set; }
    public TreeViewCSharpProjectToProjectReference TreeViewCSharpProjectToProjectReference { get; set; }
    public TreeViewNamespacePath TreeViewProjectToMove { get; set; }
    public string SolutionFolderPath { get; set; }
    public AbsolutePath ModifyProjectAbsolutePath { get; set; }
    public string ModifyProjectNamespaceString { get; set; }
    public TreeViewCSharpProjectNugetPackageReference TreeViewCSharpProjectNugetPackageReference { get; set; }
    /* End Menu */
}
