using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.Xml;

public sealed class XmlResource : CompilerServiceResource
{
    public XmlResource(ResourceUri resourceUri, XmlCompilerService xmlCompilerService)
        : base(resourceUri, xmlCompilerService)
    {
    }
}
