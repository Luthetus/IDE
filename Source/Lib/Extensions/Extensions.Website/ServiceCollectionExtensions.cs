using Microsoft.Extensions.DependencyInjection;
using Clair.Common.RazorLib.Installations.Models;
using Clair.Ide.RazorLib.Installations.Models;
using Clair.Extensions.Config.Installations.Models;

namespace Clair.Website.RazorLib;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClairWebsiteServices(
        this IServiceCollection services,
        ClairHostingInformation hostingInformation)
    {
        services.AddClairIdeRazorLibServices(hostingInformation);
        services.AddClairConfigServices(hostingInformation);
        
        return services;
    }
}
