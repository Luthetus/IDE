using Clair.Common.RazorLib.Keys.Models;
using Clair.TextEditor.RazorLib.Groups.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.TextEditor.RazorLib;

public partial class TextEditorService
{
    private readonly object Group_stateModificationLock = new();
    
    // TextEditorGroupService.cs
    private TextEditorGroupState Group_textEditorGroupState = new();
    
    /// <summary>
    /// This somewhat arbitrarily represents the "main" text editor group
    /// so that the most often seen text editor group doesn't require a search
    /// but is instead always known to not be null and available.
    /// </summary>
    public static readonly Key<TextEditorGroup> EditorTextEditorGroupKey = Key<TextEditorGroup>.NewKey();

    public TextEditorGroupState Group_GetTextEditorGroupState() => Group_textEditorGroupState;

    public void Group_SetActiveViewModel(Key<TextEditorGroup> textEditorGroupKey, int textEditorViewModelKey)
    {
        Group_SetActiveViewModelOfGroup(
            textEditorGroupKey,
            textEditorViewModelKey);
    }

    public void Group_RemoveViewModel(Key<TextEditorGroup> textEditorGroupKey, int textEditorViewModelKey)
    {
        Group_RemoveViewModelFromGroup(
            textEditorGroupKey,
            textEditorViewModelKey);
    }

    public void Group_Register(Key<TextEditorGroup> textEditorGroupKey, Category? category = null)
    {
        category ??= new Category("main");

        var textEditorGroup = new TextEditorGroup(
            textEditorGroupKey,
            0,
            new List<int>(),
            category.Value,
            this,
            CommonService);

        Group_Register(textEditorGroup);
    }

    public TextEditorGroup? Group_GetOrDefault(Key<TextEditorGroup> textEditorGroupKey)
    {
        var groupList = Group_GetTextEditorGroupState().GroupList;
        foreach (var x in groupList)
        {
            if (x.GroupKey == textEditorGroupKey)
                return x;
        }
        return null;
    }

    public void Group_AddViewModel(Key<TextEditorGroup> textEditorGroupKey, int textEditorViewModelKey)
    {
        Group_AddViewModelToGroup(
            textEditorGroupKey,
            textEditorViewModelKey);
    }

    public List<TextEditorGroup> Group_GetGroups()
    {
        return Group_GetTextEditorGroupState().GroupList;
    }

    public void Group_Register(TextEditorGroup group)
    {
        lock (Group_stateModificationLock)
        {
            var inState = Group_GetTextEditorGroupState();

            var groupList = inState.GroupList;
            var inGroup = (TextEditorGroup?)null;
            foreach (var x in groupList)
            {
                if (x.GroupKey == group.GroupKey)
                {
                    inGroup = x;
                    break;
                }
            }
            if (inGroup is not null)
                goto finalize;

            var outGroupList = new List<TextEditorGroup>(inState.GroupList);
            outGroupList.Add(group);

            Group_textEditorGroupState = new TextEditorGroupState
            {
                GroupList = outGroupList
            };

            goto finalize;
        }

        finalize:
        SecondaryChanged?.Invoke(SecondaryChangedKind.Group_TextEditorGroupStateChanged);
    }

    public void Group_AddViewModelToGroup(
        Key<TextEditorGroup> groupKey,
        int viewModelKey)
    {
        lock (Group_stateModificationLock)
        {
            var inState = Group_GetTextEditorGroupState();

            var inGroupIndex = -1;
            for (int i = 0; i < inState.GroupList.Count; i++)
            {
                if (inState.GroupList[i].GroupKey == groupKey)
                {
                    inGroupIndex = i;
                    break;
                }
            }
            if (inGroupIndex == -1)
                goto finalize;

            var inGroup = inState.GroupList[inGroupIndex];

            if (inGroup is null)
                goto finalize;

            if (inGroup.ViewModelKeyList.Contains(viewModelKey))
                goto finalize;

            var outViewModelKeyList = new List<int>(inGroup.ViewModelKeyList);
            outViewModelKeyList.Add(viewModelKey);

            var outGroup = inGroup with
            {
                ViewModelKeyList = outViewModelKeyList
            };

            if (outGroup.ViewModelKeyList.Count == 1)
            {
                outGroup = outGroup with
                {
                    ActiveViewModelKey = viewModelKey
                };
            }

            var outGroupList = new List<TextEditorGroup>(inState.GroupList);
            outGroupList[inGroupIndex] = outGroup;

            TextEditorGroup editorTextEditorGroup;
            if (outGroup.GroupKey == TextEditorService.EditorTextEditorGroupKey)
            {
                editorTextEditorGroup = outGroup;
            }
            else
            {
                editorTextEditorGroup = inState.EditorTextEditorGroup;
            }

            Group_textEditorGroupState = new TextEditorGroupState
            {
                GroupList = outGroupList,
                EditorTextEditorGroup = editorTextEditorGroup
            };

            goto finalize;
        }

        finalize:
        SecondaryChanged?.Invoke(SecondaryChangedKind.Group_TextEditorGroupStateChanged);
        Group_PostScroll(groupKey, viewModelKey);
    }

    public void Group_RemoveViewModelFromGroup(
        Key<TextEditorGroup> groupKey,
        int viewModelKey)
    {
        lock (Group_stateModificationLock)
        {
            var inState = Group_GetTextEditorGroupState();

            var inGroupIndex = -1;
            for (int i = 0; i < inState.GroupList.Count; i++)
            {
                if (inState.GroupList[i].GroupKey == groupKey)
                {
                    inGroupIndex = i;
                    break;
                }
            }
            if (inGroupIndex == -1)
                goto finalize;

            var inGroup = inState.GroupList[inGroupIndex];

            if (inGroup is null)
                goto finalize;

            var indexOfViewModelKeyToRemove = -1;
            for (int i = 0; i < inGroup.ViewModelKeyList.Count; i++)
            {
                if (inGroup.ViewModelKeyList[i] == viewModelKey)
                {
                    indexOfViewModelKeyToRemove = i;
                    break;
                }
            }
            if (indexOfViewModelKeyToRemove == -1)
                goto finalize;

            var viewModelKeyToRemove = inGroup.ViewModelKeyList[indexOfViewModelKeyToRemove];

            var nextViewModelKeyList = new List<int>(inGroup.ViewModelKeyList);
            nextViewModelKeyList.RemoveAt(indexOfViewModelKeyToRemove);

            int nextActiveTextEditorModelKey;

            if (inGroup.ActiveViewModelKey != 0 &&
                inGroup.ActiveViewModelKey != viewModelKeyToRemove)
            {
                // Because the active tab was not removed, do not bother setting a different
                // active tab.
                nextActiveTextEditorModelKey = inGroup.ActiveViewModelKey;
            }
            else
            {
                // The active tab was removed, therefore a new active tab must be chosen.

                // This variable is done for renaming
                var activeViewModelKeyIndex = indexOfViewModelKeyToRemove;

                // If last item in list
                if (activeViewModelKeyIndex >= inGroup.ViewModelKeyList.Count - 1)
                {
                    activeViewModelKeyIndex--;
                }
                else
                {
                    // ++ operation because this calculation is using the immutable list where
                    // the view model was not removed.
                    activeViewModelKeyIndex++;
                }

                // If removing the active will result in empty list set the active as an Empty TextEditorViewModelKey
                if (inGroup.ViewModelKeyList.Count - 1 == 0)
                    nextActiveTextEditorModelKey = 0;
                else
                    nextActiveTextEditorModelKey = inGroup.ViewModelKeyList[activeViewModelKeyIndex];
            }

            var outGroupList = new List<TextEditorGroup>(inState.GroupList);

            var outGroup = inGroup with
            {
                ViewModelKeyList = nextViewModelKeyList,
                ActiveViewModelKey = nextActiveTextEditorModelKey
            };

            outGroupList[inGroupIndex] = outGroup;

            TextEditorGroup editorTextEditorGroup;
            if (outGroup.GroupKey == TextEditorService.EditorTextEditorGroupKey)
            {
                editorTextEditorGroup = outGroup;
            }
            else
            {
                editorTextEditorGroup = inState.EditorTextEditorGroup;
            }

            Group_textEditorGroupState = new TextEditorGroupState
            {
                GroupList = outGroupList,
                EditorTextEditorGroup = editorTextEditorGroup
            };

            goto finalize;
        }

        finalize:
        SecondaryChanged?.Invoke(SecondaryChangedKind.Group_TextEditorGroupStateChanged);
        Group_PostScroll(groupKey, Group_GetOrDefault(groupKey).ActiveViewModelKey);
    }

    public void Group_SetActiveViewModelOfGroup(
        Key<TextEditorGroup> groupKey,
        int viewModelKey)
    {
        lock (Group_stateModificationLock)
        {
            var inState = Group_GetTextEditorGroupState();

            var inGroupIndex = -1;
            for (int i = 0; i < inState.GroupList.Count; i++)
            {
                if (inState.GroupList[i].GroupKey == groupKey)
                {
                    inGroupIndex = i;
                    break;
                }
            }
            if (inGroupIndex == -1)
                goto finalize;

            var inGroup = inState.GroupList[inGroupIndex];

            if (inGroup is null)
                goto finalize;

            var outGroupList = new List<TextEditorGroup>(inState.GroupList);

            var outGroup = inGroup with
            {
                ActiveViewModelKey = viewModelKey
            };
            
            outGroupList[inGroupIndex] = outGroup;

            TextEditorGroup editorTextEditorGroup;
            if (outGroup.GroupKey == TextEditorService.EditorTextEditorGroupKey)
            {
                editorTextEditorGroup = outGroup;
            }
            else
            {
                editorTextEditorGroup = inState.EditorTextEditorGroup;
            }

            Group_textEditorGroupState = new TextEditorGroupState
            {
                GroupList = outGroupList,
                EditorTextEditorGroup = editorTextEditorGroup
            };

            goto finalize;
        }

        finalize:
        Group_PostScroll(groupKey, viewModelKey);
        SecondaryChanged?.Invoke(SecondaryChangedKind.Group_TextEditorGroupStateChanged);
    }

    public void Group_Dispose(Key<TextEditorGroup> groupKey)
    {
        lock (Group_stateModificationLock)
        {
            var inState = Group_GetTextEditorGroupState();

            var inGroup = (TextEditorGroup?)null;
            foreach (var x in inState.GroupList)
            {
                if (x.GroupKey == groupKey)
                {
                    inGroup = x;
                    break;
                }
            }
            if (inGroup is null || inGroup.GroupKey == TextEditorService.EditorTextEditorGroupKey)
                goto finalize;

            var outGroupList = new List<TextEditorGroup>(inState.GroupList);
            outGroupList.Remove(inGroup);

            Group_textEditorGroupState = new TextEditorGroupState
            {
                GroupList = outGroupList,
            };

            goto finalize;
        }

        finalize:
        SecondaryChanged?.Invoke(SecondaryChangedKind.Group_TextEditorGroupStateChanged);
    }

    private void Group_PostScroll(
        Key<TextEditorGroup> groupKey,
        int viewModelKey)
    {
        WorkerArbitrary.PostUnique(editContext =>
        {
            var viewModelModifier = editContext.GetViewModelModifier(viewModelKey);
            if (viewModelModifier is null)
                return ValueTask.CompletedTask;

            viewModelModifier.ScrollWasModified = true;
            return ValueTask.CompletedTask;
        });
    }
}
