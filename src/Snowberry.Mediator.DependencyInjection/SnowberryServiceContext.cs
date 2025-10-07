using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.DependencyInjection.Abstractions.Implementation;
using Snowberry.DependencyInjection.Abstractions.Interfaces;
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
    public void TryRegister(Type serviceType, Type implementationType, RegistrationServiceLifetime lifetime)
    {
        _serviceRegistry.TryRegister(new ServiceDescriptor(serviceType, implementationType, lifetime switch
        {
            RegistrationServiceLifetime.Singleton => Snowberry.DependencyInjection.Abstractions.ServiceLifetime.Singleton,
            RegistrationServiceLifetime.Scoped => Snowberry.DependencyInjection.Abstractions.ServiceLifetime.Scoped,
            RegistrationServiceLifetime.Transient => Snowberry.DependencyInjection.Abstractions.ServiceLifetime.Transient,
            _ => throw new NotSupportedException($"The service lifetime '{lifetime}' is not supported."),
        }));
    }

    /// <inheritdoc/>
    public void TryRegister(Type serviceType, object instance)
    {
        _serviceRegistry.TryRegister(ServiceDescriptor.Singleton(serviceType, serviceType, singletonInstance: instance));
    }

    /// <inheritdoc/>
    public T? TryToGetSingleton<T>(out bool found)
    {
        found = false;
        if (_serviceRegistry is not IKeyedServiceProvider serviceFactory)
            return default;

        var instance = serviceFactory.GetService<T>();
        found = instance is not null;
        return instance;
    }
}
