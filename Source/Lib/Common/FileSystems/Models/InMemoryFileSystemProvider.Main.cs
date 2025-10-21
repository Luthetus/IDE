using System.Text;

namespace Clair.Common.RazorLib.FileSystems.Models;

public partial class InMemoryFileSystemProvider : IFileSystemProvider
{
    private readonly object _pathLock = new();

    public InMemoryFileSystemProvider(CommonService commonService)
    {
        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();

        RootDirectoryAbsolutePath = new AbsolutePath("/", true, this, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);
        HomeDirectoryAbsolutePath = new AbsolutePath("/Repos/", true, this, tokenBuilder, formattedBuilder, AbsolutePathNameKind.NameWithExtension);

        SafeRoamingApplicationDataDirectoryAbsolutePath = new AbsolutePath(
            "/AppData/Roaming/Clair/",
            true,
            this,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);

        ProtectedPathList.Add(new(
            RootDirectoryAbsolutePath.Value,
            RootDirectoryAbsolutePath.IsDirectory));

        ProtectedPathList.Add(new(
            HomeDirectoryAbsolutePath.Value,
            HomeDirectoryAbsolutePath.IsDirectory));

        ProtectedPathList.Add(new(
            SafeRoamingApplicationDataDirectoryAbsolutePath.Value,
            SafeRoamingApplicationDataDirectoryAbsolutePath.IsDirectory));

        // Redundantly hardcode some obvious cases for protection.
        {
            ProtectedPathList.Add(new SimplePath("/", true));
            ProtectedPathList.Add(new SimplePath("\\", true));
            ProtectedPathList.Add(new SimplePath("", true));
        }

        _file = new InMemoryFileHandler(this, commonService);
        _directory = new InMemoryDirectoryHandler(this, commonService);
    }

    public IReadOnlyList<InMemoryFile> Files => _files;

    public IFileHandler File => _file;
    public IDirectoryHandler Directory => _directory;
    
    /// <summary>
    /// I want the website demo to focus on the text editor.
    /// In order to create the files for the demo I run some very unoptimized code.
    /// I'm going to just manually add the 3 demo files here by explicit casting the IFileSystemProvider.
    /// </summary>
    public List<InMemoryFile> __Files => _files;

    public AbsolutePath RootDirectoryAbsolutePath { get; }
    public AbsolutePath HomeDirectoryAbsolutePath { get; }
    public AbsolutePath SafeRoamingApplicationDataDirectoryAbsolutePath { get; }
    public string DriveExecutingFromNoDirectorySeparator { get; } = string.Empty;
    public HashSet<SimplePath> DeletionPermittedPathList { get; private set; } = new();
    public HashSet<SimplePath> ProtectedPathList { get; private set; } = new();

    public char DirectorySeparatorChar => '/';
    public char AltDirectorySeparatorChar => '\\';

    public string DirectorySeparatorCharToStringResult => "/";

    public bool IsDirectorySeparator(char character) =>
        character == DirectorySeparatorChar || character == AltDirectorySeparatorChar;

    public string GetRandomFileName() => Guid.NewGuid().ToString();

    public string JoinPaths(string pathOne, string pathTwo)
    {
        if (IsDirectorySeparator(pathOne.LastOrDefault()))
            return pathOne + pathTwo;

        return string.Join(DirectorySeparatorChar, pathOne, pathTwo);
    }

    public void AssertDeletionPermitted(string path, bool isDirectory)
    {
        PermittanceChecker.AssertDeletionPermitted(this, path, isDirectory);
    }

    public void DeletionPermittedRegister(SimplePath simplePath, StringBuilder tokenBuilder, StringBuilder formattedBuilder)
    {
        lock (_pathLock)
        {
            var absolutePath = simplePath.AbsolutePath;

            if (absolutePath == "/" || absolutePath == "\\" || string.IsNullOrWhiteSpace(absolutePath))
                return;

            if (PermittanceChecker.IsRootOrHomeDirectory(simplePath, this, tokenBuilder, formattedBuilder))
                return;

            DeletionPermittedPathList.Add(simplePath);
        }
    }

    public void DeletionPermittedDispose(SimplePath simplePath)
    {
        lock (_pathLock)
        {
            DeletionPermittedPathList.Remove(simplePath);
        }
    }

    public void ProtectedPathsRegister(SimplePath simplePath)
    {
        lock (_pathLock)
        {
            ProtectedPathList.Add(simplePath);
        }
    }

    public void ProtectedPathsDispose(SimplePath simplePath, StringBuilder tokenBuilder, StringBuilder formattedBuilder)
    {
        lock (_pathLock)
        {
            var absolutePath = simplePath.AbsolutePath;

            if (absolutePath == "/" || absolutePath == "\\" || string.IsNullOrWhiteSpace(absolutePath))
                return;

            if (PermittanceChecker.IsRootOrHomeDirectory(simplePath, this, tokenBuilder, formattedBuilder))
                return;

            ProtectedPathList.Remove(simplePath);
        }
    }

    /// <summary>
    /// I want the website demo to focus on the text editor.
    /// In order to create the files for the demo I run some very unoptimized code.
    /// I'm going to just manually add the 3 demo files by explicit casting the IFileSystemProvider.
    /// Then I can expose '__Files' so that the website can quickly add these in.
    /// </summary>
    private readonly List<InMemoryFile> _files = new();
    private readonly SemaphoreSlim _modificationSemaphore = new(1, 1);
    private readonly InMemoryFileHandler _file;
    private readonly InMemoryDirectoryHandler _directory;
}
