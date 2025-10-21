using Clair.Common.RazorLib;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.Icons.Displays;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Ide.RazorLib.Clipboards.Models;
using Clair.Ide.RazorLib.Terminals.Models;
using Clair.TextEditor.RazorLib.Decorations.Models;

namespace Clair.Ide.RazorLib;

public static class IdeFacts
{
    /* Start TerminalFacts */
    public static readonly Key<ITerminal> EXECUTION_KEY = Key<ITerminal>.NewKey();
    public static readonly Key<ITerminal> GENERAL_KEY = Key<ITerminal>.NewKey();
    /* End TerminalFacts */

    /* Start TerminalOutputFacts */
    public const int MAX_COMMAND_COUNT = 100;
    public const int MAX_OUTPUT_LENGTH = 100_000;

    /// <summary>MAX_OUTPUT_LENGTH / 2 + 1; so that two terminal commands can sum and cause a clear</summary>
    public const int OUTPUT_LENGTH_PADDING = 50_001;
    /* End TerminalOutputFacts */

    /* Start TerminalPresentationFacts */
    public const string Terminal_CssClassString = "ci_te_terminal-presentation";

    public static readonly Key<TextEditorPresentationModel> Terminal_PresentationKey = Key<TextEditorPresentationModel>.NewKey();

    public static readonly TextEditorPresentationModel Terminal_EmptyPresentationModel = new(
        Terminal_PresentationKey,
        0,
        Terminal_CssClassString,
        new GenericDecorationMapper());
    /* End TerminalPresentationFacts */

    /* Start TerminalWebsiteFacts */
    public const string TargetFileNames_DOT_NET = "dotnet";
    public const string InitialArguments_RUN = "run";
    public const string Options_PROJECT = "--project";
    /* End TerminalWebsiteFacts */

    /* Start HiddenFileFacts */
    public const string BIN = "bin";
    public const string OBJ = "obj";

    /// <summary>
    /// If rendering a .csproj file pass in <see cref="ExtensionNoPeriodFacts.C_SHARP_PROJECT"/>
    ///
    /// Then perhaps the returning array would contain { "bin", "obj" } as they should be hidden
    /// with this context.
    /// </summary>
    /// <returns></returns>
    public static bool IsHiddenFileByContainerFileExtension(string extensionNoPeriod, string absolutePathString)
    {
        switch (extensionNoPeriod)
        {
            case CommonFacts.C_SHARP_PROJECT:
                return absolutePathString.EndsWith(BIN) || absolutePathString.EndsWith(OBJ);
            default:
                return false;
        }
    }
    /* End HiddenFileFacts */

    /* Start UniqueFileFacts */
    public const string Properties = "Properties";
    public const string WwwRoot = "wwwroot";

    /// <summary>
    /// If rendering a .csproj file pass in <see cref="ExtensionNoPeriodFacts.C_SHARP_PROJECT"/>
    ///
    /// Then perhaps the returning array would contain { "Properties", "wwwroot" } as they are unique files
    /// with this context.
    /// </summary>
    /// <returns></returns>
    public static bool IsUniqueFileByContainerFileExtension(string extensionNoPeriod, string name)
    {
        switch (extensionNoPeriod)
        {
            case CommonFacts.C_SHARP_PROJECT:
                return name == Properties || name == WwwRoot;
            default:
                return false;
        }
    }
    /* End UniqueFileFacts */

    /* Start ClipboardFacts */
    /// <summary>
    /// Indicates the start of a phrase.<br/><br/>
    /// Phrase is being defined as a tag, command, datatype and value in string form.<br/><br/>
    /// </summary>
    public const string Tag = "`'\";ci_clipboard";
    /// <summary>Deliminates tag_command_datatype_value</summary>
    public const string FieldDelimiter = "_";

    // Commands
    public const string CopyCommand = "copy";
    public const string CutCommand = "cut";
    // DataTypes
    public const string AbsolutePathDataType = "absolute-file-path";

    public static string FormatPhrase(string command, string dataType, string value)
    {
        return Tag + FieldDelimiter + command + FieldDelimiter + dataType + FieldDelimiter + value;
    }

    public static bool TryParseString(string clipboardContents, out ClipboardPhrase? clipboardPhrase)
    {
        clipboardPhrase = null;

        if (clipboardContents.StartsWith(Tag))
        {
            // Skip Tag
            clipboardContents = clipboardContents[Tag.Length..];
            // Skip Delimiter following the Tag
            clipboardContents = clipboardContents[FieldDelimiter.Length..];

            var nextDelimiter = clipboardContents.IndexOf(FieldDelimiter, StringComparison.Ordinal);

            // Take Command
            var command = clipboardContents[..nextDelimiter];

            clipboardContents = clipboardContents[(nextDelimiter + 1)..];

            nextDelimiter = clipboardContents.IndexOf(FieldDelimiter, StringComparison.Ordinal);

            // Take DataType
            var dataType = clipboardContents[..nextDelimiter];

            // Value is whatever remains in the string
            var value = clipboardContents[(nextDelimiter + 1)..];

            clipboardPhrase = new ClipboardPhrase(command, dataType, value);

            return true;
        }

        return false;
    }
    /* End ClipboardFacts */

    /// <summary>
    /// The file extension is not known so this bunch of branches is here.
    /// The aim is to avoid an outlier "worst case scenario" at the very least
    /// by using a switch to reduce the maximum amount of if statements that need to be checked.
    /// </summary>
    public static IconKind GetIconKind(AbsolutePath absolutePath)
    {
        if (absolutePath.IsDirectory)
        {
            if (absolutePath.Name == IdeFacts.Properties)
            {
                return IconKind.Properties;
            }
            else if (absolutePath.Name == IdeFacts.WwwRoot)
            {
                return IconKind.WwwRoot;
            }
            else
            {
                return IconKind.Folder;
            }
        }
        else
        {
            if (absolutePath.Name.Length > 0)
            {
                switch (absolutePath.Name[^1])
                {
                    case 's':
                    {
                        if (absolutePath.Name.EndsWith(CommonFacts.C_SHARP_CLASS))
                        {
                            return IconKind.CSharpClass;
                        }
                        else if (absolutePath.Name.EndsWith(CommonFacts.CSS))
                        {
                            return IconKind.Css;
                        }
                        else if (absolutePath.Name.EndsWith(CommonFacts.JAVA_SCRIPT))
                        {
                            return IconKind.Js;
                        }
                        else if (absolutePath.Name.EndsWith(CommonFacts.TYPE_SCRIPT))
                        {
                            return IconKind.Ts;
                        }

                        break;
                    }
                    case 'r':
                    {
                        return IconKind.Razor;
                    }
                    case 'l':
                        return IconKind.Cshtml;
                    case 'j':
                        return IconKind.CSharpProject;
                    case 'n':
                        if (absolutePath.Name.EndsWith(CommonFacts.JSON))
                        {
                            return IconKind.Json;
                        }
                        else if (absolutePath.Name.EndsWith(CommonFacts.DOT_NET_SOLUTION))
                        {
                            return IconKind.DotNetSolution;
                        }
                        break;
                    case 'x':
                        return IconKind.DotNetSolution;
                }
            }

            return IconKind.File;
        }
    }
}
