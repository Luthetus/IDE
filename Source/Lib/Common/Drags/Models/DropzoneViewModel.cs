using Clair.Common.RazorLib.Dimensions.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.Drags.Models;

public record DropzoneViewModel(
        Key<IDropzone> Key,
        MeasuredHtmlElementDimensions MeasuredHtmlElementDimensions,
        ElementDimensions DropzoneElementDimensions,
        Key<IDropzone> DropzoneKey,
        ElementDimensions ElementDimensions,
        string CssClass,
        string CssStyle)
    : IDropzone;
