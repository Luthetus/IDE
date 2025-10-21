using Microsoft.JSInterop;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.TextEditor.RazorLib.JavaScriptObjects.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Displays;

namespace Clair.TextEditor.RazorLib.JsRuntimes.Models;

/// <remarks>
/// This class is an exception to the naming convention, "don't use the word 'Clair' in class names".
/// 
/// Reason for this exception: the 'IJSRuntime' datatype is far more common in code,
///     than some specific type (example: DialogDisplay.razor).
///     So, adding 'Clair' in the class name for redundancy seems meaningful here.
/// </remarks>
public class ClairTextEditorJavaScriptInteropApi
{
    private readonly IJSRuntime _jsRuntime;

    public ClairTextEditorJavaScriptInteropApi(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask ScrollElementIntoView(string elementId)
    {
        return _jsRuntime.InvokeVoidAsync(
            "clairTextEditor.scrollElementIntoView",
            elementId);
    }
    
    public ValueTask SetPreventDefaultsAndStopPropagations(
        DotNetObjectReference<TextEditorViewModelSlimDisplay> dotNetHelper,
        string contentElementId,
        string rowSectionElementId,
        string HORIZONTAL_ScrollbarElementId,
        string VERTICAL_ScrollbarElementId,
        string CONNECTOR_ScrollbarElementId)
    {
        return _jsRuntime.InvokeVoidAsync(
            "clairTextEditor.setPreventDefaultsAndStopPropagations",
            dotNetHelper,
            contentElementId,
            rowSectionElementId,
            HORIZONTAL_ScrollbarElementId,
            VERTICAL_ScrollbarElementId,
            CONNECTOR_ScrollbarElementId);
    }

    public ValueTask<CharAndLineMeasurements> GetCharAndLineMeasurementsInPixelsById(
        string measureCharacterWidthAndLineHeightElementId)
    {
        return _jsRuntime.InvokeAsync<CharAndLineMeasurements>(
            "clairTextEditor.getCharAndLineMeasurementsInPixelsById",
            measureCharacterWidthAndLineHeightElementId);
    }

    /// <summary>
    /// TODO: This javascript function is only used from other javascript functions.
    /// </summary>
    public ValueTask<string> EscapeHtml(string input)
    {
        return _jsRuntime.InvokeAsync<string>(
            "clairTextEditor.escapeHtml",
            input);
    }

    public ValueTask<RelativeCoordinates> GetRelativePosition(
        string elementId,
        double clientX,
        double clientY)
    {
        return _jsRuntime.InvokeAsync<RelativeCoordinates>(
            "clairTextEditor.getRelativePosition",
            elementId,
            clientX,
            clientY);
    }

    public ValueTask SetScrollPositionBoth(
        string bodyElementId,
        double scrollLeftInPixels,
        double scrollTopInPixels)
    {
        return _jsRuntime.InvokeVoidAsync(
            "clairTextEditor.setScrollPositionBoth",
            bodyElementId,
            scrollLeftInPixels,
            scrollTopInPixels);
    }
    
    public ValueTask SetScrollPositionLeft(
        string bodyElementId,
        double scrollLeftInPixels)
    {
        return _jsRuntime.InvokeVoidAsync(
            "clairTextEditor.setScrollPositionLeft",
            bodyElementId,
            scrollLeftInPixels);
    }
    
    public ValueTask SetScrollPositionTop(
        string bodyElementId,
        double scrollTopInPixels)
    {
        return _jsRuntime.InvokeVoidAsync(
            "clairTextEditor.setScrollPositionTop",
            bodyElementId,
            scrollTopInPixels);
    }

    public ValueTask<TextEditorDimensions> GetTextEditorMeasurementsInPixelsById(
        string elementId)
    {
        return _jsRuntime.InvokeAsync<TextEditorDimensions>(
            "clairTextEditor.getTextEditorMeasurementsInPixelsById",
            elementId);
    }

    public ValueTask<ElementPositionInPixels> GetBoundingClientRect(string primaryCursorContentId)
    {
        return _jsRuntime.InvokeAsync<ElementPositionInPixels>(
            "clairTextEditor.getBoundingClientRect",
            primaryCursorContentId);
    }
}
