using Snatch.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

[Dependency(ReplaceServices = true)]
public class ConnectionStringResolver : IConnectionStringResolver, ITransientDependency
{
    private readonly GeneralOptions _generalOptions;

    public ConnectionStringResolver(GeneralOptions generalOptions)
    {
        _generalOptions = generalOptions;
    }

    public string Resolve(string? connectionStringName = null)
    {
        return _generalOptions.ConnectionStrings.Default
            ?? throw new NullReferenceException("ConnectionString not configured");
    }

    public Task<string> ResolveAsync(string? connectionStringName = null)
    {
        return Task.FromResult(Resolve());
    }
}
