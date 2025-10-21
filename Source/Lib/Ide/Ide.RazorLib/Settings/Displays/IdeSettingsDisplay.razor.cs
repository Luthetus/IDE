using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib;
using Clair.Ide.RazorLib.AppDatas.Models;

namespace Clair.Ide.RazorLib.Settings.Displays;

public partial class IdeSettingsDisplay : ComponentBase
{
    [Inject]
    private IAppDataService AppDataService { get; set; } = null!;
    [Inject]
    private CommonService CommonService { get; set; } = null!;
    
    private void WriteClairDebugSomethingToConsole()
    {
        Console.WriteLine(CommonFacts.CreateText());
        /*
        #if DEBUG
        Console.WriteLine(ClairDebugSomething.CreateText());
        #else
        Console.WriteLine($"Must run in debug mode to see {nameof(WriteClairDebugSomethingToConsole)}");
        #endif
        */
    }
}
