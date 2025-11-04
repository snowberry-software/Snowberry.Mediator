using System.Diagnostics.CodeAnalysis;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.DependencyInjection.Abstractions.Interfaces;
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
    /// <remarks>
    /// A service provider must be registered in the service collection before calling this method.
    /// Alternatively, if the <see cref="IServiceRegistry"/> instance is also an <see cref="IServiceProvider"/>, it will be registered as a singleton.
    /// </remarks>
    /// <param name="serviceRegistry">The service collection.</param>
    /// <param name="configure">The configuration method.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The service collection.</returns>
    [RequiresDynamicCode("This method uses reflection to find types from the asemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the asemblies defined in the options.")]
    public static IServiceRegistry AddSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        return AddSnowberryMediator(serviceRegistry, configure, serviceLifetime, append: false);
    }

    /// <summary>
    /// Appends the Mediator services to the specified <see cref="IServiceRegistry" /> and preserves existing registrations.
    /// </summary>
    /// <remarks>
    /// A service provider must be registered in the service collection before calling this method.
    /// Alternatively, if the <see cref="IServiceRegistry"/> instance is also an <see cref="IServiceProvider"/>, it will be registered as a singleton.
    /// </remarks>
    /// <param name="serviceRegistry">The service collection.</param>
    /// <param name="configure">The configuration method.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The service collection.</returns>
    [RequiresDynamicCode("This method uses reflection to find types from the asemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the asemblies defined in the options.")]
    public static IServiceRegistry AppendSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        return AddSnowberryMediator(serviceRegistry, configure, serviceLifetime, append: true);
    }

    /// <summary>
    /// Adds the Mediator services to the specified <see cref="IServiceRegistry" />.
    /// </summary>
    /// <remarks>
    /// A service provider must be registered in the service collection before calling this method.
    /// Alternatively, if the <see cref="IServiceRegistry"/> instance is also an <see cref="IServiceProvider"/>, it will be registered as a singleton.
    /// </remarks>
    /// <param name="serviceRegistry">The service collection.</param>
    /// <param name="configure">The configuration method.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <param name="append">Whether to append the registrations to existing ones.</param>
    /// <returns>The service collection.</returns>
    [RequiresDynamicCode("This method uses reflection to find types from the asemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the asemblies defined in the options.")]
    private static IServiceRegistry AddSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime, bool append)
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
        }, append: append);

        //if (!serviceRegistry.IsServiceRegistered<IServiceProvider>(serviceKey: null))
        //{
        //    if (serviceRegistry is IServiceProvider serviceProvider)
        //        serviceRegistry.RegisterSingleton(instance: serviceProvider);
        //    else
        //        throw new InvalidOperationException($"The {nameof(IServiceRegistry)} instance is not an {nameof(IServiceProvider)}. Please register an {nameof(IServiceProvider)} instance before calling {nameof(AddSnowberryMediator)}.");
        //}

        return serviceRegistry;
    }
}
