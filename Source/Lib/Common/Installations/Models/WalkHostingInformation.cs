namespace Clair.Common.RazorLib.Installations.Models;

/// <summary>
/// One use case for <see cref="ClairHostingInformation"/> would be service registration.<br/><br/>
/// If one uses <see cref="ClairHostingKind.ServerSide"/>, then 
/// services.AddHostedService&lt;TService&gt;(...); will be invoked.<br/><br/>
/// Whereas, if one uses <see cref="ClairHostingKind.Wasm"/> then 
/// services.AddSingleton&lt;TService&gt;(...); will be used.
/// Then after the initial render, a Task will be 'fire and forget' invoked to start the service.
/// </summary>
/// <remarks>
/// This class is an exception to the naming convention, "don't use the word 'Clair' in class names".
/// 
/// Reason for this exception: when one first starts interacting with this project,
///     this type might be one of the first types they interact with. So, the redundancy of namespace
///     and type containing 'Clair' feels reasonable here.
/// </remarks>
public record struct ClairHostingInformation
{
    public ClairHostingInformation(
        ClairHostingKind clairHostingKind,
        ClairPurposeKind clairPurposeKind)
    {
        ClairHostingKind = clairHostingKind;
        ClairPurposeKind = clairPurposeKind;
    }

    public ClairHostingKind ClairHostingKind { get; init; }
    public ClairPurposeKind ClairPurposeKind { get; init; }
}
