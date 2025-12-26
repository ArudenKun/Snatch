using Snatch.Core;
using Snatch.Utilities;
using Volo.Abp.DependencyInjection;

namespace Snatch.Options;

public sealed partial class DataService : JsonFileBase, ISingletonDependency
{
    public DataService()
        : base(AppHelper.DataPath) { }
}
