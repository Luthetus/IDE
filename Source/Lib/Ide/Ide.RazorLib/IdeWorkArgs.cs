using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Dynamics.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Ide.RazorLib.FileSystems.Models;
using Clair.Ide.RazorLib.InputFiles.Models;

namespace Clair.Ide.RazorLib.BackgroundTasks.Models;

public sealed class IdeWorkArgs
{
    public IdeWorkKind WorkKind { get; set; }
    public string StringValue { get; set; }
    public TextEditorModel TextEditorModel { get; set; }
    public DateTime FileLastWriteTime { get; set; }
    public Key<IDynamicViewModel> NotificationInformativeKey { get; set; }
    public AbsolutePath AbsolutePath { get; set; }
    public TreeViewAbsolutePath TreeViewAbsolutePath { get; set; }
    public string NamespaceString { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public Func<Task> OnAfterCompletion { get; set; }
    public Func<AbsolutePath, Task> OnAfterSubmitFunc { get; set; }
    public Func<AbsolutePath, Task<bool>> SelectionIsValidFunc { get; set; }
    public Func<DateTime?, Task> OnAfterSaveCompletedWrittenDateTimeFunc { get; set; }
    public FileTemplate? ExactMatchFileTemplate { get; set; }
    public List<InputFilePattern> InputFilePatterns { get; set; }
    public List<FileTemplate> RelatedMatchFileTemplatesList { get; set; }
}
