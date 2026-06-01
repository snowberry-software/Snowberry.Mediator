using System.Diagnostics.CodeAnalysis;
using Snowberry.DependencyInjection.Abstractions;
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
    /// The <see cref="IServiceRegistry"/> must be resolvable as an <see cref="IServiceProvider"/> so the mediator
    /// can resolve handlers at dispatch time.
    /// </remarks>
    /// <param name="serviceRegistry">The service registry to add the registrations to.</param>
    /// <param name="configure">A callback used to configure the <see cref="MediatorOptions"/>.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The supplied <paramref name="serviceRegistry"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceRegistry"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The mediator is already registered on <paramref name="serviceRegistry"/>; use <see cref="AppendSnowberryMediator"/> to extend it.</exception>
    [RequiresDynamicCode("This method uses reflection to find types from the assemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the assemblies defined in the options.")]
    public static IServiceRegistry AddSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        return AddSnowberryMediator(serviceRegistry, configure, serviceLifetime, append: false);
    }

    /// <summary>
    /// Appends the Mediator services to the specified <see cref="IServiceRegistry" /> and preserves existing registrations.
    /// </summary>
    /// <remarks>
    /// The <see cref="IServiceRegistry"/> must be resolvable as an <see cref="IServiceProvider"/> so the mediator
    /// can resolve handlers at dispatch time.
    /// </remarks>
    /// <param name="serviceRegistry">The service registry to add the registrations to.</param>
    /// <param name="configure">A callback used to configure the <see cref="MediatorOptions"/>.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The supplied <paramref name="serviceRegistry"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceRegistry"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    [RequiresDynamicCode("This method uses reflection to find types from the assemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the assemblies defined in the options.")]
    public static IServiceRegistry AppendSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        return AddSnowberryMediator(serviceRegistry, configure, serviceLifetime, append: true);
    }

    /// <summary>
    /// Adds the Mediator services to the specified <see cref="IServiceRegistry" />.
    /// </summary>
    /// <remarks>
    /// The <see cref="IServiceRegistry"/> must be resolvable as an <see cref="IServiceProvider"/> so the mediator
    /// can resolve handlers at dispatch time.
    /// </remarks>
    /// <param name="serviceRegistry">The service registry to add the registrations to.</param>
    /// <param name="configure">A callback used to configure the <see cref="MediatorOptions"/>.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <param name="append">Whether to append the registrations to existing ones.</param>
    /// <returns>The supplied <paramref name="serviceRegistry"/> for chaining.</returns>
    [RequiresDynamicCode("This method uses reflection to find types from the assemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the assemblies defined in the options.")]
    private static IServiceRegistry AddSnowberryMediator(this IServiceRegistry serviceRegistry, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime, bool append)
    {
        _ = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _ = configure ?? throw new ArgumentNullException(nameof(configure));

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

        return serviceRegistry;
    }
}