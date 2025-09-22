using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snowberry.Mediator.DependencyInjection.Shared;

namespace Snowberry.Mediator.Extensions.DependencyInjection;

/// <summary>
/// An implementation of <see cref="IServiceContext"/> that uses <see cref="IServiceCollection"/> to register services.
/// </summary>
/// <param name="serviceCollection">The service collection.</param>
internal class MicrosoftServiceContext(IServiceCollection serviceCollection) : IServiceContext
{
    private readonly IServiceCollection _serviceCollection = serviceCollection;

    /// <inheritdoc/>
    public void Register(Type serviceType, Type implementationType, RegistrationServiceLifetime lifetime)
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
    public void Register(Type serviceType, object instance)
    {
        var descriptor = new ServiceDescriptor(serviceType, instance: instance);
        _serviceCollection.TryAdd(descriptor);
    }
}
