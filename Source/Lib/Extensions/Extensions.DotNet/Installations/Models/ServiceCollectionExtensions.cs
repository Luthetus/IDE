using Microsoft.Extensions.DependencyInjection;
using Clair.Common.RazorLib.Installations.Models;
using Clair.Ide.RazorLib;
using Clair.Ide.RazorLib.AppDatas.Models;

namespace Clair.Extensions.DotNet.Installations.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClairExtensionsDotNetServices(
        this IServiceCollection services,
        ClairHostingInformation hostingInformation)
    {
        return services
            .AddScoped<DotNetService>(sp =>
            {
                return new DotNetService(
                    sp.GetRequiredService<IdeService>(),
                    sp.GetRequiredService<HttpClient>(),
                    sp.GetRequiredService<IAppDataService>(),
                    sp);
            });
    }
}
