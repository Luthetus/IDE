using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dropdowns.Models;

namespace Clair.Common.RazorLib;

public partial class CommonService
{
    private DropdownState _dropdownState = new();
    
    public DropdownState GetDropdownState() => _dropdownState;
    
    public void Dropdown_ReduceRegisterAction(DropdownRecord dropdown)
    {
        var inState = GetDropdownState();
    
        var indexExistingDropdown = -1;
        for (int i = 0; i < inState.DropdownList.Count; i++)
        {
            if (inState.DropdownList.UNSAFE_Items[i].Key == dropdown.Key)
            {
                indexExistingDropdown = i;
                break;
            }
        }

        if (indexExistingDropdown != -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
            return;
        }

        _dropdownState = inState with
        {
            DropdownList = inState.DropdownList.New_Add(dropdown)
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
        return;
    }

    public void Dropdown_ReduceDisposeAction(Key<DropdownRecord> key)
    {
        var inState = GetDropdownState();
    
        var indexExistingDropdown = -1;
        for (int i = 0; i < inState.DropdownList.Count; i++)
        {
            if (inState.DropdownList.UNSAFE_Items[i].Key == key)
            {
                indexExistingDropdown = i;
                break;
            }
        }

        if (indexExistingDropdown == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
            return;
        }
            
        _dropdownState = inState with
        {
            DropdownList = inState.DropdownList.New_RemoveAt(indexExistingDropdown)
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
        return;
    }

    public void Dropdown_ReduceClearAction()
    {
        var inState = GetDropdownState();
    
        _dropdownState = inState with
        {
            DropdownList = new ValueList<DropdownRecord>(capacity: 4)
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
        return;
    }

    public void Dropdown_ReduceFitOnScreenAction(DropdownRecord dropdown)
    {
        var inState = GetDropdownState();
        
        var indexExistingDropdown = -1;
        for (int i = 0; i < inState.DropdownList.Count; i++)
        {
            if (inState.DropdownList.UNSAFE_Items[i].Key == dropdown.Key)
            {
                indexExistingDropdown = i;
                break;
            }
        }

        if (indexExistingDropdown == -1)
        {
            CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
            return;
        }
        
        var inDropdown = inState.DropdownList.UNSAFE_Items[indexExistingDropdown];

        var outDropdown = inDropdown with
        {
            Width = dropdown.Width,
            Height = dropdown.Height,
            Left = dropdown.Left,
            Top = dropdown.Top
        };
        
        _dropdownState = inState with
        {
            DropdownList = inState.DropdownList.Clone_SetItem(indexExistingDropdown, outDropdown)
        };
        
        CommonUiStateChanged?.Invoke(CommonUiEventKind.DropdownStateChanged);
        return;
    }
}
