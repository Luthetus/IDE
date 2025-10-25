using System.Collections.Concurrent;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.TextEditor.RazorLib;
using Clair.Ide.RazorLib;
using Clair.Ide.RazorLib.AppDatas.Models;

namespace Clair.Extensions.DotNet;

public sealed partial class DotNetService : IBackgroundTaskGroup, IDisposable
{
    private readonly HttpClient _httpClient;
    
    public DotNetService(
        IdeService ideService,
        HttpClient httpClient,
        IAppDataService appDataService,
        IServiceProvider serviceProvider)
    {
        IdeService = ideService;
        AppDataService = appDataService;
        _httpClient = httpClient;
        
        //DotNetStateChanged += OnDotNetSolutionStateChanged;
    }
    
    public Key<IBackgroundTaskGroup> BackgroundTaskKey { get; } = Key<IBackgroundTaskGroup>.NewKey();

    public bool __TaskCompletionSourceWasCreated { get; set; }
    public IdeService IdeService { get; }
    public TextEditorService TextEditorService => IdeService.TextEditorService;
    public CommonService CommonService => IdeService.TextEditorService.CommonService;
    public IAppDataService AppDataService { get; }
    
    private readonly ConcurrentQueue<DotNetWorkArgs> _workQueue = new();
    
    public void Enqueue(DotNetWorkArgs workArgs)
    {
        _workQueue.Enqueue(workArgs);
        IdeService.TextEditorService.CommonService.Continuous_Enqueue(this);
    }
    
    public ValueTask HandleEvent()
    {
        if (!_workQueue.TryDequeue(out DotNetWorkArgs workArgs))
            return ValueTask.CompletedTask;

        switch (workArgs.WorkKind)
        {
            case DotNetWorkKind.SolutionExplorer_TreeView_MultiSelect_DeleteFiles:
                return Do_SolutionExplorer_TreeView_MultiSelect_DeleteFiles(workArgs.TreeViewCommandArgs);
            case DotNetWorkKind.SubmitNuGetQuery:
                return Do_SubmitNuGetQuery(workArgs.NugetPackageManagerQuery);
            case DotNetWorkKind.SetDotNetSolution:
                return Do_SetDotNetSolution(workArgs.DotNetSolutionAbsolutePath);
            case DotNetWorkKind.SetDotNetSolutionTreeView:
                return Do_SetDotNetSolutionTreeView(workArgs.DotNetSolutionModelKey);
            case DotNetWorkKind.Website_AddExistingProjectToSolution:
                return Do_Website_AddExistingProjectToSolution(
                    workArgs.DotNetSolutionModelKey,
                    workArgs.ProjectTemplateShortName,
                    workArgs.CSharpProjectName,
                    workArgs.CSharpProjectAbsolutePath);
            default:
                Console.WriteLine($"{nameof(DotNetService)} {nameof(HandleEvent)} default case");
                return ValueTask.CompletedTask;
        }
    }
    
    public void Dispose()
    {
    }
}
