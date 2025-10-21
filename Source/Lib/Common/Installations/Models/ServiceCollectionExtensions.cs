using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Clair.Common.RazorLib.Dimensions.Models;

namespace Clair.Common.RazorLib.Installations.Models;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The <see cref="configure"/> parameter provides an instance of a record type.
    /// Use the 'with' keyword to change properties and then return the new instance.
    /// </summary>
    public static IServiceCollection AddClairCommonServices(
        this IServiceCollection services,
        ClairHostingInformation hostingInformation)
    {
        var commonConfig = new ClairCommonConfig();

        services
            .AddScoped<BrowserResizeInterop>()
            .AddScoped<CommonService, CommonService>(sp =>
            {
                var commonService = new CommonService(
                    hostingInformation,
                    commonConfig,
                    sp.GetRequiredService<IJSRuntime>());
            
                return commonService;
            });
        
        return services;
    }
}
