using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dynamics.Models;

namespace Clair.Common.RazorLib;

public partial class CommonService
{
    private NotificationState _notificationState = new();
    
    public NotificationState GetNotificationState() => _notificationState;

    public void Notification_ReduceRegisterAction(INotification notification)
    {
        lock (_stateModificationLock)
        {
            _notificationState = _notificationState with
            {
                DefaultList = _notificationState.DefaultList.Add(notification)
            };
        }
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.NotificationStateChanged);
    }

    public void Notification_ReduceDisposeAction(Key<IDynamicViewModel> key)
    {
        lock (_stateModificationLock)
        {
            var indexNotification = -1;
            for (int i = 0; i < _notificationState.DefaultList.Count; i++)
            {
                if (_notificationState.DefaultList.Items[i].DynamicViewModelKey == key)
                {
                    indexNotification = i;
                    break;
                }
            }
    
            if (indexNotification != -1)
            {
                _notificationState = _notificationState with
                {
                    DefaultList = _notificationState.DefaultList.RemoveAt(indexNotification)
                };
            }
        }
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.NotificationStateChanged);
    }

    public void Notification_ReduceClearDefaultAction()
    {
        /*lock (_stateModificationLock)
        {
            _notificationState = _notificationState with
            {
                DefaultList = new List<INotification>()
            };
        }
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.NotificationStateChanged);*/
    }
}
