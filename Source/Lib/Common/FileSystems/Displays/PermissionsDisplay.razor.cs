using System.Text;
using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.Common.RazorLib.FileSystems.Displays;

public partial class PermissionsDisplay : ComponentBase
{
    [Inject]
    private CommonService CommonService { get; set; } = null!;

    private string _deleteAllowPathTextInput = string.Empty;
    private bool _deleteAllowPathIsDirectoryInput;

    private string _protectPathTextInput = string.Empty;
    private bool _protectPathIsDirectoryInput;

    private void AddModifyDeleteRightsOnClick(
        string localProtectPathTextInput,
        bool localProtectPathIsDirectoryInput)
    {
        CommonService.FileSystemProvider.DeletionPermittedRegister(
            new SimplePath(
                localProtectPathTextInput,
                localProtectPathIsDirectoryInput),
            tokenBuilder: new StringBuilder(),
            formattedBuilder: new StringBuilder());
    }
    
    private void SubmitProtectOnClick(
        string localProtectPathTextInput,
        bool localProtectPathIsDirectoryInput)
    {
        CommonService.FileSystemProvider.ProtectedPathsRegister(
            new SimplePath(
                localProtectPathTextInput,
                localProtectPathIsDirectoryInput));
    }
}
