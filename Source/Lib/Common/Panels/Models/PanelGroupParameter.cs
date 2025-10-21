using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.Panels.Models;

public struct PanelGroupParameter
{
    public PanelGroupParameter(
        Key<PanelGroup> panelGroupKey,
        string cssClassString)
    {
        PanelGroupKey = panelGroupKey;
        CssClassString = cssClassString;
    }

    public Key<PanelGroup> PanelGroupKey { get; set; } = Key<PanelGroup>.Empty;
    public string CssClassString { get; set; } = null!;
    
    public string PanelPositionCss { get; set; }
    public string HtmlIdTabs { get; set; }
}
