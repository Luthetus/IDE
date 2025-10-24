using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Widgets.Models;
using Clair.Extensions.DotNet.CommandLines.Models;
using Clair.Extensions.DotNet.CSharpProjects.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Models;
using Clair.Ide.RazorLib;
using Clair.Ide.RazorLib.InputFiles.Models;
using Clair.Ide.RazorLib.BackgroundTasks.Models;
using Clair.Ide.RazorLib.Terminals.Models;

namespace Clair.Extensions.DotNet;

public partial class DotNetService
{
    public MenuOptionRecord RemoveCSharpProjectReferenceFromSolution(
        SolutionExplorerTreeViewContainer container,
        AbsolutePath projectNode,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        return new MenuOptionRecord(
            "Remove (no files are deleted)",
            MenuOptionKind.Delete,
            menuOptionOnClickArgs => 
            {
                MenuRecord.OpenWidget(
                    CommonService,
                    menuOptionOnClickArgs.MenuMeasurements,
                    menuOptionOnClickArgs.TopOffsetOptionFromMenu,
                    elementIdToRestoreFocusToOnClose: menuOptionOnClickArgs.MenuHtmlId,
                    SimpleWidgetKind.RemoveCSharpProjectFromSolution,
                    isDirectory: default,
                    checkForTemplates: false,
                    fileName: string.Empty,
                    onAfterSubmitFuncAbsolutePathTask: new Func<AbsolutePath, Task>(
                        _ =>
                        {
                            Enqueue_PerformRemoveCSharpProjectReferenceFromSolution(
                                treeViewSolution,
                                projectNode,
                                terminal,
                                commonService,
                                onAfterCompletion);

                            return Task.CompletedTask;
                        }),
                    onAfterSubmitFuncOther: null,
                    absolutePath: projectNode.Item);
                    
                return Task.CompletedTask;
            })
            {
                IconKind = AutocompleteEntryKind.Widget,
            };;
    }

    public MenuOptionRecord AddProjectToProjectReference(
        AbsolutePath projectReceivingReference,
        ITerminal terminal,
        IdeService ideService,
        Func<Task> onAfterCompletion)
    {
        return new MenuOptionRecord("Add Project Reference", MenuOptionKind.Other,
            onClickFunc:
            _ =>
            {
                PerformAddProjectToProjectReference(
                    projectReceivingReference,
                    terminal,
                    ideService,
                    onAfterCompletion);

                return Task.CompletedTask;
            });
    }

    public MenuOptionRecord RemoveProjectToProjectReference(
        TreeViewCSharpProjectToProjectReference treeViewCSharpProjectToProjectReference,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        return new MenuOptionRecord("Remove Project Reference", MenuOptionKind.Other,
            onClickFunc:
                _ =>
                {
                    Enqueue_PerformRemoveProjectToProjectReference(
                        treeViewCSharpProjectToProjectReference,
                        terminal,
                        commonService,
                        onAfterCompletion);

                    return Task.CompletedTask;
                });
    }

    public MenuOptionRecord MoveProjectToSolutionFolder(
        SolutionExplorerTreeViewContainer container,
        TreeViewNamespacePath treeViewProjectToMove,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        return new MenuOptionRecord(
            "Move to Solution Folder",
            MenuOptionKind.Other,
            menuOptionOnClickArgs => 
            {
                MenuRecord.OpenWidget(
                    CommonService,
                    menuOptionOnClickArgs.MenuMeasurements,
                    menuOptionOnClickArgs.TopOffsetOptionFromMenu,
                    elementIdToRestoreFocusToOnClose: menuOptionOnClickArgs.MenuHtmlId,
                    SimpleWidgetKind.FileForm,
                    isDirectory: false,
                    checkForTemplates: false,
                    fileName: string.Empty,
                    onAfterSubmitFuncAbsolutePathTask: null,
                    onAfterSubmitFuncOther: new Func<string, FileTemplate?, List<FileTemplate>, Task>((nextName, _, _) =>
                        {
                            Enqueue_PerformMoveProjectToSolutionFolder(
                                treeViewSolution,
                                treeViewProjectToMove,
                                nextName,
                                terminal,
                                commonService,
                                onAfterCompletion);
        
                            return Task.CompletedTask;
                        }),
                    absolutePath: default);
                    
                return Task.CompletedTask;
            })
            {
                IconKind = AutocompleteEntryKind.Widget,
            };;
    }

    public MenuOptionRecord RemoveNuGetPackageReferenceFromProject(
        AbsolutePath modifyProjectAbsolutePath,
        string namespaceString,
        TreeViewCSharpProjectNugetPackageReference treeViewCSharpProjectNugetPackageReference,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        return new MenuOptionRecord("Remove NuGet Package Reference", MenuOptionKind.Other,
            onClickFunc: _ =>
            {
                Enqueue_PerformRemoveNuGetPackageReferenceFromProject(
                    modifyProjectAbsolutePath,
                    namespaceString,
                    treeViewCSharpProjectNugetPackageReference,
                    terminal,
                    commonService,
                    onAfterCompletion);

                return Task.CompletedTask;
            });
    }

    private void Enqueue_PerformRemoveCSharpProjectReferenceFromSolution(
        SolutionExplorerTreeViewContainer container,
        AbsolutePath projectNode,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        Enqueue(new DotNetWorkArgs
        {
            WorkKind = DotNetWorkKind.PerformRemoveCSharpProjectReferenceFromSolution,
            TreeViewSolution = treeViewSolution,
            ProjectNode = projectNode,
            Terminal = terminal,
            OnAfterCompletion = onAfterCompletion
        });
    }

    private ValueTask Do_PerformRemoveCSharpProjectReferenceFromSolution(
        SolutionExplorerTreeViewContainer container,
        AbsolutePath projectNode,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        var workingDirectory = treeViewSolution.Item.AbsolutePath.CreateSubstringParentDirectory();
        if (workingDirectory is null)
            return ValueTask.CompletedTask;

        var formattedCommandValue = DotNetCliCommandFormatter.FormatRemoveCSharpProjectReferenceFromSolutionAction(
            treeViewSolution.Item.AbsolutePath.Value,
            projectNode.Item.Value);

        var terminalCommandRequest = new TerminalCommandRequest(
            formattedCommandValue,
            workingDirectory)
        {
            ContinueWithFunc = parsedCommand => onAfterCompletion.Invoke()
        };

        terminal.EnqueueCommand(terminalCommandRequest);
        return ValueTask.CompletedTask;
    }

    public void PerformAddProjectToProjectReference(
        TreeViewNamespacePath projectReceivingReference,
        ITerminal terminal,
        IdeService ideService,
        Func<Task> onAfterCompletion)
    {
        ideService.Enqueue(new IdeWorkArgs
        {
            WorkKind = IdeWorkKind.RequestInputFileStateForm,
            StringValue = $"Add Project reference to {projectReceivingReference.Item.Name}",
            OnAfterSubmitFunc = referencedProject =>
            {
                if (referencedProject.Value is null)
                    return Task.CompletedTask;

                var formattedCommandValue = DotNetCliCommandFormatter.FormatAddProjectToProjectReference(
                    projectReceivingReference.Item.Value,
                    referencedProject.Value);

                var terminalCommandRequest = new TerminalCommandRequest(
                    formattedCommandValue,
                    null)
                {
                    ContinueWithFunc = parsedCommand =>
                    {
                        CommonFacts.DispatchInformative("Add Project Reference", $"Modified {projectReceivingReference.Item.Name} to have a reference to {referencedProject.Name}", ideService.CommonService, TimeSpan.FromSeconds(7));
                        return onAfterCompletion.Invoke();
                    }
                };

                terminal.EnqueueCommand(terminalCommandRequest);
                return Task.CompletedTask;
            },
            SelectionIsValidFunc = absolutePath =>
            {
                if (absolutePath.Value is null || absolutePath.IsDirectory)
                    return Task.FromResult(false);

                return Task.FromResult(
                    absolutePath.Name.EndsWith(CommonFacts.C_SHARP_PROJECT));
            },
            InputFilePatterns = new()
            {
                new InputFilePattern(
                    "C# Project",
                    absolutePath => absolutePath.Name.EndsWith(CommonFacts.C_SHARP_PROJECT))
            }
        });
    }

    public void Enqueue_PerformRemoveProjectToProjectReference(
        AbsolutePath modifyProjectAbsolutePath,
        AbsolutePath referenceProjectAbsolutePath,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        Enqueue(new DotNetWorkArgs
        {
            WorkKind = DotNetWorkKind.PerformRemoveProjectToProjectReference,
            ModifyProjectAbsolutePath = modifyProjectAbsolutePath,
            ReferenceProjectAbsolutePath = referenceProjectAbsolutePath,
            Terminal = terminal,
            OnAfterCompletion = onAfterCompletion
        });
    }

    public ValueTask Do_PerformRemoveProjectToProjectReference(
        AbsolutePath modifyProjectAbsolutePath,
        AbsolutePath referenceProjectAbsolutePath,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        var formattedCommandValue = DotNetCliCommandFormatter.FormatRemoveProjectToProjectReference(
            modifyProjectAbsolutePath.Value,
            referenceProjectAbsolutePath.Value);

        var terminalCommandRequest = new TerminalCommandRequest(
            formattedCommandValue,
            null)
        {
            ContinueWithFunc = parsedCommand =>
            {
                CommonFacts.DispatchInformative("Remove Project Reference", $"Modified {treeViewCSharpProjectToProjectReference.Item.ModifyProjectAbsolutePath.Name} to have a reference to {treeViewCSharpProjectToProjectReference.Item.ReferenceProjectAbsolutePath.Name}", commonService, TimeSpan.FromSeconds(7));
                return onAfterCompletion.Invoke();
            }
        };

        terminal.EnqueueCommand(terminalCommandRequest);
        return ValueTask.CompletedTask;
    }

    public void Enqueue_PerformMoveProjectToSolutionFolder(
        SolutionExplorerTreeViewContainer container,
        AbsolutePath treeViewProjectToMove,
        string solutionFolderPath,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        Enqueue(new DotNetWorkArgs
        {
            WorkKind = DotNetWorkKind.PerformMoveProjectToSolutionFolder,
            TreeViewSolution = treeViewSolution,
            TreeViewProjectToMove = treeViewProjectToMove,
            SolutionFolderPath = solutionFolderPath,
            Terminal = terminal,
            OnAfterCompletion = onAfterCompletion
        });
    }

    public ValueTask Do_PerformMoveProjectToSolutionFolder(
        SolutionExplorerTreeViewContainer container,
        AbsolutePath treeViewProjectToMove,
        string solutionFolderPath,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        var formattedCommandValue = DotNetCliCommandFormatter.FormatMoveProjectToSolutionFolder(
            treeViewSolution.Item.AbsolutePath.Value,
            treeViewProjectToMove.Value,
            solutionFolderPath);

        var terminalCommandRequest = new TerminalCommandRequest(
            formattedCommandValue,
            null)
        {
            ContinueWithFunc = parsedCommand =>
            {
                CommonFacts.DispatchInformative("Move Project To Solution Folder", $"Moved {treeViewProjectToMove.Name} to the Solution Folder path: {solutionFolderPath}", commonService, TimeSpan.FromSeconds(7));
                return onAfterCompletion.Invoke();
            }
        };

        Enqueue_PerformRemoveCSharpProjectReferenceFromSolution(
            treeViewSolution,
            treeViewProjectToMove,
            terminal,
            commonService,
            () =>
            {
                terminal.EnqueueCommand(terminalCommandRequest);
                return Task.CompletedTask;
            });

        return ValueTask.CompletedTask;
    }

    public void Enqueue_PerformRemoveNuGetPackageReferenceFromProject(
        AbsolutePath modifyProjectAbsolutePath,
        string namespaceString,
        string namespaceString,
        string cSharpProjectAbsolutePathString,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        Enqueue(new DotNetWorkArgs
        {
            WorkKind = DotNetWorkKind.PerformRemoveNuGetPackageReferenceFromProject,
            ModifyProjectAbsolutePath = modifyProjectAbsolutePath,
            ModifyProjectNamespaceString = namespaceString,
            TreeViewCSharpProjectNugetPackageReference = treeViewCSharpProjectNugetPackageReference,
            Terminal = terminal,
            OnAfterCompletion = onAfterCompletion
        });
    }

    public ValueTask Do_PerformRemoveNuGetPackageReferenceFromProject(
        AbsolutePath modifyProjectAbsolutePath,
        string namespaceString,
        string cSharpProjectAbsolutePathString,
        LightWeightNugetPackageRecord lightWeightNugetPackageRecord,
        ITerminal terminal,
        CommonService commonService,
        Func<Task> onAfterCompletion)
    {
        var formattedCommandValue = DotNetCliCommandFormatter.FormatRemoveNugetPackageReferenceFromProject(
            modifyProjectAbsolutePath.Value,
            lightWeightNugetPackageRecord.Id);

        var terminalCommandRequest = new TerminalCommandRequest(
            formattedCommandValue,
            null)
        {
            ContinueWithFunc = parsedCommand =>
            {
                CommonFacts.DispatchInformative("Remove Project Reference", $"Modified {modifyProjectAbsolutePath.Name} to NOT have a reference to {lightWeightNugetPackageRecord.Id}", commonService, TimeSpan.FromSeconds(7));
                return onAfterCompletion.Invoke();
            }
        };

        terminal.EnqueueCommand(terminalCommandRequest);
        return ValueTask.CompletedTask;
    }
}