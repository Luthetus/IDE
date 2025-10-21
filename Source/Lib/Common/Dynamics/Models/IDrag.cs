using Microsoft.AspNetCore.Components.Web;

namespace Clair.Common.RazorLib.Dynamics.Models;

public interface IDrag : IDynamicViewModel
{
    public List<IDropzone> DropzoneList { get; set; }

    public Type DragComponentType { get; }
    public string? DragCssClass { get; set; }
    public string? DragCssStyle { get; set; }

    public Task OnDragStartAsync();
    public Task OnDragEndAsync(MouseEventArgs mouseEventArgs, IDropzone? dropzone);
}
