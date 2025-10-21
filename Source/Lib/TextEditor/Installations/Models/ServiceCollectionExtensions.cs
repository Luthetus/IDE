using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Clair.Common.RazorLib;
using Clair.Common.RazorLib.Installations.Models;

namespace Clair.TextEditor.RazorLib.Installations.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClairTextEditor(
        this IServiceCollection services,
        ClairHostingInformation hostingInformation)
    {
        services.AddClairCommonServices(hostingInformation);

        services
            .AddScoped<TextEditorService>(sp =>
            {
                return new TextEditorService(
                    sp.GetRequiredService<IJSRuntime>(),
                    sp.GetRequiredService<CommonService>());
            });
        
        return services;
    }
}
