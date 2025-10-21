namespace Clair.Common.RazorLib.FileSystems.Models;

public struct FileTemplate
{
    public FileTemplate(
        string displayName,
        FileTemplateKind fileTemplateKind,
        string fileExtensionNoPeriod,
        bool initialCheckedStateWhenIsRelatedFile)
    {
        DisplayName = displayName;
        FileTemplateKind = fileTemplateKind;
        FileExtensionNoPeriod = fileExtensionNoPeriod;
        InitialCheckedStateWhenIsRelatedFile = initialCheckedStateWhenIsRelatedFile;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName { get; }
    public FileTemplateKind FileTemplateKind { get; }
    public string FileExtensionNoPeriod { get; }
    public bool InitialCheckedStateWhenIsRelatedFile { get; }
}
