using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snowberry.Mediator.DependencyInjection.Shared;
using Snowberry.Mediator.DependencyInjection.Shared.Contracts;

namespace Snowberry.Mediator.Extensions.DependencyInjection;

/// <summary>
/// An implementation of <see cref="IServiceContext"/> that uses <see cref="IServiceCollection"/> to register services.
/// </summary>
/// <param name="serviceCollection">The service collection.</param>
public class MicrosoftServiceContext(IServiceCollection serviceCollection) : IServiceContext
{
    private readonly IServiceCollection _serviceCollection = serviceCollection;

    /// <summary>Creates an <see cref="IServiceContext"/> over the given <see cref="IServiceCollection"/>.</summary>
    /// <remarks>Used by the Snowberry.Mediator source generator to build the registration context.</remarks>
    /// <param name="serviceCollection">The service collection.</param>
    /// <returns>The service context.</returns>
    public static IServiceContext Create(IServiceCollection serviceCollection) => new MicrosoftServiceContext(serviceCollection);

    /// <inheritdoc/>
    public bool IsServiceRegistered<T>()
    {
        return _serviceCollection.Any(sd => sd.ServiceType == typeof(T));
    }

    /// <inheritdoc/>
    public void TryRegister(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type implementationType, RegistrationServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime switch
        {
            RegistrationServiceLifetime.Singleton => ServiceLifetime.Singleton,
            RegistrationServiceLifetime.Scoped => ServiceLifetime.Scoped,
            RegistrationServiceLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new NotSupportedException($"The service lifetime '{lifetime}' is not supported."),
        });

        _serviceCollection.TryAdd(descriptor);
    }

    /// <inheritdoc/>
    public void TryRegister([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type serviceType, object instance)
    {
        var descriptor = new ServiceDescriptor(serviceType, instance: instance);
        _serviceCollection.TryAdd(descriptor);
    }

    /// <inheritdoc/>
    public T? TryToGetSingleton<T>(out bool found)
    {
        found = false;
        object? instance = _serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(T) && sd.Lifetime == ServiceLifetime.Singleton)?.ImplementationInstance;

        if (instance is T typed)
        {
            found = true;
            return typed;
        }

        return default;
    }
}