using System.Text.Json;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.Common.RazorLib.Dialogs.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Themes.Models;
using Clair.TextEditor.RazorLib.JavaScriptObjects.Models;
using Clair.TextEditor.RazorLib.Options.Models;

namespace Clair.TextEditor.RazorLib;

public partial class TextEditorService
{
    public const int Options_TAB_WIDTH_MIN = 2;
    public const int Options_TAB_WIDTH_MAX = 4;
    public const int MINIMUM_CURSOR_SIZE_IN_PIXELS = 1;

    private TextEditorOptionsState Options_textEditorOptionsState = new();

    private IDialog? Options_findAllDialog;

    public TextEditorOptionsState Options_GetTextEditorOptionsState() => Options_textEditorOptionsState;

    public TextEditorOptions Options_GetOptions()
    {
        return Options_GetTextEditorOptionsState().Options;
    }

    public void Options_InvokeTextEditorWrapperCssStateChanged()
    {
        SecondaryChanged?.Invoke(SecondaryChangedKind.TextEditorWrapperCssStateChanged);
    }

    public void Options_ShowFindAllDialog(bool? isResizableOverride = null, string? cssClassString = null)
    {
        // TODO: determine the actively focused element at time of invocation,
        //       then restore focus to that element when this dialog is closed.
        Options_findAllDialog ??= new DialogViewModel(
            Key<IDynamicViewModel>.NewKey(),
            "Find All",
            typeof(Clair.TextEditor.RazorLib.FindAlls.Displays.FindAllDisplay),
            null,
            cssClassString,
            isResizableOverride ?? true,
            null);

        CommonService.Dialog_ReduceRegisterAction(Options_findAllDialog);
    }

    public void Options_SetTheme(ThemeRecord theme, bool updateStorage = true)
    {
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CommonOptions = inState.Options.CommonOptions with
                {
                    ThemeKey = theme.Key
                },
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };

        HandleThemeChange();

        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    public void HandleThemeChange()
    {
        var usingThemeCss = CommonService.GetThemeState().ThemeList
            .FirstOrDefault(x => x.Key == Options_GetTextEditorOptionsState().Options.CommonOptions.ThemeKey);
        var usingThemeCssClassString = usingThemeCss == default
            ? CommonFacts.VisualStudioDarkThemeClone.CssClassString
            : usingThemeCss.CssClassString;
        ThemeCssClassString = usingThemeCssClassString;
    }

    public void Options_SetShowWhitespace(bool showWhitespace, bool updateStorage = true)
    {
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                ShowWhitespace = showWhitespace,
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        // ShowWhitespace needs virtualization result to be re-calculated.
        SecondaryChanged?.Invoke(SecondaryChangedKind.MeasuredStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public void Options_SetShowNewlines(bool showNewlines, bool updateStorage = true)
    {
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                ShowNewlines = showNewlines,
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public void Options_SetTabKeyBehavior(bool tabKeyBehavior, bool updateStorage = true)
    {
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                TabKeyBehavior = tabKeyBehavior,
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public int ValidateTabWidth(int tabWidth)
    {
        if (tabWidth < Options_TAB_WIDTH_MIN)
            return Options_TAB_WIDTH_MIN;
        else if (tabWidth > Options_TAB_WIDTH_MAX)
            return Options_TAB_WIDTH_MAX;
        else
            return tabWidth;
    }

    public void Options_SetTabWidth(int tabWidth, bool updateStorage = true)
    {
        tabWidth = ValidateTabWidth(tabWidth);

        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                TabWidth = tabWidth,
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.MeasuredStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public int ValidateFontSize(int fontSizeInPixels)
    {
        if (fontSizeInPixels < TextEditorOptionsState.MINIMUM_FONT_SIZE_IN_PIXELS)
            return TextEditorOptionsState.MINIMUM_FONT_SIZE_IN_PIXELS;
        
        return fontSizeInPixels;
    }

    public void Options_SetFontSize(int fontSizeInPixels, bool updateStorage = true)
    {
        fontSizeInPixels = ValidateFontSize(fontSizeInPixels);
    
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CommonOptions = inState.Options.CommonOptions with
                {
                    FontSizeInPixels = fontSizeInPixels
                },
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.NeedsMeasured);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public string ValidateFontFamily(string? fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
            return null;
        else
            return fontFamily.Trim();
    }

    public void Options_SetFontFamily(string? fontFamily, bool updateStorage = true)
    {
        fontFamily = ValidateFontFamily(fontFamily);
    
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CommonOptions = inState.Options.CommonOptions with
                {
                    FontFamily = fontFamily
                },
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.NeedsMeasured);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public double ValidateCursorWidth(double cursorWidthInPixels)
    {
        if (cursorWidthInPixels < MINIMUM_CURSOR_SIZE_IN_PIXELS)
            return MINIMUM_CURSOR_SIZE_IN_PIXELS;
    
        return cursorWidthInPixels;
    }

    public void Options_SetCursorWidth(double cursorWidthInPixels, bool updateStorage = true)
    {
        cursorWidthInPixels = ValidateCursorWidth(cursorWidthInPixels);
    
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CursorWidthInPixels = cursorWidthInPixels,
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public void Options_SetRenderStateKey(Key<TextEditorOptions> renderStateKey)
    {
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                RenderStateKey = renderStateKey
            },
        };
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
    }

    public void Options_SetCharAndLineMeasurements(TextEditorEditContext editContext, CharAndLineMeasurements charAndLineMeasurements)
    {
        var inState = Options_GetTextEditorOptionsState();
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CharAndLineMeasurements = charAndLineMeasurements,
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };

        SecondaryChanged?.Invoke(SecondaryChangedKind.MeasuredStateChanged);
    }

    public void Options_WriteToStorage()
    {
        CommonService.Enqueue(new CommonWorkArgs
        {
            WorkKind = CommonWorkKind.WriteToLocalStorage,
            WriteToLocalStorage_Key = StorageKey,
            WriteToLocalStorage_Value = new TextEditorOptionsJsonDto(Options_GetTextEditorOptionsState().Options),
        });
    }

    public async Task Options_SetFromLocalStorageAsync()
    {
        var optionsJsonString = await JsRuntimeCommonApi.LocalStorageGetItem(StorageKey);

        if (string.IsNullOrWhiteSpace(optionsJsonString))
        {
            HandleDefaultOptionsJsonString();
            return;
        }

        TextEditorOptionsJsonDto? optionsJson = null;
        
        try
        {
            optionsJson = JsonSerializer.Deserialize<TextEditorOptionsJsonDto>(optionsJsonString);
        }
        catch (System.Text.Json.JsonException)
        {
            // TODO: Preserve the values that do parse.
            await CommonService.Storage_SetValue(StorageKey, null).ConfigureAwait(false);
        }
        
        if (optionsJson is null)
        {
            HandleDefaultOptionsJsonString();
            return;
        }
        
        int themeKey;
        int fontSizeInPixels;
        
        if (optionsJson.CommonOptionsJsonDto is null)
        {
            themeKey = CommonFacts.VisualStudioDarkThemeClone.Key;
            fontSizeInPixels = TextEditorOptionsState.MINIMUM_FONT_SIZE_IN_PIXELS;
        }
        else
        {
            themeKey = optionsJson.CommonOptionsJsonDto.ThemeKey;
            fontSizeInPixels = optionsJson.CommonOptionsJsonDto.FontSizeInPixels;
        }
        
        var inState = Options_GetTextEditorOptionsState();
        
        var matchedTheme = CommonService.GetThemeState().ThemeList.FirstOrDefault(x => x.Key == themeKey);
        
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CommonOptions = inState.Options.CommonOptions with
                {
                    ThemeKey = matchedTheme == default ? CommonFacts.VisualStudioDarkThemeClone.Key : matchedTheme.Key,
                    FontSizeInPixels = ValidateFontSize(fontSizeInPixels),
                    
                },
                CursorWidthInPixels = ValidateCursorWidth(optionsJson.CursorWidthInPixels),
                ShowNewlines = optionsJson.ShowNewlines,
                ShowWhitespace = optionsJson.ShowWhitespace,
                TabKeyBehavior = optionsJson.TabKeyBehavior,
                TabWidth = ValidateTabWidth(optionsJson.TabWidth),
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        HandleThemeChange();

        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
        SecondaryChanged?.Invoke(SecondaryChangedKind.NeedsMeasured);
        // ShowWhitespace needs virtualization result to be re-calculated.
        SecondaryChanged?.Invoke(SecondaryChangedKind.MeasuredStateChanged);
    }
    
    private void HandleDefaultOptionsJsonString()
    {
        var inState = Options_GetTextEditorOptionsState();
    
        Options_textEditorOptionsState = new TextEditorOptionsState
        {
            Options = inState.Options with
            {
                CommonOptions = inState.Options.CommonOptions with
                {
                    ThemeKey = CommonFacts.VisualStudioDarkThemeClone.Key,
                    FontSizeInPixels = ValidateFontSize(TextEditorOptionsState.DEFAULT_FONT_SIZE_IN_PIXELS),
                },
                RenderStateKey = Key<TextEditorOptions>.NewKey(),
            },
        };
        HandleThemeChange();
        
        SecondaryChanged?.Invoke(SecondaryChangedKind.StaticStateChanged);
        SecondaryChanged?.Invoke(SecondaryChangedKind.NeedsMeasured);
    }
}
