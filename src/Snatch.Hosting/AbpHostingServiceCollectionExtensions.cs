using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Snatch.Hosting;

public static class AbpHostingServiceCollectionExtensions
{
    public static IHostEnvironment GetHostingEnvironment(this IServiceCollection services)
    {
        var hostingEnvironment = services.GetSingletonInstanceOrNull<IHostEnvironment>();
        return hostingEnvironment
            ?? new EmptyHostingEnvironment { EnvironmentName = Environments.Development };
    }
}
