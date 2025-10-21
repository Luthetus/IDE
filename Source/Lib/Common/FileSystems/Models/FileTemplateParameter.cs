namespace Clair.Common.RazorLib.FileSystems.Models;

public class FileTemplateParameter
{
    public FileTemplateParameter(
        string filename,
        AbsolutePath parentDirectory,
        string parentDirectoryNamespace,
        IFileSystemProvider fileSystemProvider,
        string extensionNoPeriod)
    {
        Filename = filename;
        ParentDirectory = parentDirectory;
        ParentDirectoryNamespace = parentDirectoryNamespace;
        FileSystemProvider = fileSystemProvider;
        ExtensionNoPeriod = extensionNoPeriod;
    }

    public string Filename { get; }
    public AbsolutePath ParentDirectory { get; }
    public string ParentDirectoryNamespace { get; }
    public IFileSystemProvider FileSystemProvider { get; }
    public string ExtensionNoPeriod { get; }
}
