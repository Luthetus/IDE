using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.Panels.Models;

public record PanelGroupDropzone(
        MeasuredHtmlElementDimensions MeasuredHtmlElementDimensions,
        Key<PanelGroup> PanelGroupKey,
        ElementDimensions ElementDimensions,
        Key<IDropzone> DropzoneKey,
        string? CssClass,
        string? CssStyle)
    : IDropzone;
