using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;

namespace HelloWorld;

public static partial class BrighterRegistrations
{
    [BrighterRegistrations]
    public static partial IBrighterBuilder AddFromThisAssembly(this IBrighterBuilder builder);
}
