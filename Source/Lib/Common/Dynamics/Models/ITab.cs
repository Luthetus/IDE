namespace Clair.Common.RazorLib.Dynamics.Models;

public interface ITab : IDynamicViewModel
{
    public ITabGroup? TabGroup { get; set; }
}
