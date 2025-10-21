using Clair.Common.RazorLib.Exceptions;

namespace Clair.Ide.RazorLib.Exceptions;

/// <remarks>
/// This class is an exception to the naming convention, "don't use the word 'Clair' in class names".
/// 
/// Reason for this exception: the 'Exception' datatype is far more common in code,
///     than some specific type (example: DialogDisplay.razor).
///     So, adding 'Clair' in the class name for redundancy seems meaningful here.
/// </remarks>
public class ClairIdeException : ClairException
{
    public ClairIdeException(string? message = null, Exception? innerException = null)
        : base(message, innerException)
    {

    }
}
