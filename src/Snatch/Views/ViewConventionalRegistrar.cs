using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Snatch.Views;

public class ViewConventionalRegistrar : DefaultConventionalRegistrar
{
    protected override bool IsConventionalRegistrationDisabled(Type type) =>
        !type.GetInterfaces()
            .AsValueEnumerable()
            .Any(x => x.IsGenericType && typeof(IView<>) == x.GetGenericTypeDefinition())
        || base.IsConventionalRegistrationDisabled(type);

    protected override List<Type> GetExposedServiceTypes(Type type)
    {
        var exposedServiceTypes = base.GetExposedServiceTypes(type).AsValueEnumerable();
        var viewInterfaces = type.GetInterfaces()
            .AsValueEnumerable()
            .Where(x => x.IsGenericType && typeof(IView<>) == x.GetGenericTypeDefinition());
        return exposedServiceTypes.Union(viewInterfaces).Distinct().ToList();
    }
}
