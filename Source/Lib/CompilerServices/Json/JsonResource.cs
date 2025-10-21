using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Lexers.Models;

namespace Clair.CompilerServices.Json;

public class JsonResource : CompilerServiceResource
{
    public JsonResource(ResourceUri resourceUri, JsonCompilerService jsonCompilerService)
        : base(resourceUri, jsonCompilerService)
    {
    }
}
