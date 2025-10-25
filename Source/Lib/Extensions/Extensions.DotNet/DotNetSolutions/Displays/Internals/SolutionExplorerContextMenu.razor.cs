using System.Text;
using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Dialogs.Models;
using Clair.Common.RazorLib.Commands.Models;
using Clair.Common.RazorLib.TreeViews.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.CompilerServices.DotNetSolution.Models;
using Clair.Ide.RazorLib.InputFiles.Models;
using Clair.Ide.RazorLib.Terminals.Models;
using Clair.Ide.RazorLib.BackgroundTasks.Models;
using Clair.Extensions.DotNet.CSharpProjects.Displays;
using Clair.Extensions.DotNet.CommandLines.Models;
using Clair.Extensions.DotNet.DotNetSolutions.Models;

namespace Clair.Extensions.DotNet.DotNetSolutions.Displays.Internals;

public partial class SolutionExplorerContextMenu : ComponentBase
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;

    [Parameter, EditorRequired]
    public SolutionExplorerContextMenuData SolutionExplorerContextMenuData { get; set; }

    private static readonly Key<IDynamicViewModel> _solutionPropertiesDialogKey = Key<IDynamicViewModel>.NewKey();
    private static readonly Key<IDynamicViewModel> _newCSharpProjectDialogKey = Key<IDynamicViewModel>.NewKey();

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    private (SolutionExplorerContextMenuData solutionExplorerContextMenuData, MenuRecord menuRecord) _previousGetMenuRecordInvocation;

    private MenuRecord GetMenuRecord(SolutionExplorerContextMenuData data)
    {
        /*_previousGetMenuRecordInvocation = (solutionExplorerContextMenuData, new MenuRecord(MenuRecord.NoMenuOptionsExistList));
        return _previousGetMenuRecordInvocation.menuRecord;*/

        if (_previousGetMenuRecordInvocation.solutionExplorerContextMenuData == data)
            return _previousGetMenuRecordInvocation.menuRecord;

        //if (data.TreeViewContainer.SelectedNodeList.Count > 1)
        //    return GetMenuRecordManySelections(data);

        if (data.IndexNodeValue == -1 ||
            data.IndexNodeValue >= data.TreeViewContainer.NodeValueList.Count)
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (data, menuRecord);
            return menuRecord;
        }

        var menuOptionList = new List<MenuOptionRecord>();
        var treeViewModel = data.TreeViewContainer.NodeValueList[data.IndexNodeValue];
        
        TreeViewNodeValue parentTreeViewModel;
        if (treeViewModel.ParentIndex == -1)
            parentTreeViewModel = default;
        else
            parentTreeViewModel = data.TreeViewContainer.NodeValueList[treeViewModel.ParentIndex];

        if (data.TreeViewContainer is SolutionExplorerTreeViewContainer container)
        {
            switch (treeViewModel.ByteKind)
            {
                case SolutionExplorerTreeViewContainer.ByteKind_Solution:
                    
                    var dotNetSolutionModel = container.DotNetSolutionModel;
                    if (container.DotNetSolutionModel.AbsolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION) ||
                        container.DotNetSolutionModel.AbsolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION_X))
                    {
                        if (treeViewModel.ParentIndex == -1)
                            menuOptionList.AddRange(GetDotNetSolutionMenuOptions(dotNetSolutionModel));
                    }
                    break;
                case SolutionExplorerTreeViewContainer.ByteKind_SolutionFolder:
                    break;
                case SolutionExplorerTreeViewContainer.ByteKind_Csproj:
                    menuOptionList.AddRange(GetCSharpProjectMenuOptions(
                        container,
                        data.IndexNodeValue,
                        container.DotNetSolutionModel.DotNetProjectList[treeViewModel.TraitsIndex],
                        treeViewModel,
                        parentTreeViewModel)
                        /*.Union(GetDebugMenuOptions(treeViewNamespacePath))*/);
                    break;
                case SolutionExplorerTreeViewContainer.ByteKind_Dir:
                    var absolutePath = container.DirectoryTraitsList[treeViewModel.TraitsIndex];
                    menuOptionList.AddRange(GetFileMenuOptions(container, absolutePath, treeViewModel, parentTreeViewModel)
                        .Union(GetDirectoryMenuOptions(container, data.IndexNodeValue, absolutePath, treeViewModel, parentTreeViewModel))
                        /*.Union(GetDebugMenuOptions(treeViewNamespacePath))*/);
                    break;
                case SolutionExplorerTreeViewContainer.ByteKind_File:
                    menuOptionList.AddRange(GetFileMenuOptions(
                        container,
                        container.FileTraitsList[treeViewModel.TraitsIndex],
                        treeViewModel,
                        parentTreeViewModel)
                        /*.Union(GetDebugMenuOptions(treeViewNamespacePath))*/);
                    break;
                default:
                    break;
            }
        }
        
        /*var parentTreeViewNamespacePath = parentTreeViewModel as TreeViewNamespacePath;

        if (treeViewModel is TreeViewNamespacePath treeViewNamespacePath)
        {
            if (treeViewNamespacePath.Item.IsDirectory)
            {
                menuOptionList.AddRange(GetFileMenuOptions(treeViewNamespacePath, parentTreeViewNamespacePath)
                    .Union(GetDirectoryMenuOptions(treeViewNamespacePath))
                    .Union(GetDebugMenuOptions(treeViewNamespacePath)));
            }
            else
            {
                if (treeViewNamespacePath.Item.Name.EndsWith(CommonFacts.C_SHARP_PROJECT))
                {
                    menuOptionList.AddRange(GetCSharpProjectMenuOptions(treeViewNamespacePath)
                        .Union(GetDebugMenuOptions(treeViewNamespacePath)));
                }
                else
                {
                    menuOptionList.AddRange(GetFileMenuOptions(treeViewNamespacePath, parentTreeViewNamespacePath)
                        .Union(GetDebugMenuOptions(treeViewNamespacePath)));
                }
            }
        }
        else if (treeViewModel is TreeViewSolution treeViewSolution)
        {
            if (treeViewSolution.Item.AbsolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION) ||
                treeViewSolution.Item.AbsolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION_X))
            {
                if (treeViewSolution.Parent is null || treeViewSolution.Parent is TreeViewAdhoc)
                    menuOptionList.AddRange(GetDotNetSolutionMenuOptions(treeViewSolution));
            }
        }
        else if (treeViewModel is TreeViewCSharpProjectToProjectReference treeViewCSharpProjectToProjectReference)
        {
            menuOptionList.AddRange(GetCSharpProjectToProjectReferenceMenuOptions(
                treeViewCSharpProjectToProjectReference));
        }
        else if (treeViewModel is TreeViewCSharpProjectNugetPackageReference treeViewCSharpProjectNugetPackageReference)
        {
            menuOptionList.AddRange(GetTreeViewLightWeightNugetPackageRecordMenuOptions(
                treeViewCSharpProjectNugetPackageReference));
        }*/

        if (!menuOptionList.Any())
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (data, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuRecord(menuOptionList);
            _previousGetMenuRecordInvocation = (data, menuRecord);
            return menuRecord;
        }
    }

    private MenuRecord GetMenuRecordManySelections(TreeViewCommandArgs solutionExplorerContextMenuData)
    {
        return new MenuRecord(MenuRecord.NoMenuOptionsExistList);

        /*
        var menuOptionList = new List<MenuOptionRecord>();

        var getFileOptions = true;
        var filenameList = new List<string>();

        foreach (var selectedNode in solutionExplorerContextMenuData.TreeViewContainer.SelectedNodeList)
        {
            if (selectedNode is TreeViewNamespacePath treeViewNamespacePath)
            {
                if (treeViewNamespacePath.Item.Name.EndsWith(CommonFacts.C_SHARP_PROJECT))
                    getFileOptions = false;
                else if (getFileOptions)
                    filenameList.Add(treeViewNamespacePath.Item.Name + " __FROM__ " + (treeViewNamespacePath.Item.CreateSubstringParentDirectory() ?? "null"));
            }
            else
            {
                getFileOptions = false;
            }
        }

        if (getFileOptions)
        {
            menuOptionList.Add(newClairuOptionRecord(
                "Delete",
                MenuOptionKind.Delete/*,
                simpleWidgetKind: Walk.Common.RazorLib.Widgets.Models.SimpleWidgetKind.BooleanPromptOrCancel,
                widgetParameterMap: new Dictionary<string, object?>
                {
                    { nameof(BooleanPromptOrCancelDisplay.IncludeCancelOption), false },
                    { nameof(BooleanPromptOrCancelDisplay.Message), $"DELETE:" },
                    { nameof(BooleanPromptOrCancelDisplay.ListOfMessages), filenameList },
                    { nameof(BooleanPromptOrCancelDisplay.AcceptOptionTextOverride), null },
                    { nameof(BooleanPromptOrCancelDisplay.DeclineOptionTextOverride), null },
                    {
                        nameof(BooleanPromptOrCancelDisplay.OnAfterAcceptFunc),
                        async () =>
                        {
                            await solutionExplorerContextMenuData.RestoreFocusToTreeView
                                .Invoke()
                                .ConfigureAwait(false);

                            DotNetService.Enqueue(new DotNetWorkArgs
                            {
                                WorkKind = DotNetWorkKind.SolutionExplorer_TreeView_MultiSelect_DeleteFiles,
                                TreeViewCommandArgs = solutionExplorerContextMenuData,
                            });
                        }
                    },
                    { nameof(BooleanPromptOrCancelDisplay.OnAfterDeclineFunc), solutionExplorerContextMenuData.RestoreFocusToTreeView },
                    { nameof(BooleanPromptOrCancelDisplay.OnAfterCancelFunc), solutionExplorerContextMenuData.RestoreFocusToTreeView },
                }*//*));
        }

        if (!menuOptionList.Any())
        {
            var menuRecord = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            _previousGetMenuRecordInvocation = (solutionExplorerContextMenuData, menuRecord);
            return menuRecord;
        }

        // Default case
        {
            var menuRecord = new MenuRecord(menuOptionList);
            _previousGetMenuRecordInvocation = (solutionExplorerContextMenuData, menuRecord);
            return menuRecord;
        }*/
    }

    private MenuOptionRecord[] GetDotNetSolutionMenuOptions(DotNetSolutionModel dotNetSolutionModel)
    {
        // TODO: Add menu options for non C# projects perhaps a more generic option is good

        var addNewCSharpProject = new MenuOptionRecord(
            "New C# Project",
            MenuOptionKind.Other,
            _ => OpenNewCSharpProjectDialog(dotNetSolutionModel));

        var addExistingCSharpProject = new MenuOptionRecord(
            "Existing C# Project",
            MenuOptionKind.Other,
            _ =>
            {
                AddExistingProjectToSolution(dotNetSolutionModel);
                return Task.CompletedTask;
            });

        var createOptions = new MenuOptionRecord(
            "Add",
            MenuOptionKind.Create,
            menuOptionOnClickArgs =>
            {
                MenuRecord.OpenSubMenu(
                    DotNetService.CommonService,
                    subMenu: new MenuRecord(new List<MenuOptionRecord>
                    {
                        addNewCSharpProject,
                        addExistingCSharpProject,
                    }),
                    menuOptionOnClickArgs.MenuMeasurements,
                    menuOptionOnClickArgs.TopOffsetOptionFromMenu,
                    elementIdToRestoreFocusToOnClose: menuOptionOnClickArgs.MenuHtmlId);

                return Task.CompletedTask;
            })
        {
            IconKind = AutocompleteEntryKind.Chevron
        };

        var openInTextEditor = new MenuOptionRecord(
            "Open in text editor",
            MenuOptionKind.Update,
            _ => OpenSolutionInTextEditor(dotNetSolutionModel));

        var properties = new MenuOptionRecord(
            "Properties",
            MenuOptionKind.Update,
            _ => OpenSolutionProperties(dotNetSolutionModel));

        return new[]
        {
            createOptions,
            openInTextEditor,
            properties,
        };
    }

    private MenuOptionRecord[] GetCSharpProjectMenuOptions(
        SolutionExplorerTreeViewContainer container,
        int indexNodeValue,
        Clair.CompilerServices.DotNetSolution.Models.Project.IDotNetProject project,
        TreeViewNodeValue treeViewModel,
        TreeViewNodeValue parentTreeViewModel)
    {
        var absolutePath = project.AbsolutePath;
    
        var parentDirectory = absolutePath.CreateSubstringParentDirectory();
        if (parentDirectory is null)
            return Array.Empty<MenuOptionRecord>();

        var treeViewSolution = container.DotNetSolutionModel;

        var parentDirectoryAbsolutePath = new AbsolutePath(
            parentDirectory,
            true,
            DotNetService.IdeService.TextEditorService.CommonService.FileSystemProvider,
            tokenBuilder: new StringBuilder(),
            formattedBuilder: new StringBuilder(),
            AbsolutePathNameKind.NameWithExtension);

        return new[]
        {
            DotNetService.IdeService.NewEmptyFile(
                parentDirectoryAbsolutePath,
                async () => await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false)),
            DotNetService.IdeService.NewTemplatedFile(
                parentDirectoryAbsolutePath,
                () => GetNamespaceString(container, treeViewModel),
                async () => await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false)),
            DotNetService.IdeService.NewDirectory(
                parentDirectoryAbsolutePath,
                async () => await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false)),
            /*DotNetService.IdeService.PasteClipboard(
                parentDirectoryAbsolutePath,
                async () =>
                {
                    var localParentOfCutFile = DotNetService.CommonService.ParentOfCutFile;
                    DotNetService.CommonService.ParentOfCutFile = null;

                    if (localParentOfCutFile is TreeViewNamespacePath parentTreeViewNamespacePath)
                        await ReloadTreeViewModel(parentTreeViewNamespacePath).ConfigureAwait(false);

                    await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false);
                }),*/
            /*DotNetService.AddProjectToProjectReference(
                treeViewModel,
                DotNetService.IdeService.GetTerminalState().GeneralTerminal,
                DotNetService.IdeService,
                () => Task.CompletedTask),*/
            /*DotNetService.MoveProjectToSolutionFolder(
                treeViewSolution,
                treeViewModel,
                DotNetService.IdeService.GetTerminalState().GeneralTerminal,
                DotNetService.IdeService.TextEditorService.CommonService,
                () =>
                {
                    DotNetService.Enqueue(new DotNetWorkArgs
                    {
                        WorkKind = DotNetWorkKind.SetDotNetSolution,
                        DotNetSolutionAbsolutePath = treeViewSolution.AbsolutePath
                    });
                    return Task.CompletedTask;
                }),*/
            /*new MenuOptionRecord(
                "Set as Startup Project",
                MenuOptionKind.Other,
                _ =>
                {
                    var startupControl = DotNetService.IdeService.GetIdeStartupControlState().StartupControlList.FirstOrDefault(
                        x => x.StartupProjectAbsolutePath.Value == treeViewModel.Item.Value);

                    if (startupControl.StartupProjectAbsolutePath.Value is null)
                        return Task.CompletedTask;

                    DotNetService.IdeService.Ide_SetActiveStartupControlKey(startupControl.StartupProjectAbsolutePath.Value);
                    return Task.CompletedTask;
                }),*/
            /*DotNetService.RemoveCSharpProjectReferenceFromSolution(
                treeViewSolution,
                treeViewModel,
                DotNetService.IdeService.GetTerminalState().GeneralTerminal,
                DotNetService.IdeService.TextEditorService.CommonService,
                () =>
                {
                    DotNetService.Enqueue(new DotNetWorkArgs
                    {
                        WorkKind = DotNetWorkKind.SetDotNetSolution,
                        DotNetSolutionAbsolutePath = treeViewSolution.AbsolutePath,
                    });
                    return Task.CompletedTask;
                }),*/
        };
    }

    private MenuOptionRecord[] GetCSharpProjectToProjectReferenceMenuOptions(
        TreeViewNodeValue treeViewCSharpProjectToProjectReference)
    {
        /*
        return new[]
        {
            DotNetService.RemoveProjectToProjectReference(
                treeViewCSharpProjectToProjectReference,
                DotNetService.IdeService.GetTerminalState().GeneralTerminal,
                DotNetService.IdeService.TextEditorService.CommonService,
                () => Task.CompletedTask),
        };
        */
        return Array.Empty<MenuOptionRecord>();
    }

    private IReadOnlyList<MenuOptionRecord> GetTreeViewLightWeightNugetPackageRecordMenuOptions(
        TreeViewNodeValue treeViewCSharpProjectNugetPackageReference)
    {
        /*
        if (treeViewCSharpProjectNugetPackageReference.Parent
                is not TreeViewCSharpProjectNugetPackageReferences treeViewCSharpProjectNugetPackageReferences)
        {
            return MenuRecord.NoMenuOptionsExistList;
        }

        return new List<MenuOptionRecord>
        {
            DotNetService.RemoveNuGetPackageReferenceFromProject(
                treeViewCSharpProjectNugetPackageReferences.Item.CSharpProjectAbsolutePath,
                string.Empty,
                treeViewCSharpProjectNugetPackageReference,
                DotNetService.IdeService.GetTerminalState().GeneralTerminal,
                DotNetService.IdeService.TextEditorService.CommonService,
                () => Task.CompletedTask),
        };
        */

        return Array.Empty<MenuOptionRecord>();
    }

    private MenuOptionRecord[] GetDirectoryMenuOptions(
        SolutionExplorerTreeViewContainer container,
        int indexNodeValue,
        AbsolutePath absolutePath,
        TreeViewNodeValue treeViewModel,
        TreeViewNodeValue parentTreeViewModel)
    {
        return new[]
        {
            DotNetService.IdeService.NewEmptyFile(
                absolutePath,
                async () => await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false)),
            DotNetService.IdeService.NewTemplatedFile(
                absolutePath,
                () => GetNamespaceString(container, treeViewModel),
                async () => await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false)),
            DotNetService.IdeService.NewDirectory(
                absolutePath,
                async () => await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false)),
            DotNetService.IdeService.PasteClipboard(
                absolutePath,
                async () =>
                {
                    /*if (DotNetService.CommonService.IndexParentOfCutFile >= 0 &&
                        DotNetService.CommonService.IndexParentOfCutFile < container.NodeValueList.Count)
                    {
                        var localParentOfCutFile = container.NodeValueList[DotNetService.CommonService.IndexParentOfCutFile];
                        if (localParentOfCutFile is TreeViewNamespacePath parentTreeViewNamespacePath)
                            await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false);
    
                        await ReloadTreeViewModel(container, indexNodeValue).ConfigureAwait(false);
                    }
                    
                    DotNetService.CommonService.IndexParentOfCutFile = -1;*/
                }),
        };
    }

    private string GetNamespaceString(SolutionExplorerTreeViewContainer container, TreeViewNodeValue treeViewModel)
    {
        var targetNode = treeViewModel;
    
        if (targetNode.ByteKind != SolutionExplorerTreeViewContainer.ByteKind_Csproj &&
            targetNode.ByteKind != SolutionExplorerTreeViewContainer.ByteKind_Dir)
        {
            return string.Empty;
        }
        
        // The upcoming algorithm has a lot of "shifting" due to 0 index insertions and likely is NOT the most optimal solution.
        StringBuilder namespaceBuilder;
        if (targetNode.ByteKind == SolutionExplorerTreeViewContainer.ByteKind_Csproj)
        {
            namespaceBuilder = new StringBuilder(container.DotNetSolutionModel.DotNetProjectList[targetNode.TraitsIndex].AbsolutePath.Name);
        }
        else if (targetNode.ByteKind == SolutionExplorerTreeViewContainer.ByteKind_Dir)
        {
            namespaceBuilder = new StringBuilder(container.DirectoryTraitsList[targetNode.TraitsIndex].Name);
        }
        else
        {
            throw new NotImplementedException($"{nameof(TreeViewNodeValue.ByteKind)} of {targetNode.ByteKind} is not supported. Check {nameof(SolutionExplorerTreeViewContainer)} for the supported 'ByteKind_...' values.");
        }
        
        // for loop is an arbitrary "while-loop limit" until I prove to myself this won't infinite loop.
        for (int i = 0; i < 256; i++)
        {
            if (targetNode.IsDefault())
                break;

            // EndsWith check includes the period to ensure a direct match on the extension rather than a substring.
            if (targetNode.ByteKind == SolutionExplorerTreeViewContainer.ByteKind_Csproj)
            {
                if (i != 0)
                {
                    namespaceBuilder.Insert(0, '.');
                    // This insertion is duplicated when invoking the StringBuilder constructor for initial capacity.
                    namespaceBuilder.Insert(0, container.DotNetSolutionModel.DotNetProjectList[targetNode.TraitsIndex].AbsolutePath.Name.Replace(".csproj", string.Empty));
                }
                break;
            }
            else
            {
                if (i != 0)
                {
                    namespaceBuilder.Insert(0, '.');
                    // This insertion is duplicated when invoking the StringBuilder constructor for initial capacity.
                    namespaceBuilder.Insert(0, container.DirectoryTraitsList[targetNode.TraitsIndex].Name);
                }
                
                if (targetNode.ParentIndex == -1)
                {
                    break;
                }
                else
                {
                    targetNode = container.NodeValueList[targetNode.ParentIndex];
                    if (targetNode.ByteKind != SolutionExplorerTreeViewContainer.ByteKind_Csproj &&
                        targetNode.ByteKind != SolutionExplorerTreeViewContainer.ByteKind_Dir)
                    {
                        break;
                    }
                }
            }
        }

        return namespaceBuilder.ToString();
    }

    private MenuOptionRecord[] GetFileMenuOptions(
        SolutionExplorerTreeViewContainer container,
        AbsolutePath absolutePath,
        TreeViewNodeValue treeViewModel,
        TreeViewNodeValue parentTreeViewModel)
    {
        return new[]
        {
            DotNetService.IdeService.CopyFile(
                absolutePath,
                (Func<Task>)(() => {
                    CommonFacts.DispatchInformative("Copy Action", $"Copied: {absolutePath.Name}", DotNetService.IdeService.TextEditorService.CommonService, TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                })),
            DotNetService.IdeService.CutFile(
                absolutePath,
                (Func<Task>)(() => {
                    //DotNetService.CommonService.ParentOfCutFile = parentTreeViewModel;
                    CommonFacts.DispatchInformative("Cut Action", $"Cut: {absolutePath.Name}", DotNetService.IdeService.TextEditorService.CommonService, TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                })),
            DotNetService.IdeService.DeleteFile(
                absolutePath,
                async () => await ReloadTreeViewModel(container, treeViewModel.ParentIndex).ConfigureAwait(false)),
            DotNetService.IdeService.RenameFile(
                absolutePath,
                DotNetService.IdeService.TextEditorService.CommonService,
                async ()  => await ReloadTreeViewModel(container, treeViewModel.ParentIndex).ConfigureAwait(false)),
        };
    }

    private MenuOptionRecord[] GetDebugMenuOptions(TreeViewNodeValue treeViewModel)
    {
        /*
        return new MenuOptionRecord[]
        {
            // new MenuOptionRecord(
            //     $"namespace: {treeViewModel.Item.Namespace}",
            //     MenuOptionKind.Read)
        };
        */
        return Array.Empty<MenuOptionRecord>();
    }

    private Task OpenNewCSharpProjectDialog(DotNetSolutionModel dotNetSolutionModel)
    {
        var dialogRecord = new DialogViewModel(
            _newCSharpProjectDialogKey,
            "New C# Project",
            typeof(CSharpProjectFormDisplay),
            new Dictionary<string, object?>
            {
                {
                    nameof(CSharpProjectFormDisplay.DotNetSolutionModelKey),
                    dotNetSolutionModel.Key
                },
            },
            null,
            true,
            null);

        DotNetService.IdeService.TextEditorService.CommonService.Dialog_ReduceRegisterAction(dialogRecord);
        return Task.CompletedTask;
    }

    private void AddExistingProjectToSolution(DotNetSolutionModel dotNetSolutionModel)
    {
        DotNetService.IdeService.Enqueue(new IdeWorkArgs
        {
            WorkKind = IdeWorkKind.RequestInputFileStateForm,
            StringValue = "Existing C# Project to add to solution",
            OnAfterSubmitFunc = absolutePath =>
            {
                if (absolutePath.Value is null)
                    return Task.CompletedTask;

                var localFormattedAddExistingProjectToSolutionCommandValue = DotNetCliCommandFormatter.FormatAddExistingProjectToSolution(
                    dotNetSolutionModel.AbsolutePath.Value,
                    absolutePath.Value);

                var terminalCommandRequest = new TerminalCommandRequest(
                    localFormattedAddExistingProjectToSolutionCommandValue,
                    null)
                {
                    ContinueWithFunc = parsedCommand =>
                    {
                        DotNetService.Enqueue(new DotNetWorkArgs
                        {
                            WorkKind = DotNetWorkKind.SetDotNetSolution,
                            DotNetSolutionAbsolutePath = dotNetSolutionModel.AbsolutePath,
                        });
                        return Task.CompletedTask;
                    }
                };

                DotNetService.IdeService.GetTerminalState().GeneralTerminal.EnqueueCommand(terminalCommandRequest);
                return Task.CompletedTask;
            },
            SelectionIsValidFunc = absolutePath =>
            {
                if (absolutePath.Value is null || absolutePath.IsDirectory)
                    return Task.FromResult(false);

                return Task.FromResult(absolutePath.Name.EndsWith(CommonFacts.C_SHARP_PROJECT));
            },
            InputFilePatterns = new()
            {
                new InputFilePattern(
                    "C# Project",
                    absolutePath => absolutePath.Name.EndsWith(CommonFacts.C_SHARP_PROJECT))
            }
        });
    }

    private Task OpenSolutionInTextEditor(DotNetSolutionModel dotNetSolutionModel)
    {
        DotNetService.IdeService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            await DotNetService.IdeService.TextEditorService.OpenInEditorAsync(
                editContext,
                dotNetSolutionModel.AbsolutePath.Value,
                true,
                null,
                new Category("main"),
                editContext.TextEditorService.NewViewModelKey());
        });
        return Task.CompletedTask;
    }

    private Task OpenSolutionProperties(DotNetSolutionModel dotNetSolutionModel)
    {
        DotNetService.IdeService.TextEditorService.CommonService.Dialog_ReduceRegisterAction(new DialogViewModel(
            dynamicViewModelKey: _solutionPropertiesDialogKey,
            title: "Solution Properties",
            componentType: typeof(SolutionPropertiesDisplay),
            componentParameterMap: null,
            cssClass: null,
            isResizable: true,
            setFocusOnCloseElementId: null));
        return Task.CompletedTask;
    }

    /// <summary>
    /// This method I believe is causing bugs
    /// <br/><br/>
    /// For example, when removing a C# Project the
    /// solution is reloaded and a new root is made.
    /// <br/><br/>
    /// Then there is a timing issue where the new root is made and set
    /// as the root. But this method erroneously reloads the old root.
    /// </summary>
    /// <param name="treeViewModel"></param>
    private async Task ReloadTreeViewModel(SolutionExplorerTreeViewContainer container, int indexNodeValue)
    {
        if (indexNodeValue < 0 ||
            indexNodeValue >= container.NodeValueList.Count)
        {
            return;
        }

        if (!DotNetService.CommonService.TryGetTreeViewContainer(DotNetSolutionState.TreeViewSolutionExplorerStateKey, out var treeViewContainer))
            return;

        await container.LoadChildListAsync(indexNodeValue).ConfigureAwait(false);

        DotNetService.IdeService.TextEditorService.CommonService.TreeView_MoveUpAction(
            DotNetSolutionState.TreeViewSolutionExplorerStateKey,
            false,
            false);

        /*DotNetService.IdeService.TextEditorService.CommonService.TreeView_ReRenderNodeAction(
            DotNetSolutionState.TreeViewSolutionExplorerStateKey,
            treeViewModel,
            flatListChanged: true);*/
    }
}
