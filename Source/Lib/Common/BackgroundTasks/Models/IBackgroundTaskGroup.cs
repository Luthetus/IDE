using Clair.Common.RazorLib.Keys.Models;

namespace Clair.Common.RazorLib.BackgroundTasks.Models;

public interface IBackgroundTaskGroup
{
    public Key<IBackgroundTaskGroup> BackgroundTaskKey { get; }
    public bool __TaskCompletionSourceWasCreated { get; set; }
    
    /// <summary>
    /// This method is the actual work item that gets awaited in order to handle the event.
    /// </summary>
    public ValueTask HandleEvent();
}
