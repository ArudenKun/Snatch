using Microsoft.Extensions.DependencyInjection;

namespace Snatch.Dependency;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class DependencyAttribute : Attribute
{
    public DependencyAttribute()
        : this(ServiceLifetime.Transient) { }

    public DependencyAttribute(ServiceLifetime serviceLifetime)
    {
        Lifetime = serviceLifetime;
    }

    public ServiceLifetime Lifetime { get; }
}
