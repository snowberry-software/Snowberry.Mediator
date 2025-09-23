using Snowberry.DependencyInjection.Interfaces;
using Snowberry.Mediator.DependencyInjection.Shared;
using Snowberry.Mediator.DependencyInjection.Shared.Contracts;

namespace Snowberry.Mediator.DependencyInjection;

/// <summary>
/// An implementation of <see cref="IServiceContext"/> that uses <see cref="IServiceRegistry"/> to register services.
/// </summary>
/// <param name="serviceRegistry">The service registry.</param>
internal class SnowberryServiceContext(IServiceRegistry serviceRegistry) : IServiceContext
{
    private readonly IServiceRegistry _serviceRegistry = serviceRegistry;

    /// <inheritdoc/>
    public bool IsServiceRegistered<T>()
    {
        return _serviceRegistry.IsServiceRegistered<T>(serviceKey: null);
    }

    /// <inheritdoc/>
    public void Register(Type serviceType, Type implementationType, RegistrationServiceLifetime lifetime)
    {
        _serviceRegistry.Register(serviceType: serviceType, implementationType: implementationType, serviceKey: null, lifetime: lifetime switch
        {
            RegistrationServiceLifetime.Singleton => Snowberry.DependencyInjection.ServiceLifetime.Singleton,
            RegistrationServiceLifetime.Scoped => Snowberry.DependencyInjection.ServiceLifetime.Scoped,
            RegistrationServiceLifetime.Transient => Snowberry.DependencyInjection.ServiceLifetime.Transient,
            _ => throw new NotSupportedException($"The service lifetime '{lifetime}' is not supported."),
        }, singletonInstance: null);
    }

    /// <inheritdoc/>
    public void Register(Type serviceType, object instance)
    {
        _serviceRegistry.Register(
            serviceType: serviceType,
            implementationType: serviceType,
            serviceKey: null,
            lifetime: Snowberry.DependencyInjection.ServiceLifetime.Singleton,
            singletonInstance: instance);
    }

    /// <inheritdoc/>
    public T? TryToGetSingleton<T>(out bool found)
    {
        found = false;
        if (_serviceRegistry is not IKeyedServiceProvider serviceFactory)
            return default;

        var instance = serviceFactory.GetOptionalKeyedService<T>(serviceKey: null);
        found = instance is not null;
        return instance;
    }
}
