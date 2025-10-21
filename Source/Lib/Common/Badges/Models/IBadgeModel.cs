using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.Badges.Models;

public interface IBadgeModel
{
    public Key<IBadgeModel> Key { get; }
    public BadgeKind BadgeKind { get; }
    public int Count { get; }
    
    public void OnClick();
    public void AddSubscription(Func<Task> updateUiFunc);
    public void DisposeSubscription();
}
