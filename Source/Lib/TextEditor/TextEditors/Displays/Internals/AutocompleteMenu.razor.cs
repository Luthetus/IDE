using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Menus.Displays;
using Clair.Common.RazorLib.Menus.Models;
using Clair.TextEditor.RazorLib.Autocompletes.Models;
using Clair.TextEditor.RazorLib.Commands.Models.Defaults;
using Clair.TextEditor.RazorLib.Exceptions;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;

public sealed partial class AutocompleteMenu : ComponentBase, ITextEditorDependentComponent
{
    [Inject]
    private TextEditorService TextEditorService { get; set; } = null!;

    [Parameter, EditorRequired]
    public Key<TextEditorComponentData> ComponentDataKey { get; set; }
    
    public const string HTML_ELEMENT_ID = "ci_te_autocomplete-menu-id";
    
    private AutocompleteContainer _autocompleteContainer = new();
    
    private Key<TextEditorComponentData> _componentDataKeyPrevious = Key<TextEditorComponentData>.Empty;
    private TextEditorComponentData? _componentData;

    protected override void OnInitialized()
    {
        _activeIndex = 0;
        _dotNetHelper = DotNetObjectReference.Create(this);
        _htmlId = $"luth_common_treeview-{_guidId}";

        TextEditorService.TextEditorStateChanged += OnTextEditorStateChanged;
        OnTextEditorStateChanged();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Do not ConfigureAwait(false) so that the UI doesn't change out from under you
            // before you finish setting up the events?
            // (is this a thing, I'm just presuming this would be true).
            _menuMeasurements = await TextEditorService.CommonService.JsRuntimeCommonApi.JsRuntime.InvokeAsync<MenuMeasurements>(
                "clairCommon.menuInitialize",
                _dotNetHelper,
                _htmlId);

            /*if (Menu.ShouldImmediatelyTakeFocus)
            {
                await TextEditorService.CommonService.JsRuntimeCommonApi.JsRuntime.InvokeVoidAsync(
                    "clairCommon.focusHtmlElementById",
                    _htmlId,
                    /*preventScroll:*//* true);
            }*/
        }
        var componentData = GetComponentData();
        if (componentData?.MenuShouldTakeFocus ?? false)
        {
            componentData.MenuShouldTakeFocus = false;
            //await _menuDisplay.SetFocusAndSetFirstOptionActiveAsync();
        }
    }
    
    private TextEditorVirtualizationResult GetVirtualizationResult()
    {
        return GetComponentData()?.Virtualization ?? TextEditorVirtualizationResult.Empty;
    }
    
    private TextEditorComponentData? GetComponentData()
    {
        if (_componentDataKeyPrevious != ComponentDataKey)
        {
            if (!TextEditorService.TextEditorState._componentDataMap.TryGetValue(ComponentDataKey, out var componentData) ||
                componentData is null)
            {
                _componentData = null;
            }
            else
            {
                _componentData = componentData;
                _componentDataKeyPrevious = ComponentDataKey;
            }
        }
        
        return _componentData;
    }
    
    private async void OnTextEditorStateChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    private AutocompleteContainer GetMenuRecord()
    {
        var virtualizationResult = GetVirtualizationResult();
        if (!virtualizationResult.IsValid)
            return null;
        return virtualizationResult.Model.PersistentState.CompilerService.GetAutocompleteMenu(virtualizationResult, this);
    }

    public async Task SelectMenuOption(Func<Task> menuOptionAction)
    {
        var virtualizationResult = GetVirtualizationResult();
        if (!virtualizationResult.IsValid)
            return;
    
        TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            var viewModelModifier = editContext.GetViewModelModifier(virtualizationResult.ViewModel.PersistentState.ViewModelKey);

            if (viewModelModifier.PersistentState.MenuKind != MenuKind.None)
            {
                TextEditorCommandDefaultFunctions.RemoveDropdown(
                    editContext,
                    viewModelModifier,
                    TextEditorService.CommonService);
            }

            await menuOptionAction.Invoke().ConfigureAwait(false);
            await virtualizationResult.ViewModel.FocusAsync();
        });
    }

    public async Task InsertAutocompleteMenuOption(
        string word,
        AutocompleteValue autocompleteEntry,
        TextEditorViewModel viewModel)
    {
        var virtualizationResult = GetVirtualizationResult();
        if (!virtualizationResult.IsValid)
            return;
    
        TextEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            var modelModifier = editContext.GetModelModifier(viewModel.PersistentState.ResourceUri);
            var viewModelModifier = editContext.GetViewModelModifier(viewModel.PersistentState.ViewModelKey);
            if (modelModifier is null || viewModelModifier is null)
                return ValueTask.CompletedTask;
        
            modelModifier.Insert(autocompleteEntry.DisplayName.Substring(word.Length), viewModel);
                
            return virtualizationResult.ViewModel.FocusAsync();
        });
        
        await virtualizationResult.ViewModel.FocusAsync();
    }

    /// <summary>Pixels</summary>
    private int LineHeight => TextEditorService.CommonService.Options_LineHeight;

    private Guid _guidId = Guid.NewGuid();
    private string _htmlId = null!;

    /// <summary>
    /// Start at -1 so when menu opens user can choose to start at index '0' or 'count - 1' with ArrowDown or ArrowUp.
    /// If MenuRecord.InitialActiveMenuOptionRecordIndex is not -1, then use the provided index as this initial value.
    /// </summary>
    private int _activeIndex = -1;

    private readonly HashSet<int> _horizontalRuleElementIndexHashSet = new();

    private MenuMeasurements _menuMeasurements;
    private DotNetObjectReference<AutocompleteMenu>? _dotNetHelper;

    /// <summary>In pixels (px)</summary>
    private int _horizontalRuleTotalVerticalMargin = 10;
    /// <summary>In pixels (px)</summary>
    private double _horizontalRuleHeight = 1.5;
    private double HorizontalRuleVerticalOffset => _horizontalRuleTotalVerticalMargin + _horizontalRuleHeight;

    private int _seenWidgetHeight = -1;

    private int _indexMenuOptionShouldDisplayWidget = -1;
    private int WidgetHeight => 4 * LineHeight;

    public string HtmlId => _htmlId;

    public async Task SetFocusAndSetFirstOptionActiveAsync()
    {
        _activeIndex = 0;
        await TextEditorService.CommonService.JsRuntimeCommonApi.JsRuntime.InvokeVoidAsync(
            "clairCommon.focusHtmlElementById",
            _htmlId,
            /*preventScroll:*/ true);
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task ReceiveOnKeyDown(MenuEventArgsKeyDown eventArgsKeyDown)
    {
        _menuMeasurements = new MenuMeasurements(
            eventArgsKeyDown.ViewWidth,
            eventArgsKeyDown.ViewHeight,
            eventArgsKeyDown.BoundingClientRectLeft,
            eventArgsKeyDown.BoundingClientRectTop);

        switch (eventArgsKeyDown.Key)
        {
            case "ArrowDown":
                if (_activeIndex >= _autocompleteContainer.AutocompleteMenuList.Length - 1)
                {
                    _activeIndex = 0;
                }
                else
                {
                    _activeIndex++;
                }
                break;
            case "ArrowUp":
                if (_activeIndex <= 0)
                {
                    _activeIndex = _autocompleteContainer.AutocompleteMenuList.Length - 1;
                }
                else
                {
                    _activeIndex--;
                }
                break;
            case "ArrowRight":
                //OpenSubmenu();
                break;
            case "ArrowLeft":
                await Close();
                break;
            case "Home":
                _activeIndex = 0;
                break;
            case "End":
                _activeIndex = _autocompleteContainer.AutocompleteMenuList.Length - 1;
                break;
            case "Escape":
                await Close();
                break;
            case "Enter":
            case " ":
                var option = _autocompleteContainer.AutocompleteMenuList[_activeIndex];
                /*if (option.OnClickFunc is not null)
                {
                    await option.OnClickFunc.Invoke(new MenuOptionOnClickArgs
                    {
                        MenuMeasurements = _menuMeasurements,
                        TopOffsetOptionFromMenu = GetTopByIndex(_activeIndex),
                        MenuHtmlId = _htmlId,
                    });

                    if (option.IconKind != AutocompleteEntryKind.Chevron && option.IconKind != AutocompleteEntryKind.Widget)
                        await Close();
                }*/
                break;
        }

        StateHasChanged();
    }

    [JSInvokable]
    public void ReceiveOnContextMenu(MenuEventArgsMouseDown eventArgsMouseDown)
    {
        _menuMeasurements = new MenuMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop);

        StateHasChanged();
    }

    [JSInvokable]
    public void ReceiveContentOnMouseDown(MenuEventArgsMouseDown eventArgsMouseDown)
    {
        _menuMeasurements = new MenuMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop);

        var indexClicked = GetIndexClicked(eventArgsMouseDown);
        if (indexClicked == -1)
            return;

        StateHasChanged();
    }

    [JSInvokable]
    public async Task ReceiveOnClick(MenuEventArgsMouseDown eventArgsMouseDown)
    {
        _menuMeasurements = new MenuMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop);

        var indexClicked = GetIndexClicked(eventArgsMouseDown);
        if (indexClicked == -1)
            return;

        _activeIndex = indexClicked;
        var option = _autocompleteContainer.AutocompleteMenuList[indexClicked];
        _autocompleteContainer.OnClick(option);
    }

    [JSInvokable]
    public async Task ReceiveOnDoubleClick(MenuEventArgsMouseDown eventArgsMouseDown)
    {
        _menuMeasurements = new MenuMeasurements(
            eventArgsMouseDown.ViewWidth,
            eventArgsMouseDown.ViewHeight,
            eventArgsMouseDown.BoundingClientRectLeft,
            eventArgsMouseDown.BoundingClientRectTop);

        var indexClicked = GetIndexClicked(eventArgsMouseDown);
        if (indexClicked == -1)
            return;

        StateHasChanged();
    }

    /// <summary>
    /// TODO: This seems to be slightly inaccurate...
    /// ...I'm going to try checking if difference is less than 1.1px then return -1, don't do anything.
    /// The -1 trick seems to result in accuracy but having that tiny deadzone is probably going to be annoying.
    ///
    /// Must be on the UI thread so the method safely can read '_horizontalRuleElementIndexHashSet'.
    /// </summary>
    private int GetIndexClicked(MenuEventArgsMouseDown eventArgsMouseDown)
    {
        var relativeY = eventArgsMouseDown.Y - _menuMeasurements.BoundingClientRectTop + eventArgsMouseDown.ScrollTop;
        relativeY = Math.Max(0, relativeY);

        double buildHeight = 0.0;

        int optionIndex = 0;

        for (; optionIndex < _autocompleteContainer.AutocompleteMenuList.Length; optionIndex++)
        {
            if (_horizontalRuleElementIndexHashSet.Contains(optionIndex))
                buildHeight += HorizontalRuleVerticalOffset;
            if (_indexMenuOptionShouldDisplayWidget == optionIndex)
                buildHeight += WidgetHeight;

            buildHeight += LineHeight;

            if (buildHeight > relativeY)
                break;
        }

        if (Math.Abs(buildHeight - relativeY) < 1.1)
            return -1;

        return IndexBasicValidation(optionIndex);
    }

    /// <summary>
    /// TODO: Don't replicate this method, it is essentially the inverse of 'GetIndexClicked(...)'
    ///
    /// Must be on the UI thread so the method safely can read '_horizontalRuleElementIndexHashSet'.
    /// </summary>
    private double GetTopByIndex(int index)
    {
        double buildHeight = 0.0;

        int optionIndex = 0;

        for (; optionIndex < index; optionIndex++)
        {
            if (_horizontalRuleElementIndexHashSet.Contains(optionIndex))
                buildHeight += HorizontalRuleVerticalOffset;
            if (_indexMenuOptionShouldDisplayWidget == optionIndex)
                buildHeight += WidgetHeight;

            buildHeight += LineHeight;
        }

        return buildHeight;
    }

    private int IndexBasicValidation(int indexLocal)
    {
        if (indexLocal < 0)
            return 0;
        else if (indexLocal >= _autocompleteContainer.AutocompleteMenuList.Length)
            return _autocompleteContainer.AutocompleteMenuList.Length - 1;

        return indexLocal;
    }

    private async Task Close()
    {
        TextEditorService.CommonService.Dropdown_ReduceClearAction();
        var virtualizationResult = GetVirtualizationResult();
        if (virtualizationResult.IsValid)
        {
            await virtualizationResult.ViewModel!.FocusAsync();
        }    
    }

    public AutocompleteContainer GetAutocompleteOptions()
    {
        var virtualizationResult = GetVirtualizationResult();
        if (virtualizationResult.IsValid)
        {
            return virtualizationResult.Model!.PersistentState.CompilerService.GetAutocompleteMenu(virtualizationResult, this);
        }
        
        return new();
    }

    public void Dispose()
    {
        TextEditorService.TextEditorStateChanged -= OnTextEditorStateChanged;

        _dotNetHelper?.Dispose();

        var virtualizationResult = GetVirtualizationResult();
        if (!virtualizationResult.IsValid)
            return;
        
        TextEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            var viewModelModifier = editContext.GetViewModelModifier(virtualizationResult.ViewModel.PersistentState.ViewModelKey);
            viewModelModifier.PersistentState.MenuKind = MenuKind.None;
            return ValueTask.CompletedTask;
        });
    }
}
