using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dynamics.Models;

namespace Clair.Common.RazorLib;

public partial class CommonService
{
    /// <summary>
    /// TODO: Some methods just invoke a single method, so remove the redundant middle man.
    /// TODO: Thread safety.
    /// </summary>
    private DialogState _dialogState = new();
    
    public DialogState GetDialogState() => _dialogState;
    
    public void Dialog_ReduceRegisterAction(IDialog dialog)
    {
        var inState = GetDialogState();

        for (int i = 0; i < inState.DialogList.Count; i++)
        {
            var x = inState.DialogList.u_Items[i];
            if (x.DynamicViewModelKey == dialog.DynamicViewModelKey)
            {
                _ = Task.Run(async () =>
                    await JsRuntimeCommonApi
                        .FocusHtmlElementById(dialog.DialogFocusPointHtmlElementId)
                        .ConfigureAwait(false));
                
                CommonUiStateChanged?.Invoke(CommonUiEventKind.DialogStateChanged);
                return;
            }
        }

        _dialogState = inState with 
        {
            DialogList = inState.DialogList.New_Add(dialog),
            ActiveDialogKey = dialog.DynamicViewModelKey,
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DialogStateChanged);
        return;
    }

    public void Dialog_ReduceSetIsMaximizedAction(
        Key<IDynamicViewModel> dynamicViewModelKey,
        bool isMaximized)
    {
        var inState = GetDialogState();
        
        var indexDialog = -1;
        for (int i = 0; i < inState.DialogList.Count; i++)
        {
            if (inState.DialogList.u_Items[i].DynamicViewModelKey == dynamicViewModelKey)
            {
                indexDialog = i;
                break;
            }
        }

        if (indexDialog == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.DialogStateChanged);
            return;
        }
            
        var inDialog = inState.DialogList.u_Items[indexDialog];
        _dialogState = inState with
        {
            DialogList = inState.DialogList.New_SetItem(indexDialog, inDialog.SetDialogIsMaximized(isMaximized))
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DialogStateChanged);
        return;
    }
    
    public void Dialog_ReduceSetActiveDialogKeyAction(Key<IDynamicViewModel> dynamicViewModelKey)
    {
        var inState = GetDialogState();
        
        _dialogState = inState with { ActiveDialogKey = dynamicViewModelKey };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.ActiveDialogKeyChanged);
        return;
    }

    public void Dialog_ReduceDisposeAction(Key<IDynamicViewModel> dynamicViewModelKey)
    {
        var inState = GetDialogState();
    
        var indexDialog = -1;
        for (int i = 0; i < inState.DialogList.Count; i++)
        {
            if (inState.DialogList.u_Items[i].DynamicViewModelKey == dynamicViewModelKey)
            {
                indexDialog = i;
                break;
            }
        }

        if (indexDialog == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.DialogStateChanged);
            return;
        }

        _dialogState = inState with
        {
            DialogList = inState.DialogList.New_RemoveAt(indexDialog)
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DialogStateChanged);
        return;
    }
}
