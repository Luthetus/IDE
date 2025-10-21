using System.Text;

namespace Clair.Common.RazorLib.FileSystems.Models;

public class LocalFileSystemProvider : IFileSystemProvider
{
    public LocalFileSystemProvider(CommonService commonService)
    {
        DirectorySeparatorCharToStringResult = Path.DirectorySeparatorChar.ToString();

        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();

        RootDirectoryAbsolutePath = new AbsolutePath(
            "/",
            true,
            this,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);

        HomeDirectoryAbsolutePath = new AbsolutePath(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            true,
            this,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);

        var actualRoamingApplicationDataDirectoryAbsolutePath = new AbsolutePath(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            true,
            this,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);

#if DEBUG
        var safeRelativeDirectory = "Clair/Debug/";
#else
        var safeRelativeDirectory = "Clair/Release/";
#endif

        SafeRoamingApplicationDataDirectoryAbsolutePath = new AbsolutePath(
            JoinPaths(actualRoamingApplicationDataDirectoryAbsolutePath.Value, safeRelativeDirectory),
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

            try
            {
                var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();

                if (entryAssembly is not null)
                {
                    var driveExecutingFrom = Path.GetPathRoot(entryAssembly.Location);

                    if (driveExecutingFrom is not null)
                    {
                        if (driveExecutingFrom.EndsWith('/') || driveExecutingFrom.EndsWith('\\'))
                        {
                            DriveExecutingFromNoDirectorySeparator = driveExecutingFrom[..^1];

                            ProtectedPathList.Add(
                                new SimplePath(DriveExecutingFromNoDirectorySeparator + '/',
                                true));

                            ProtectedPathList.Add(
                                new SimplePath(DriveExecutingFromNoDirectorySeparator + '\\',
                                true));
                        }
                        else
                        {
                            DriveExecutingFromNoDirectorySeparator = driveExecutingFrom;
                            ProtectedPathList.Add(new SimplePath(driveExecutingFrom, true));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // This code is intended to be an extra level of caution.
                // it trys to add the current drive to the protected path list if possible.
            }
        }

        File = new LocalFileHandler(commonService);
        Directory = new LocalDirectoryHandler(commonService);
    }

    public IFileHandler File { get; }
    public IDirectoryHandler Directory { get; }

    private readonly object _pathLock = new();

    public AbsolutePath RootDirectoryAbsolutePath { get; }
    public AbsolutePath HomeDirectoryAbsolutePath { get; }
    public AbsolutePath SafeRoamingApplicationDataDirectoryAbsolutePath { get; }

    public string DriveExecutingFromNoDirectorySeparator { get; }

    public char DirectorySeparatorChar => Path.DirectorySeparatorChar;
    public char AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

    public string DirectorySeparatorCharToStringResult { get; }

    public HashSet<SimplePath> DeletionPermittedPathList { get; private set; } = [];
    public HashSet<SimplePath> ProtectedPathList { get; private set; } = [];

    public bool IsDirectorySeparator(char character) =>
        character == DirectorySeparatorChar || character == AltDirectorySeparatorChar;

    public string GetRandomFileName() => Path.GetRandomFileName();

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
}
