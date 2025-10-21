using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.Dynamics.Models;

public interface IDropzone
{
    public Key<IDropzone> DropzoneKey { get; }
    public MeasuredHtmlElementDimensions MeasuredHtmlElementDimensions { get; }
    public ElementDimensions ElementDimensions { get; }
    public string? CssClass { get; }
    public string? CssStyle { get; }
}
