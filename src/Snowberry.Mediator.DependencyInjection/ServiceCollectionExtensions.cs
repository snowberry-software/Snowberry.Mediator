using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Interfaces;
using Snowberry.Mediator.DependencyInjection.Shared;

namespace Snowberry.Mediator.DependencyInjection;

/// <summary>
/// Extension methods for the <see cref="IServiceRegistry"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mediator services to the specified <see cref="IServiceRegistry" />.
    /// </summary>
    /// <param name="serviceRegistry">The service collection.</param>
    /// <param name="configure">The configuration method.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The service collection.</returns>
    public static IServiceRegistry AddSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var options = new MediatorOptions();
        configure(options);

        var serviceContext = new SnowberryServiceContext(serviceRegistry);
        DependencyInjectionHelper.AddSnowberryMediator(serviceContext, options, serviceLifetime: serviceLifetime switch
        {
            ServiceLifetime.Scoped => RegistrationServiceLifetime.Scoped,
            ServiceLifetime.Singleton => RegistrationServiceLifetime.Singleton,
            ServiceLifetime.Transient => RegistrationServiceLifetime.Transient,
            _ => throw new NotSupportedException($"The service lifetime '{serviceLifetime}' is not supported."),
        });

        if (!serviceRegistry.IsServiceRegistered<IServiceProvider>(serviceKey: null))
        {
            if (serviceRegistry is IServiceProvider serviceProvider)
                serviceRegistry.RegisterSingleton(instance: serviceProvider);
            else
                throw new InvalidOperationException($"The {nameof(IServiceRegistry)} instance is not an {nameof(IServiceProvider)}. Please register an {nameof(IServiceProvider)} instance before calling {nameof(AddSnowberryMediator)}.");
        }

        return serviceRegistry;
    }
}
