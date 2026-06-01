using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.DependencyInjection.Shared;

namespace Snowberry.Mediator.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mediator services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A callback used to configure the <see cref="MediatorOptions"/>.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <param name="append">
    /// Whether to append the registrations to existing ones (<see langword="true"/>) instead of replacing them
    /// (<see langword="false"/>). Use <see langword="true"/> to extend a mediator already configured on
    /// <paramref name="services"/> (for example, when loading plugins).
    /// </param>
    /// <returns>The supplied <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The mediator is already registered on <paramref name="services"/> and <paramref name="append"/> is <see langword="false"/>; pass <see langword="true"/> to extend it.</exception>
    [RequiresDynamicCode("This method uses reflection to find types from the assemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the assemblies defined in the options.")]
    public static IServiceCollection AddSnowberryMediator(this IServiceCollection services, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped, bool append = false)
    {
        _ = services ?? throw new ArgumentNullException(nameof(services));
        _ = configure ?? throw new ArgumentNullException(nameof(configure));

        var options = new MediatorOptions();
        configure(options);

        var serviceContext = new MicrosoftServiceContext(services);
        DependencyInjectionHelper.AddSnowberryMediator(serviceContext, options, serviceLifetime switch
        {
            ServiceLifetime.Singleton => RegistrationServiceLifetime.Singleton,
            ServiceLifetime.Scoped => RegistrationServiceLifetime.Scoped,
            ServiceLifetime.Transient => RegistrationServiceLifetime.Transient,
            _ => throw new NotSupportedException($"The service lifetime '{serviceLifetime}' is not supported."),
        }, append: append);

        return services;
    }
}