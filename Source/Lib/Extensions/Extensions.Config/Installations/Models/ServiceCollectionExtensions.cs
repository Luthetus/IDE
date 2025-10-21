using Microsoft.Extensions.DependencyInjection;
using Clair.Common.RazorLib.Installations.Models;
using Clair.Extensions.DotNet.Installations.Models;

namespace Clair.Extensions.Config.Installations.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClairConfigServices(
        this IServiceCollection services,
        ClairHostingInformation hostingInformation)
    {
        return services
            .AddClairExtensionsDotNetServices(hostingInformation);
    }
}
