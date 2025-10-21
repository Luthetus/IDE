using Microsoft.JSInterop;

namespace Clair.TextEditor.RazorLib.JsRuntimes.Models;

/// <remarks>
/// This class is an exception to the naming convention, "don't use the word 'Clair' in class names".
/// 
/// Reason for this exception: the 'IJSRuntime' datatype is far more common in code,
///     than some specific type (example: DialogDisplay.razor).
///     So, adding 'Clair' in the class name for redundancy seems meaningful here.
/// </remarks>
public static class ClairTextEditorJsRuntimeExtensionMethods
{
    public static ClairTextEditorJavaScriptInteropApi GetClairTextEditorApi(this IJSRuntime jsRuntime)
    {
        return new ClairTextEditorJavaScriptInteropApi(jsRuntime);
    }
}
