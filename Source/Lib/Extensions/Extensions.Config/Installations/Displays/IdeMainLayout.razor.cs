using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Dialogs.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.Panels.Models;
using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Menus.Models;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Tabs.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Commands.Models.Defaults;
using Clair.Ide.RazorLib;
using Clair.Ide.RazorLib.Settings.Displays;
using Clair.Ide.RazorLib.Shareds.Displays.Internals;
using Clair.Ide.RazorLib.CodeSearches.Displays;
using Clair.Extensions.DotNet;
using Clair.Extensions.DotNet.DotNetSolutions.Models;

namespace Clair.Extensions.Config.Installations.Displays;

public partial class IdeMainLayout : LayoutComponentBase, IDisposable
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;
    [Inject]
    private BrowserResizeInterop BrowserResizeInterop { get; set; } = null!;
    
    // NOTE TO SELF: Don't put an event for Drag that makes the website unselectable,...
    // ...just ensure the drag start target is unselectable.
    // I'm pretty sure this works. If it doesn't make sure it isn't cause you're
    // doing drag start on a selectable element that then propagates the drag start event?
    
    /// <summary>
    /// This can only be set from the "UI thread".
    /// </summary>
    private bool _shouldRecalculateCssStrings = true;
    
    private string _classCssString;
    private string _styleCssString;
    private string _headerCssStyle;

    private PanelGroupParameter _leftPanelGroupParameter;
    private PanelGroupParameter _rightPanelGroupParameter;
    private PanelGroupParameter _bottomPanelGroupParameter;
    
    private TabCascadingValueBatch _tabCascadingValueBatch;
    
    private static readonly Key<IDynamicViewModel> _settingsDialogKey = Key<IDynamicViewModel>.NewKey();
    
    private CancellationTokenSource _workerCancellationTokenSource = new();
    
    private Func<ElementDimensions, ElementDimensions, (MouseEventArgs firstMouseEventArgs, MouseEventArgs secondMouseEventArgs), Task>? _dragEventHandler;
    private MouseEventArgs? _previousDragMouseEventArgs;
    
    private MainLayoutDragEventKind _mainLayoutDragEventKind;
    
    private bool _userInterfaceSawIsExecuting;
    
    private const string _startButtonElementId = "ci_startup-controls_id";

    private Key<DropdownRecord> _startButtonDropdownKey = Key<DropdownRecord>.NewKey();

    private Key<IDynamicViewModel> _dynamicViewModelKeyPrevious;
    
    public static readonly Key<DropdownRecord> DropdownKeyFile = Key<DropdownRecord>.NewKey();
    public const string ButtonFileId = "ci_header-button-file";

    public static readonly Key<DropdownRecord> DropdownKeyTools = Key<DropdownRecord>.NewKey();
    public const string ButtonToolsId = "ci_header-button-tools";

    public static readonly Key<DropdownRecord> DropdownKeyView = Key<DropdownRecord>.NewKey();
    public const string ButtonViewId = "ci_header-button-view";

    public static readonly Key<DropdownRecord> DropdownKeyRun = Key<DropdownRecord>.NewKey();
    public const string ButtonRunId = "ci_header-button-run";

    private string GetIsActiveCssClass(ITab localTabViewModel) => (localTabViewModel.TabGroup?.GetIsActive(localTabViewModel) ?? false)
        ? "ci_active"
        : string.Empty;

    public string? SelectedStartupControlAbsolutePathValue
    {
        get
        {
            return DotNetService.IdeService.GetIdeStartupControlState().ActiveStartupProjectAbsolutePathValue;
        }
        set
        {
            DotNetService.IdeService.Ide_SetActiveStartupControlKey(value);
        }
    }
    
    protected override void OnInitialized()
    {
        _tabCascadingValueBatch = new()
        {
            SubscribeToDragEventForScrolling = SubscribeToDragEventForScrolling,
        };
    
        InitializationHelper.InitializePanelTabs(DotNetService);
        InitializationHelper.HandleCompilerServicesAndDecorationMappers(DotNetService);

        DotNetService.TextEditorService.IdeBackgroundTaskApi = DotNetService.IdeService;

        var panelState = DotNetService.CommonService.GetPanelState();
        
        _leftPanelGroupParameter = new(
            panelGroupKey: CommonFacts.LeftPanelGroupKey,
            cssClassString: null)
        {
            PanelPositionCss = "ci_ide_panel_left",
            HtmlIdTabs = "ci_ide_panel_left_tabs"
        };

        _rightPanelGroupParameter = new(
            panelGroupKey: CommonFacts.RightPanelGroupKey,
            cssClassString: null)
        {
            PanelPositionCss = "ci_ide_panel_right",
            HtmlIdTabs = "ci_ide_panel_right_tabs"
        };

        _bottomPanelGroupParameter = new(
            panelGroupKey: CommonFacts.BottomPanelGroupKey,
            cssClassString: "ci_ide_footer")
        {
            PanelPositionCss = "ci_ide_panel_bottom",
            HtmlIdTabs = "ci_ide_panel_bottom_tabs"
        };
        
        DotNetService.CommonService.CommonUiStateChanged += OnCommonUiStateChanged;
        DotNetService.TextEditorService.SecondaryChanged += TextEditorOptionsStateWrap_StateChanged;
        DotNetService.IdeService.IdeStateChanged += OnIdeStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializationHelper.InitializeOnAfterRenderFirstRender(DotNetService, BrowserResizeInterop, _workerCancellationTokenSource);
            DotNetService.CommonService.Panel_OnUserAgent_AppDimensionStateChanged();
            await InvokeAsync(StateHasChanged);
        }
    }
    
    /// <summary>
    /// This can only be invoked from the UI thread due to the usage of `CommonBackgroundTaskApi.UiStringBuilder`.
    /// </summary>
    private void CreateCssStrings()
    {
        if (_shouldRecalculateCssStrings)
        {
            _shouldRecalculateCssStrings = false;
        
            var uiStringBuilder = DotNetService.CommonService.UiStringBuilder;
            
            uiStringBuilder.Clear();
            uiStringBuilder.Append("ci_main-layout ");
            uiStringBuilder.Append(DotNetService.CommonService.Options_ThemeCssClassString);
            uiStringBuilder.Append(" ");
            uiStringBuilder.Append(DotNetService.TextEditorService.ThemeCssClassString);
            _classCssString = uiStringBuilder.ToString();
            
            uiStringBuilder.Clear();
            uiStringBuilder.Append(DotNetService.CommonService.Options_FontSizeCssStyleString);
            uiStringBuilder.Append(" ");
            uiStringBuilder.Append(DotNetService.CommonService.Options_FontFamilyCssStyleString);
            uiStringBuilder.Append(" ");
            uiStringBuilder.Append(DotNetService.CommonService.Options_ColorSchemeCssStyleString);
            _styleCssString = uiStringBuilder.ToString();
            
            uiStringBuilder.Clear();
            uiStringBuilder.Append("display: flex; justify-content: space-between; border-bottom: ");
            uiStringBuilder.Append(DotNetService.CommonService.GetAppOptionsState().Options.ResizeHandleHeightInPixels);
            uiStringBuilder.Append("px solid var(--ci_primary-border-color);");
            uiStringBuilder.Append("height: ");
            uiStringBuilder.Append(DotNetService.CommonService.Options_LineHeight * 2);
            uiStringBuilder.Append("px;");
            _headerCssStyle = uiStringBuilder.ToString();
        }
    }
    
    private async void OnCommonUiStateChanged(CommonUiEventKind commonUiEventKind)
    {
        switch (commonUiEventKind)
        {
            case CommonUiEventKind.AppOptionsStateChanged:
                await InvokeAsync(() =>
                {
                    _shouldRecalculateCssStrings = true;
                    StateHasChanged();
                }).ConfigureAwait(false);
                break;
            case CommonUiEventKind.UserAgent_AppDimensionStateChanged:
                DotNetService.CommonService.Panel_OnUserAgent_AppDimensionStateChanged();
                await InvokeAsync(StateHasChanged);
                break;
            case CommonUiEventKind.PanelStateChanged:
                await InvokeAsync(StateHasChanged);
                break;
            case CommonUiEventKind.DragStateChanged:
                if (_dragEventHandler is not null)
                {
                    if (_mainLayoutDragEventKind == MainLayoutDragEventKind.TopLeftResizeColumn)
                    {
                        await Clair.Common.RazorLib.Resizes.Models.ResizableColumn.Do(
                            DotNetService.CommonService,
                            leftElementDimensions: null,
                            rightElementDimensions: null,
                            _dragEventHandler,
                            _previousDragMouseEventArgs,
                            x => _dragEventHandler = x,
                            x => _previousDragMouseEventArgs = x);
                    }
                    else if (_mainLayoutDragEventKind == MainLayoutDragEventKind.TopRightResizeColumn)
                    {
                        await Clair.Common.RazorLib.Resizes.Models.ResizableColumn.Do(
                            DotNetService.CommonService,
                            leftElementDimensions: null,
                            rightElementDimensions: null,
                            _dragEventHandler,
                            _previousDragMouseEventArgs,
                            x => _dragEventHandler = x,
                            x => _previousDragMouseEventArgs = x);
                    }
                    else if (_mainLayoutDragEventKind == MainLayoutDragEventKind.BottomResizeRow)
                    {
                        await Clair.Common.RazorLib.Resizes.Models.ResizableRow.Do(
                            DotNetService.CommonService,
                            topElementDimensions: null,
                            bottomElementDimensions: null,
                            _dragEventHandler,
                            _previousDragMouseEventArgs,
                            x => _dragEventHandler = x,
                            x => _previousDragMouseEventArgs = x);
                    }
                    
                    await InvokeAsync(StateHasChanged);
                }
                break;
        }
    }

    private async void TextEditorOptionsStateWrap_StateChanged(SecondaryChangedKind secondaryChangedKind)
    {
        if (secondaryChangedKind == SecondaryChangedKind.StaticStateChanged)
        {
            await InvokeAsync(() =>
            {
                _shouldRecalculateCssStrings = true;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    private string GetElementDimensionsStyleString(PanelGroup? panelGroup)
    {
        if (panelGroup?.ActiveTab is null)
        {
            return DotNetService.CommonService.Rotate_Options_LineHeight_CssStyle;
        }
    
        return panelGroup?.ElementDimensions.GetStyleString(DotNetService.CommonService.UiStringBuilder) ?? string.Empty;
    }
    
    private string GetTopPanelBodyStyle(PanelGroup? panelGroup)
    {
        if (panelGroup?.ActiveTab is null)
        {
            return "width: 0;";
        }
        
        return DotNetService.CommonService.TopPanel_Body_Options_LineHeight_CssStyle;
    }
    
    private string GetBottomPanelBodyStyle(PanelGroup? panelGroup)
    {
        if (panelGroup?.ActiveTab is null)
        {
            return "width: 0;";
        }
        
        return DotNetService.CommonService.BottomPanel_Body_Options_LineHeight_CssStyle;
    }

    private Task TopDropzoneOnMouseUp(Key<PanelGroup> panelGroupKey, MouseEventArgs mouseEventArgs)
    {
        var panelState = DotNetService.CommonService.GetPanelState();

        PanelGroup panelGroup;

        if (panelGroupKey == panelState.TopLeftPanelGroup.Key)
        {
            panelGroup = panelState.TopLeftPanelGroup;
        }
        else if (panelGroupKey == panelState.TopRightPanelGroup.Key)
        {
            panelGroup = panelState.TopRightPanelGroup;
        }
        else if (panelGroupKey == panelState.BottomPanelGroup.Key)
        {
            panelGroup = panelState.BottomPanelGroup;
        }
        else
        {
            return Task.CompletedTask;
        }

        if (panelGroup is null)
            return Task.CompletedTask;

        var dragEventArgs_PanelTab = panelState.DragEventArgs_PanelTab;
        var dragEventArgs_PanelGroup = panelState.DragEventArgs_PanelGroup;

        if (dragEventArgs_PanelTab is not null && dragEventArgs_PanelGroup is not null)
        {
            DotNetService.CommonService.DisposePanelTab(
                dragEventArgs_PanelGroup.Key,
                dragEventArgs_PanelTab.Key);

            DotNetService.CommonService.RegisterPanelTab(
                panelGroup.Key,
                dragEventArgs_PanelTab,
                true);

            DotNetService.CommonService.Panel_SetDragEventArgs(
                dragEventArgs_PanelTab: null,
                dragEventArgs_PanelGroup: null);

            DotNetService.CommonService.Drag_ShouldDisplayAndMouseEventArgsSetAction(false, null);
        }

        return Task.CompletedTask;
    }

    private Task BottomDropzoneOnMouseUp(Key<PanelGroup> panelGroupKey, MouseEventArgs mouseEventArgs)
    {
        var panelState = DotNetService.CommonService.GetPanelState();

        PanelGroup panelGroup;

        if (panelGroupKey == panelState.TopLeftPanelGroup.Key)
        {
            panelGroup = panelState.TopLeftPanelGroup;
        }
        else if (panelGroupKey == panelState.TopRightPanelGroup.Key)
        {
            panelGroup = panelState.TopRightPanelGroup;
        }
        else if (panelGroupKey == panelState.BottomPanelGroup.Key)
        {
            panelGroup = panelState.BottomPanelGroup;
        }
        else
        {
            return Task.CompletedTask;
        }

        if (panelGroup is null)
            return Task.CompletedTask;

        var dragEventArgs_PanelTab = panelState.DragEventArgs_PanelTab;
        var dragEventArgs_PanelGroup = panelState.DragEventArgs_PanelGroup;

        if (dragEventArgs_PanelTab is not null && dragEventArgs_PanelGroup is not null)
        {
            DotNetService.CommonService.DisposePanelTab(
                dragEventArgs_PanelGroup.Key,
                dragEventArgs_PanelTab.Key);

            DotNetService.CommonService.RegisterPanelTab(
                panelGroup.Key,
                dragEventArgs_PanelTab,
                false);

            DotNetService.CommonService.Panel_SetDragEventArgs(
                dragEventArgs_PanelTab: null,
                dragEventArgs_PanelGroup: null);

            DotNetService.CommonService.Drag_ShouldDisplayAndMouseEventArgsSetAction(false, null);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// This method should only be invoked from the "UI thread" due to the usage of `CommonBackgroundTaskApi.UiStringBuilder`.
    /// </summary>
    private string GetPanelElementCssClass(string panelPositionCss, string cssClassString)
    {
        DotNetService.CommonService.UiStringBuilder.Clear();
        DotNetService.CommonService.UiStringBuilder.Append("ci_ide_panel ");
        DotNetService.CommonService.UiStringBuilder.Append(panelPositionCss);
        DotNetService.CommonService.UiStringBuilder.Append(" ");
        DotNetService.CommonService.UiStringBuilder.Append(cssClassString);

        return DotNetService.CommonService.UiStringBuilder.ToString();
    }

    private void HandleOnMouseUp()
    {
        _tabCascadingValueBatch.ThinksLeftMouseButtonIsDown = false;
    }

    public void SubscribeToDragEventForScrolling(IDrag draggable)
    {
        DotNetService.CommonService.Drag_ShouldDisplayAndMouseEventArgsAndDragSetAction(true, null, draggable);
    }

    /// <summary>
    /// This method can only be invoked from the "UI thread" due to the shared `UiStringBuilder` usage.
    /// </summary>
    private string GetCssClass(ITab localTabViewModel)
    {
        var uiStringBuilder = DotNetService.CommonService.UiStringBuilder;

        uiStringBuilder.Clear();
        uiStringBuilder.Append("ci_dynamic-tab ci_button ci_unselectable ");
        uiStringBuilder.Append(GetIsActiveCssClass(localTabViewModel));
        uiStringBuilder.Append(" ");
        uiStringBuilder.Append(localTabViewModel.TabGroup?.GetDynamicCss(localTabViewModel));
        uiStringBuilder.Append(" ");
        uiStringBuilder.Append("ci_ide_panel-tab");

        return uiStringBuilder.ToString();
    }
    
    private Task RenderFileDropdownOnClick()
    {
        return CommonFacts.RenderDropdownAsync(
            DotNetService.CommonService,
            DotNetService.CommonService.JsRuntimeCommonApi,
            ButtonFileId,
            DropdownOrientation.Bottom,
            DropdownKeyFile,
            InitializeMenuFile(DotNetService),
            ButtonFileId,
            preventScroll: false);
    }
    
    private Task RenderToolsDropdownOnClick()
    {
        return CommonFacts.RenderDropdownAsync(
            DotNetService.CommonService,
            DotNetService.CommonService.JsRuntimeCommonApi,
            ButtonToolsId,
            DropdownOrientation.Bottom,
            DropdownKeyTools,
            InitializeMenuTools(DotNetService),
            ButtonToolsId,
            preventScroll: false);
    }
    
    private Task RenderViewDropdownOnClick()
    {
        return CommonFacts.RenderDropdownAsync(
            DotNetService.CommonService,
            DotNetService.CommonService.JsRuntimeCommonApi,
            ButtonViewId,
            DropdownOrientation.Bottom,
            DropdownKeyView,
            InitializeMenuView(DotNetService),
            ButtonViewId,
            preventScroll: false);
    }
    
    private Task RenderRunDropdownOnClick()
    {
        return CommonFacts.RenderDropdownAsync(
            DotNetService.CommonService,
            DotNetService.CommonService.JsRuntimeCommonApi,
            ButtonRunId,
            DropdownOrientation.Bottom,
            DropdownKeyRun,
            InitializeMenuRun(DotNetService),
            ButtonRunId,
            preventScroll: false);
    }
    
    private Task OpenInfoDialogOnClick()
    {
        var dialogRecord = new DialogViewModel(
            InitializationHelper._infoDialogKey,
            "Info",
            typeof(IdeInfoDisplay),
            null,
            null,
            true,
            null);
    
        DotNetService.CommonService.Dialog_ReduceRegisterAction(dialogRecord);
        return Task.CompletedTask;
    }
    
    public void SubscribeToDragEvent(MainLayoutDragEventKind mainLayoutDragEventKind)
    {
        DotNetService.CommonService.MainLayoutDragEventKind = mainLayoutDragEventKind;
        _dragEventHandler = DotNetService.CommonService.DragEventHandlerResizeHandleAsync;
        
        DotNetService.CommonService.Drag_ShouldDisplayAndMouseEventArgsSetAction(true, null);
    }
    
    public void SubscribeToDragEventTopLeft()
    {
        SubscribeToDragEvent(MainLayoutDragEventKind.TopLeftResizeColumn);
    }
    
    public void SubscribeToDragEventTopRight()
    {
        SubscribeToDragEvent(MainLayoutDragEventKind.TopRightResizeColumn);
    }
    
    public void SubscribeToDragEventBottom()
    {
        SubscribeToDragEvent(MainLayoutDragEventKind.BottomResizeRow);
    }
    
    /* Start StartupControlDisplay.razor */
    private async Task StartProgramWithoutDebuggingOnClick()
    {
        var localStartupControlState = DotNetService.IdeService.GetIdeStartupControlState();
        var activeStartupControl = default(StartupControlModel);
        foreach (var x in localStartupControlState.StartupControlList)
        {
            if (x.StartupProjectAbsolutePath.Value == localStartupControlState.ActiveStartupProjectAbsolutePathValue)
            {
                activeStartupControl = x;
                break;
            }
        }
        if (activeStartupControl.StartupProjectAbsolutePath.Value is null)
            return;
    
        if (_userInterfaceSawIsExecuting)
        {
            var menuOptionList = new List<MenuOptionValue>();
            
            menuOptionList.Add(new MenuOptionValue(
                "View Output",
                MenuOptionKind.Other,
                onClickFunc: _ => 
                {
                    var panelState = DotNetService.CommonService.GetPanelState();
                    
                    var outputPanel = (Panel?)null;
                    foreach (var x in panelState.PanelList)
                    {
                        if (x.Title == "Output")
                        {
                            outputPanel = x;
                            break;
                        }
                    }
                    if (outputPanel is not null)
                        DotNetService.CommonService.ShowOrAddPanelTab(outputPanel);
                    
                    return Task.CompletedTask;
                }));
                
            menuOptionList.Add(new MenuOptionValue(
                "View Terminal",
                MenuOptionKind.Other,
                onClickFunc: _ => 
                {
                    DotNetService.IdeService.TerminalGroup_SetActiveTerminal(IdeFacts.EXECUTION_KEY);
                    var panelState = DotNetService.CommonService.GetPanelState();
                    var outputPanel = (Panel?)null;
                    foreach (var x in panelState.PanelList)
                    {
                        if (x.Title == "Terminal")
                        {
                            outputPanel = x;
                            break;
                        }
                    }
                    if (outputPanel is not null)
                        DotNetService.CommonService.ShowOrAddPanelTab(outputPanel);
                    
                    return Task.CompletedTask;
                }));
                
            menuOptionList.Add(new MenuOptionValue(
                "Stop Execution",
                MenuOptionKind.Other,
                onClickFunc: _ =>
                {
                    var localStartupControlState = DotNetService.IdeService.GetIdeStartupControlState();
                    var activeStartupControl = default(StartupControlModel);
                    foreach (var x in localStartupControlState.StartupControlList)
                    {
                        if (x.StartupProjectAbsolutePath.Value == localStartupControlState.ActiveStartupProjectAbsolutePathValue)
                        {
                            activeStartupControl = x;
                            break;
                        }
                    }
                    if (activeStartupControl.StartupProjectAbsolutePath.Value is null)
                        return Task.CompletedTask;
                        
                    return DotNetService.StopButtonOnClick(activeStartupControl);
                }));
                
            await CommonFacts.RenderDropdownAsync(
                DotNetService.CommonService,
                DotNetService.CommonService.JsRuntimeCommonApi,
                _startButtonElementId,
                DropdownOrientation.Bottom,
                _startButtonDropdownKey,
                new MenuContainer(menuOptionList),
                _startButtonElementId,
                preventScroll: false);
        }
        else
        {
            await DotNetService.StartButtonOnClick(activeStartupControl)
                .ConfigureAwait(false);
        }
    }
    
    private async void OnIdeStateChanged(IdeStateChangedKind ideStateChangedKind)
    {
        if (ideStateChangedKind == IdeStateChangedKind.TerminalHasExecutingProcessStateChanged ||
            ideStateChangedKind == IdeStateChangedKind.Ide_StartupControlStateChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    /* End StartupControlDisplay.razor */
    
    private void DispatchRegisterDialogRecordAction()
    {
        InitializationHelper.DispatchRegisterDialogRecordAction(
            DotNetService,
            new DialogViewModel(
                _settingsDialogKey,
                "Settings",
                typeof(SettingsDisplay),
                null,
                null,
                true,
                null));
    }
    
    public static MenuContainer InitializeMenuRun(DotNetService DotNetService)
    {
        var menuOptionsList = new List<MenuOptionValue>();

        // Menu Option Build Project (startup project)
        menuOptionsList.Add(new MenuOptionValue(
            "Build Project (startup project)",
            MenuOptionKind.Create,
            _ =>
            {
                var startupControlState = DotNetService.IdeService.GetIdeStartupControlState();
                var activeStartupControl = default(StartupControlModel);
                foreach (var x in startupControlState.StartupControlList)
                {
                    if (x.StartupProjectAbsolutePath.Value == startupControlState.ActiveStartupProjectAbsolutePathValue)
                    {
                        activeStartupControl = x;
                        break;
                    }
                }
                if (activeStartupControl.StartupProjectAbsolutePath.Value is not null)
                    InitializationHelper.BuildProjectOnClick(DotNetService, activeStartupControl.StartupProjectAbsolutePath.Value);
                else
                    CommonFacts.DispatchError(nameof(InitializationHelper.BuildProjectOnClick), "activeStartupControl?.StartupProjectAbsolutePath was null", DotNetService.CommonService, TimeSpan.FromSeconds(6));
                return Task.CompletedTask;
            }));

        // Menu Option Clean (startup project)
        menuOptionsList.Add(new MenuOptionValue(
            "Clean Project (startup project)",
            MenuOptionKind.Create,
            _ =>
            {
                var startupControlState = DotNetService.IdeService.GetIdeStartupControlState();
                var activeStartupControl = default(StartupControlModel);
                foreach (var x in startupControlState.StartupControlList)
                {
                    if (x.StartupProjectAbsolutePath.Value == startupControlState.ActiveStartupProjectAbsolutePathValue)
                    {
                        activeStartupControl = x;
                        break;
                    }
                }
                if (activeStartupControl.StartupProjectAbsolutePath.Value is not null)
                    InitializationHelper.CleanProjectOnClick(DotNetService, activeStartupControl.StartupProjectAbsolutePath.Value);
                else
                    CommonFacts.DispatchError(nameof(InitializationHelper.CleanProjectOnClick), "activeStartupControl?.StartupProjectAbsolutePath was null", DotNetService.CommonService, TimeSpan.FromSeconds(6));
                return Task.CompletedTask;
            }));

        // Menu Option Build Solution
        menuOptionsList.Add(new MenuOptionValue(
            "Build Solution",
            MenuOptionKind.Delete,
            _ =>
            {
                var dotNetSolutionState = DotNetService.GetDotNetSolutionState();
                var dotNetSolutionModel = dotNetSolutionState.DotNetSolutionModel;

                if (dotNetSolutionModel?.AbsolutePath is not null)
                    InitializationHelper.BuildSolutionOnClick(DotNetService, dotNetSolutionModel.AbsolutePath.Value);
                else
                    CommonFacts.DispatchError(nameof(InitializationHelper.BuildSolutionOnClick), "dotNetSolutionModel?.AbsolutePath was null", DotNetService.CommonService, TimeSpan.FromSeconds(6));
                return Task.CompletedTask;
            }));

        // Menu Option Clean Solution
        menuOptionsList.Add(new MenuOptionValue(
            "Clean Solution",
            MenuOptionKind.Delete,
            _ =>
            {
                var dotNetSolutionState = DotNetService.GetDotNetSolutionState();
                var dotNetSolutionModel = dotNetSolutionState.DotNetSolutionModel;

                if (dotNetSolutionModel?.AbsolutePath is not null)
                    InitializationHelper.CleanSolutionOnClick(DotNetService, dotNetSolutionModel.AbsolutePath.Value);
                else
                    CommonFacts.DispatchError(nameof(InitializationHelper.CleanSolutionOnClick), "dotNetSolutionModel?.AbsolutePath was null", DotNetService.CommonService, TimeSpan.FromSeconds(6));
                return Task.CompletedTask;
            }));

        return new MenuContainer(menuOptionsList);
    }
    
    public static MenuContainer InitializeMenuView(DotNetService DotNetService)
    {
        var panelState = DotNetService.CommonService.GetPanelState();
        var dialogState = DotNetService.CommonService.GetDialogState();
        var menuOptionsList = new List<MenuOptionValue>(capacity: panelState.PanelList.Count);

        foreach (var panel in panelState.PanelList)
        {
            menuOptionsList.Add(new MenuOptionValue(
                panel.Title,
                MenuOptionKind.Delete,
                _ => DotNetService.CommonService.ShowOrAddPanelTab(panel)));
        }
    
        if (menuOptionsList.Count == 0)
        {
            return new MenuContainer();
        }
        else
        {
            return new MenuContainer(menuOptionsList);
        }
    }
    
    public static MenuContainer InitializeMenuFile(DotNetService DotNetService)
    {
        var menuOptionsList = new List<MenuOptionValue>();

        // Menu Option New
        var menuOptionNewDotNetSolution = new MenuOptionValue(
            ".NET Solution",
            MenuOptionKind.Other,
            _ => DotNetService.OpenNewDotNetSolutionDialog());

        var menuOptionNew = new MenuOptionValue(
            "New",
            MenuOptionKind.Other,
            menuOptionOnClickArgs =>
            {
                MenuContainer.OpenSubMenu(
                    DotNetService.CommonService,
                    subMenu: new MenuContainer(new List<MenuOptionValue> { menuOptionNewDotNetSolution }),
                    menuOptionOnClickArgs.MenuMeasurements,
                    menuOptionOnClickArgs.TopOffsetOptionFromMenu,
                    elementIdToRestoreFocusToOnClose: menuOptionOnClickArgs.MenuHtmlId);
                    
                return Task.CompletedTask;
            }/*,
            */)
        {
            IconKind = AutocompleteEntryKind.Chevron
        };

        menuOptionsList.Add(menuOptionNew);
        
        // Menu Option Open
        var menuOptionOpenDotNetSolution = new MenuOptionValue(
            ".NET Solution",
            MenuOptionKind.Other,
            _ =>
            {
                DotNetSolutionState.ShowInputFile(DotNetService.IdeService, DotNetService);
                return Task.CompletedTask;
            });
        
        var menuOptionOpenFile = new MenuOptionValue(
            "File",
            MenuOptionKind.Other,
            _ =>
            {
                DotNetService.IdeService.Editor_ShowInputFile();
                return Task.CompletedTask;
            });

        var menuOptionOpenDirectory = new MenuOptionValue(
            "Directory",
            MenuOptionKind.Other,
            _ =>
            {
                DotNetService.IdeService.FolderExplorer_ShowInputFile();
                return Task.CompletedTask;
            });

        var menuOptionOpen = new MenuOptionValue(
            "Open",
            MenuOptionKind.Other,
            menuOptionOnClickArgs =>
            {
                MenuContainer.OpenSubMenu(
                    DotNetService.CommonService,
                    subMenu: new MenuContainer(new List<MenuOptionValue>()
                    {
                        menuOptionOpenDotNetSolution,
                        menuOptionOpenFile,
                        menuOptionOpenDirectory,
                    }),
                    menuOptionOnClickArgs.MenuMeasurements,
                    menuOptionOnClickArgs.TopOffsetOptionFromMenu,
                    elementIdToRestoreFocusToOnClose: menuOptionOnClickArgs.MenuHtmlId);
                    
                return Task.CompletedTask;
            }/*,
            )*/)
        {
            IconKind = AutocompleteEntryKind.Chevron
        };

        menuOptionsList.Add(menuOptionOpen);

        var menuOptionSave = new MenuOptionValue(
            "Save (Ctrl s)",
            MenuOptionKind.Other,
            _ =>
            {
                TextEditorCommandDefaultFunctions.TriggerSave_NoTextEditorFocus(DotNetService.TextEditorService, Clair.TextEditor.RazorLib.TextEditorService.EditorTextEditorGroupKey);
                return Task.CompletedTask;
            });
        menuOptionsList.Add(menuOptionSave);

        var menuOptionSaveAll = new MenuOptionValue(
            "Save All (Ctrl Shift s)",
            MenuOptionKind.Other,
            _ =>
            {
                TextEditorCommandDefaultFunctions.TriggerSaveAll(DotNetService.TextEditorService, Clair.TextEditor.RazorLib.TextEditorService.EditorTextEditorGroupKey);
                return Task.CompletedTask;
            });
        menuOptionsList.Add(menuOptionSaveAll);

        // Menu Option Permissions
        var menuOptionPermissions = new MenuOptionValue(
            "Permissions",
            MenuOptionKind.Delete,
            _ => InitializationHelper.ShowPermissionsDialog(DotNetService));

        menuOptionsList.Add(menuOptionPermissions);

        return new MenuContainer(menuOptionsList);
    }

    public static MenuContainer InitializeMenuTools(DotNetService DotNetService)
    {
        var menuOptionsList = new List<MenuOptionValue>();

        // Menu Option Find All
        var menuOptionFindAll = new MenuOptionValue(
            "Find All (Ctrl Shift f)",
            MenuOptionKind.Delete,
            _ =>
            {
                DotNetService.TextEditorService.Options_ShowFindAllDialog();
                return Task.CompletedTask;
            });

        menuOptionsList.Add(menuOptionFindAll);

        // Menu Option Code Search
        var menuOptionCodeSearch = new MenuOptionValue(
            "Code Search (Ctrl ,)",
            MenuOptionKind.Delete,
            _ =>
            {
                DotNetService.IdeService.CodeSearchDialog ??= new DialogViewModel(
                    Key<IDynamicViewModel>.NewKey(),
                    "Code Search",
                    typeof(CodeSearchDisplay),
                    null,
                    null,
                    true,
                    null);

                DotNetService.CommonService.Dialog_ReduceRegisterAction(DotNetService.IdeService.CodeSearchDialog);
                return Task.CompletedTask;
            });

        menuOptionsList.Add(menuOptionCodeSearch);

        return new MenuContainer(menuOptionsList);
    }
    
    public void Dispose()
    {
        
        DotNetService.CommonService.CommonUiStateChanged -= OnCommonUiStateChanged;
        DotNetService.TextEditorService.SecondaryChanged -= TextEditorOptionsStateWrap_StateChanged;
        DotNetService.IdeService.IdeStateChanged -= OnIdeStateChanged;
        
        BrowserResizeInterop.DisposeWindowSizeChanged(DotNetService.CommonService.JsRuntimeCommonApi);
        
        _workerCancellationTokenSource.Cancel();
        _workerCancellationTokenSource.Dispose();
        
        DotNetService.CommonService.Continuous_StartAsyncTask = null;
        DotNetService.CommonService.Indefinite_StartAsyncTask = null;
    }
}
