using System.Text.Json;
using System.Text;
using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Themes.Models;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.Common.RazorLib.Options.Models;

namespace Clair.Common.RazorLib;

public partial class CommonService
{
    private AppOptionsState _appOptionsState = new();

#if DEBUG
    public string Options_StorageKey => "clair-common_theme-storage-key-debug"; 
#else
    public string Options_StorageKey => "clair-common_theme-storage-key";
#endif

    public string Options_ThemeCssClassString { get; set; } = CommonFacts.VisualStudioDarkThemeClone.CssClassString;

    public string? Options_FontFamilyCssStyleString { get; set; }

    public string Options_FontSizeCssStyleString { get; set; }
    
    public string Options_ResizeHandleCssWidth { get; set; } =
        $"width: {AppOptionsState.DEFAULT_RESIZE_HANDLE_WIDTH_IN_PIXELS.ToCssValue()}px";
        
    public string Options_ResizeHandleCssHeight { get; set; } =
        $"height: {AppOptionsState.DEFAULT_RESIZE_HANDLE_HEIGHT_IN_PIXELS.ToCssValue()}px";
    
    public string Options_IconSizeCssValue { get; set; } = AppOptionsState.DEFAULT_ICON_SIZE_IN_PIXELS.ToCssValue();
    
    public string Options_ColorSchemeCssStyleString { get; set; }
    
    public int Options_LineHeight { get; set; } = 20;
    public string Options_LineHeight_CssStyle { get; set; } = "height: 20px;";
    public string Rotate_Options_LineHeight_CssStyle { get; set; } = "width: 20px;";
    
    public string TopPanel_Body_Options_LineHeight_CssStyle { get; set; } = "width: calc(100% - 20px);";
    public string BottomPanel_Body_Options_LineHeight_CssStyle { get; set; } = "height: calc(100% - 20px);";
    
    public AppOptionsState GetAppOptionsState() => _appOptionsState;

    public void Options_SetActiveThemeRecordKey(int themeKey, bool updateStorage = true)
    {
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                ThemeKey = themeKey
            }
        };
        
        HandleThemeChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }

    public void Options_SetTheme(ThemeRecord theme, bool updateStorage = true)
    {
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                ThemeKey = theme.Key
            }
        };
        
        HandleThemeChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    private void HandleThemeChange()
    {
        ThemeRecord usingTheme = default;
        foreach (var theme in GetThemeState().ThemeList)
        {
            if (theme.Key == GetAppOptionsState().Options.ThemeKey)
            {
                usingTheme = theme;
                break;
            }
        }
        if (usingTheme.IsDefault())
            return;
        usingTheme = usingTheme.IsDefault() ? CommonFacts.VisualStudioDarkThemeClone : usingTheme;
        
        Options_ThemeCssClassString = usingTheme.CssClassString;
        
        var cssStyleStringBuilder = new StringBuilder("color-scheme: ");
        if (usingTheme.ThemeColorKind == ThemeColorKind.Dark)
            cssStyleStringBuilder.Append("dark");
        else if (usingTheme.ThemeColorKind == ThemeColorKind.Light)
            cssStyleStringBuilder.Append("light");
        else
            cssStyleStringBuilder.Append("dark");
        cssStyleStringBuilder.Append(';');
        Options_ColorSchemeCssStyleString = cssStyleStringBuilder.ToString();
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
            
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                FontFamily = fontFamily
            }
        };
        
        HandleFontFamilyChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.LineHeightNeedsMeasured);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    public void HandleFontFamilyChange()
    {
        var usingFontFamily = GetAppOptionsState().Options.FontFamily;
        if (usingFontFamily is null)
            Options_FontFamilyCssStyleString = null;
        else
            Options_FontFamilyCssStyleString = $"font-family: {usingFontFamily};";
    }
    
    public int ValidateFontSize(int fontSizeInPixels)
    {
        if (fontSizeInPixels < AppOptionsState.MINIMUM_FONT_SIZE_IN_PIXELS)
            return AppOptionsState.MINIMUM_FONT_SIZE_IN_PIXELS;
        
        return fontSizeInPixels;
    }

    public void Options_SetFontSize(int fontSizeInPixels, bool updateStorage = true)
    {
        fontSizeInPixels = ValidateFontSize(fontSizeInPixels);
    
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                FontSizeInPixels = fontSizeInPixels
            }
        };

        HandleFontSizeChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.LineHeightNeedsMeasured);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    public void HandleFontSizeChange()
    {
        var usingFontSizeInPixels = GetAppOptionsState().Options.FontSizeInPixels;
        var usingFontSizeInPixelsCssValue = usingFontSizeInPixels.ToCssValue();
        Options_FontSizeCssStyleString = $"font-size: {usingFontSizeInPixelsCssValue}px;";
    }
    
    public void Options_SetLineHeight(int lineHeightInPixels)
    {
        Options_LineHeight = lineHeightInPixels;
        
        HandleLineHeightChange();
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
    }
    
    public void HandleLineHeightChange()
    {
        Options_LineHeight_CssStyle = $"height: {Options_LineHeight.ToString()}px;";
        Rotate_Options_LineHeight_CssStyle = $"width: {Options_LineHeight.ToString()}px;";
        
        TopPanel_Body_Options_LineHeight_CssStyle = $"width: calc(100% - {Options_LineHeight.ToString()}px);";
        BottomPanel_Body_Options_LineHeight_CssStyle = $"height: calc(100% - {Options_LineHeight.ToString()}px);";
        
        /*var panelState = GetPanelState();
        
        panelState.TopLeftPanelGroup.ElementDimensions.Width_Base_1 = new DimensionUnit(
            Options_LineHeight,
            DimensionUnitKind.Pixels,
            DimensionOperatorKind.Subtract);
            
        panelState.TopRightPanelGroup.ElementDimensions.Width_Base_1 = new DimensionUnit(
            Options_LineHeight,
            DimensionUnitKind.Pixels,
            DimensionOperatorKind.Subtract);
        
        panelState.BottomPanelGroup.ElementDimensions.Height_Base_1 = new DimensionUnit(
            Options_LineHeight,
            DimensionUnitKind.Pixels,
            DimensionOperatorKind.Subtract);*/
    }
    
    public int ValidateResizeHandleWidth(int resizeHandleWidthInPixels)
    {
        if (resizeHandleWidthInPixels < AppOptionsState.MINIMUM_RESIZE_HANDLE_WIDTH_IN_PIXELS)
            return AppOptionsState.MINIMUM_RESIZE_HANDLE_WIDTH_IN_PIXELS;
    
        return resizeHandleWidthInPixels;
    }

    public void Options_SetResizeHandleWidth(int resizeHandleWidthInPixels, bool updateStorage = true)
    {
        resizeHandleWidthInPixels = ValidateResizeHandleWidth(resizeHandleWidthInPixels);
    
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                ResizeHandleWidthInPixels = resizeHandleWidthInPixels
            }
        };
        
        HandleResizeHandleWidthChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    public void HandleResizeHandleWidthChange()
    {
        Options_ResizeHandleCssWidth = $"width: {GetAppOptionsState().Options.ResizeHandleWidthInPixels.ToCssValue()}px";
    }
    
    public int ValidateResizeHandleHeight(int resizeHandleHeightInPixels)
    {
        if (resizeHandleHeightInPixels < AppOptionsState.MINIMUM_RESIZE_HANDLE_HEIGHT_IN_PIXELS)
            return AppOptionsState.MINIMUM_RESIZE_HANDLE_HEIGHT_IN_PIXELS;
    
        return resizeHandleHeightInPixels;
    }

    public void Options_SetResizeHandleHeight(int resizeHandleHeightInPixels, bool updateStorage = true)
    {
        resizeHandleHeightInPixels = ValidateResizeHandleHeight(resizeHandleHeightInPixels);
    
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                ResizeHandleHeightInPixels = resizeHandleHeightInPixels
            }
        };
        
        HandleResizeHandleHeightChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    public void HandleResizeHandleHeightChange()
    {
        Options_ResizeHandleCssHeight = $"height: {GetAppOptionsState().Options.ResizeHandleHeightInPixels.ToCssValue()}px";
    }

    public int ValidateIconSize(int iconSizeInPixels)
    {
        if (iconSizeInPixels < AppOptionsState.MINIMUM_ICON_SIZE_IN_PIXELS)
            return AppOptionsState.MINIMUM_ICON_SIZE_IN_PIXELS;
    
        return iconSizeInPixels;
    }

    public void Options_SetIconSize(int iconSizeInPixels, bool updateStorage = true)
    {
        iconSizeInPixels = ValidateIconSize(iconSizeInPixels);
    
        var inState = GetAppOptionsState();
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                IconSizeInPixels = iconSizeInPixels
            }
        };
        
        HandleIconSizeChange();
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
        if (updateStorage)
            Options_WriteToStorage();
    }
    
    public void HandleIconSizeChange()
    {
        Options_IconSizeCssValue = GetAppOptionsState().Options.IconSizeInPixels.ToCssValue();
    }

    public async Task Options_SetFromLocalStorageAsync()
    {
        var optionsJsonString = await JsRuntimeCommonApi.LocalStorageGetItem(Options_StorageKey);

        if (string.IsNullOrWhiteSpace(optionsJsonString))
        {
            HandleDefaultOptionsJsonString();
            return;
        }

        CommonOptionsJsonDto? optionsJson = null;
        
        try
        {
            optionsJson = JsonSerializer.Deserialize<CommonOptionsJsonDto>(optionsJsonString);
        }
        catch (System.Text.Json.JsonException)
        {
            // TODO: Preserve the values that do parse.
            await Storage_SetValue(Options_StorageKey, null).ConfigureAwait(false);
        }

        if (optionsJson is null)
        {
            HandleDefaultOptionsJsonString();
            return;
        }
        
        var inState = GetAppOptionsState();

        var matchedTheme = GetThemeState().ThemeList.FirstOrDefault(x => x.Key == optionsJson.ThemeKey);
        
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                ThemeKey = matchedTheme.IsDefault() ? CommonFacts.VisualStudioDarkThemeClone.Key : matchedTheme.Key,
                FontFamily = ValidateFontFamily(optionsJson.FontFamily),
                FontSizeInPixels = ValidateFontSize(optionsJson.FontSizeInPixels),
                ResizeHandleWidthInPixels = ValidateResizeHandleWidth(optionsJson.ResizeHandleWidthInPixels),
                ResizeHandleHeightInPixels = ValidateResizeHandleHeight(optionsJson.ResizeHandleHeightInPixels),
                IconSizeInPixels = ValidateIconSize(optionsJson.IconSizeInPixels)
            }
        };
        HandleThemeChange();
        HandleFontFamilyChange();
        HandleFontSizeChange();
        HandleResizeHandleWidthChange();
        HandleResizeHandleHeightChange();
        HandleIconSizeChange();
    
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
    }

    public void Options_WriteToStorage()
    {
        Enqueue(new CommonWorkArgs
        {
            WorkKind = CommonWorkKind.WriteToLocalStorage,
            WriteToLocalStorage_Key = Options_StorageKey,
            WriteToLocalStorage_Value = new CommonOptionsJsonDto(GetAppOptionsState().Options)
        });
    }
    
    private void HandleDefaultOptionsJsonString()
    {
        var inState = GetAppOptionsState();
    
        _appOptionsState = inState with
        {
            Options = inState.Options with
            {
                ThemeKey = CommonFacts.VisualStudioDarkThemeClone.Key,
                FontSizeInPixels = ValidateFontSize(AppOptionsState.DEFAULT_FONT_SIZE_IN_PIXELS),
            }
        };
        HandleThemeChange();
        HandleFontSizeChange();
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.AppOptionsStateChanged);
    }
}
