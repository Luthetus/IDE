using Microsoft.JSInterop;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Dimensions.Models;

namespace Clair.Common.RazorLib.JsRuntimes.Models;

/// <remarks>
/// This class is an exception to the naming convention, "don't use the word 'Clair' in class names".
/// 
/// Reason for this exception: the 'IJSRuntime' datatype is far more common in code,
///     than some specific type (example: DialogDisplay.razor).
///     So, adding 'Clair' in the class name for redundancy seems meaningful here.
/// </remarks>
public class ClairCommonJavaScriptInteropApi
{
    public IJSRuntime JsRuntime { get; }

    public ClairCommonJavaScriptInteropApi(IJSRuntime jsRuntime)
    {
        JsRuntime = jsRuntime;
    }

    public ValueTask<AppDimensionState> SubscribeWindowSizeChanged(
        DotNetObjectReference<BrowserResizeInterop> browserResizeInteropDotNetObjectReference,
        string rootHtmlElementId)
    {
        return JsRuntime.InvokeAsync<AppDimensionState>(
            "clairCommon.subscribeWindowSizeChanged",
            browserResizeInteropDotNetObjectReference,
            rootHtmlElementId);
    }

    public ValueTask DisposeWindowSizeChanged()
    {
        return JsRuntime.InvokeVoidAsync(
            "clairCommon.disposeWindowSizeChanged");
    }
    
    public ValueTask FocusHtmlElementById(string elementId, bool preventScroll = false)
    {
        return JsRuntime.InvokeVoidAsync(
            "clairCommon.focusHtmlElementById",
            elementId,
            preventScroll);
    }

    public ValueTask<bool> TryFocusHtmlElementById(string elementId)
    {
        return JsRuntime.InvokeAsync<bool>(
            "clairCommon.tryFocusHtmlElementById",
            elementId);
    }
    
    public ValueTask<MeasuredHtmlElementDimensions> MeasureElementById(string elementId)
    {
        return JsRuntime.InvokeAsync<MeasuredHtmlElementDimensions>(
            "clairCommon.measureElementById",
            elementId);
    }

    public ValueTask LocalStorageSetItem(string key, object? value)
    {
        return JsRuntime.InvokeVoidAsync(
            "clairCommon.localStorageSetItem",
            key,
            value);
    }

    public ValueTask<string?> LocalStorageGetItem(string key)
    {
        return JsRuntime.InvokeAsync<string?>(
            "clairCommon.localStorageGetItem",
            key);
    }

    public ValueTask<string> ReadClipboard()
    {
        return JsRuntime.InvokeAsync<string>(
            "clairCommon.readClipboard");
    }

    public ValueTask SetClipboard(string value)
    {
        return JsRuntime.InvokeVoidAsync(
            "clairCommon.setClipboard",
            value);
    }

    public ValueTask<ContextMenuFixedPosition> GetTreeViewContextMenuFixedPosition(
        string nodeElementId)
    {
        return JsRuntime.InvokeAsync<ContextMenuFixedPosition>(
            "clairCommon.getTreeViewContextMenuFixedPosition",
            nodeElementId);
    }
}
