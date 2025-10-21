using System.Text;

namespace Clair.Common.RazorLib.FileSystems.Models;

/// <summary>
/// Verify that 'ExactInput is not null' to know this struct was constructed, not defaulted.
/// </summary>
public struct AbsolutePath
{
    /// <summary>
    /// If providing tokenBuilder or formattedBuilder, ensure they are '.Clear()'ed prior to invoking the constructor.
    /// Prior to returning from the constructor, tokenBuilder and formattedBuilder will be '.Clear()'ed.
    /// 
    /// The invoker of this method must choose between "myFile.txt" or "myFile" as the resulting 'Name' property.
    /// This is done with the `shouldNameContainsExtension` argument,
    /// and this argument gets stored on the instance as the `NameContainsExtension` property.
    /// </summary>
    public AbsolutePath(
        string absolutePathString,
        bool isDirectory,
        IFileSystemProvider fileSystemProvider,
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        AbsolutePathNameKind nameKind,
        List<string>? ancestorDirectoryList = null)
    {
        NameKind = nameKind;
        bool seenRootDrive = false;

        IsDirectory = isDirectory;
    
        var lengthAbsolutePathString = absolutePathString.Length;
        
        if (IsDirectory && lengthAbsolutePathString > 1)
        {
            // Strip the last character if this is a directory, where the exact input ended in a directory separator char.
            // Reasoning: This standardizes what a directory looks like within the scope of this method.
            //
            if (fileSystemProvider.IsDirectorySeparator(absolutePathString[^1]))
                lengthAbsolutePathString--;
        }
        
        int position = 0;
        int parentDirectoryEndExclusiveIndex = -1;
    
        while (position < lengthAbsolutePathString)
        {
            char currentCharacter = absolutePathString[position++];
    
            if (fileSystemProvider.IsDirectorySeparator(currentCharacter))
            {
                formattedBuilder.Append(fileSystemProvider.DirectorySeparatorChar);
                
                tokenBuilder.Clear();
                
                parentDirectoryEndExclusiveIndex = formattedBuilder.Length;
                
                if (ancestorDirectoryList is not null)
                {
                    ancestorDirectoryList.Add(formattedBuilder.ToString());
                }
            }
            else if (currentCharacter == ':' && !seenRootDrive)
            {
                // Take all files from the drive the app is executed from
                // TODO: Look into multi drive scenarios.
                //
                seenRootDrive = true;
                tokenBuilder.Clear();
                formattedBuilder.Clear();
            }
            else
            {
                formattedBuilder.Append(currentCharacter);
                tokenBuilder.Append(currentCharacter);
            }
        }

        if (IsDirectory)
        {
            // Directories get the ending '/' stripped prior to processing them
            if (nameKind == AbsolutePathNameKind.NameNoExtension)
                Name = tokenBuilder.ToString();
            else if (nameKind == AbsolutePathNameKind.NameWithExtension)
                Name = tokenBuilder.Append(fileSystemProvider.DirectorySeparatorChar).ToString();
            else
                Name = fileSystemProvider.DirectorySeparatorCharToStringResult;
        }
        else
        {
            if (nameKind == AbsolutePathNameKind.NameWithExtension)
            {
                Name = tokenBuilder.ToString();
            }
            else
            {
                string extensionNoPeriod;
                string nameNoExtension;
            
                var fileNameAmbiguous = tokenBuilder.ToString();
                var splitFileNameAmbiguous = fileNameAmbiguous.Split('.');
        
                if (splitFileNameAmbiguous.Length == 2)
                {
                    nameNoExtension = splitFileNameAmbiguous[0];
                    extensionNoPeriod = splitFileNameAmbiguous[1];
                }
                else if (splitFileNameAmbiguous.Length == 1)
                {
                    nameNoExtension = splitFileNameAmbiguous[0];
                    extensionNoPeriod = string.Empty;
                }
                else
                {
                    tokenBuilder.Clear();
                
                    foreach (var split in splitFileNameAmbiguous.SkipLast(1))
                    {
                        tokenBuilder.Append(split);
                        tokenBuilder.Append(".");
                    }
        
                    tokenBuilder.Remove(tokenBuilder.Length - 1, 1);

                    nameNoExtension = tokenBuilder.ToString();
                    extensionNoPeriod = splitFileNameAmbiguous.Last();
                }
                
                if (nameKind == AbsolutePathNameKind.NameNoExtension)
                    Name = nameNoExtension;
                else if (nameKind == AbsolutePathNameKind.ExtensionNoPeriod)
                    Name = extensionNoPeriod;
            }
        }

        if (IsDirectory)
        {
            formattedBuilder.Append(fileSystemProvider.DirectorySeparatorChar);
        }

        var formattedString = absolutePathString;
        if (absolutePathString.Length == formattedBuilder.Length)
        {
            for (int i = 0; i < absolutePathString.Length; i++)
            {
                if (absolutePathString[i] != formattedBuilder[i])
                {
                    formattedString = formattedBuilder.ToString();
                    break;
                }
            }
        }
        else
        {
            formattedString = formattedBuilder.ToString();
        }

        // This is working for files, but directories if they differ by terminating directory separator
        // then a string allocation occurs which feels quite unnecessary...
        //
        /*if (formattedString == absolutePathString)
        {
            Console.WriteLine("eq");
        }
        else
        {
            Console.WriteLine($"{formattedString} == {absolutePathString}");
        }*/

        tokenBuilder.Clear();
        formattedBuilder.Clear();
    
        if (formattedString.Length == 2)
        {
            // If two directory separators chars are one after another and that is the only text in the string.
            if ((formattedString[0] == fileSystemProvider.DirectorySeparatorChar && formattedString[1] == fileSystemProvider.DirectorySeparatorChar) ||
                (formattedString[0] == fileSystemProvider.AltDirectorySeparatorChar && formattedString[1] == fileSystemProvider.AltDirectorySeparatorChar))
            {
                Value = fileSystemProvider.DirectorySeparatorCharToStringResult;
                return;
            }
        }
    
        ParentDirectoryEndExclusiveIndex = parentDirectoryEndExclusiveIndex;
        Value = formattedString;
    }

    public static string GetFormattedStringOnly(
        string absolutePathString,
        bool isDirectory,
        IFileSystemProvider fileSystemProvider,
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder)
    {
        bool seenRootDrive = false;

        var lengthAbsolutePathString = absolutePathString.Length;

        if (isDirectory && lengthAbsolutePathString > 1)
        {
            // Strip the last character if this is a directory, where the exact input ended in a directory separator char.
            // Reasoning: This standardizes what a directory looks like within the scope of this method.
            //
            if (fileSystemProvider.IsDirectorySeparator(absolutePathString[^1]))
                lengthAbsolutePathString--;
        }

        int position = 0;
        int parentDirectoryEndExclusiveIndex = -1;

        while (position < lengthAbsolutePathString)
        {
            char currentCharacter = absolutePathString[position++];

            if (fileSystemProvider.IsDirectorySeparator(currentCharacter))
            {
                formattedBuilder.Append(fileSystemProvider.DirectorySeparatorChar);

                tokenBuilder.Clear();

                parentDirectoryEndExclusiveIndex = formattedBuilder.Length;
            }
            else if (currentCharacter == ':' && !seenRootDrive)
            {
                // Take all files from the drive the app is executed from
                // TODO: Look into multi drive scenarios.
                //
                seenRootDrive = true;
                tokenBuilder.Clear();
                formattedBuilder.Clear();
            }
            else
            {
                formattedBuilder.Append(currentCharacter);
                tokenBuilder.Append(currentCharacter);
            }
        }

        if (isDirectory)
        {
            formattedBuilder.Append(fileSystemProvider.DirectorySeparatorChar);
        }

        var formattedString = absolutePathString;
        if (absolutePathString.Length == formattedBuilder.Length)
        {
            for (int i = 0; i < absolutePathString.Length; i++)
            {
                if (absolutePathString[i] != formattedBuilder[i])
                {
                    formattedString = formattedBuilder.ToString();
                    break;
                }
            }
        }
        else
        {
            formattedString = formattedBuilder.ToString();
        }

        // This is working for files, but directories if they differ by terminating directory separator
        // then a string allocation occurs which feels quite unnecessary...
        //
        /*if (formattedString == absolutePathString)
        {
            Console.WriteLine("eq");
        }
        else
        {
            Console.WriteLine($"{formattedString} == {absolutePathString}");
        }*/

        tokenBuilder.Clear();
        formattedBuilder.Clear();

        if (formattedString.Length == 2)
        {
            // If two directory separators chars are one after another and that is the only text in the string.
            if ((formattedString[0] == fileSystemProvider.DirectorySeparatorChar && formattedString[1] == fileSystemProvider.DirectorySeparatorChar) ||
                (formattedString[0] == fileSystemProvider.AltDirectorySeparatorChar && formattedString[1] == fileSystemProvider.AltDirectorySeparatorChar))
            {
                return fileSystemProvider.DirectorySeparatorCharToStringResult;
            }
        }

        return formattedString;
    }

    // public string? ParentDirectory { get; private set; }
    public bool IsDirectory { get; private set; }
    /// <summary>
    /// The <see cref="NameNoExtension"/> for a directory does NOT end with a directory separator char.
    /// </summary>
    public string Name { get; private set; }
    public AbsolutePathNameKind NameKind { get; set; }

    /// <summary>
    /// TODO: If it is discovered that the provided absolute path is formatted as the app likes...
    /// ...then don't ToString() the 'formattedBuilder'.
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// If this property is NOT == -1;
    /// Then the parent directory's string can be calculated with: Value[..ParentDirectoryEndExclusiveIndex];
    /// </summary>
    public int ParentDirectoryEndExclusiveIndex { get; }
    
    public bool IsRootDirectory => ParentDirectoryEndExclusiveIndex == -1;

    public string? CreateSubstringParentDirectory()
    {
        if (ParentDirectoryEndExclusiveIndex == -1)
            return null;
        
        return Value[..ParentDirectoryEndExclusiveIndex];
    }

    /// <summary>
    ///  If providing tokenBuilder or formattedBuilder, ensure they are '.Clear()'ed prior to invoking.
    /// Prior to returning, tokenBuilder and formattedBuilder will be '.Clear()'ed.
    /// 
    /// The invoker of this method must choose between "myFile.txt" or "myFile" as the resulting 'Name' property.
    /// This is done with the `shouldNameContainsExtension` argument,
    /// and this argument gets stored on the instance as the `NameContainsExtension` property.
    /// </summary>
    public List<string> GetAncestorDirectoryList(
        IFileSystemProvider fileSystemProvider,
        StringBuilder tokenBuilder,
        StringBuilder formattedBuilder,
        AbsolutePathNameKind nameKind)
    {
        var ancestorDirectoryList = new List<string>();
        
        _ = new AbsolutePath(
            Value,
            IsDirectory,
            fileSystemProvider,
            tokenBuilder,
            formattedBuilder,
            nameKind,
            ancestorDirectoryList: ancestorDirectoryList);
    
        return ancestorDirectoryList;
    }
}
