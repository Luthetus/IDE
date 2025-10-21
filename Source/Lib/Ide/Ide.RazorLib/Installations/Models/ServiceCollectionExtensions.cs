using Microsoft.Extensions.DependencyInjection;
using Clair.Common.RazorLib.Installations.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Installations.Models;
using Clair.Ide.RazorLib.AppDatas.Models;

namespace Clair.Ide.RazorLib.Installations.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClairIdeRazorLibServices(
        this IServiceCollection services,
        ClairHostingInformation hostingInformation)
    {
        services.AddClairTextEditor(hostingInformation);
    
        if (hostingInformation.ClairHostingKind == ClairHostingKind.Photino)
            services.AddScoped<IAppDataService, NativeAppDataService>();
        else
            services.AddScoped<IAppDataService, DoNothingAppDataService>();

        services
            .AddScoped<IdeService>(sp =>
            {
                return new IdeService(
                    sp.GetRequiredService<TextEditorService>(),
                    sp);
            });

        return services;
    }
}
