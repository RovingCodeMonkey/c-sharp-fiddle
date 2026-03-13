using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    // Call this once during application startup
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new Exception("ServiceLocator not initialized!");

        return _serviceProvider.GetRequiredService<T>();
    }
}
