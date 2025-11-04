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
    /// <param name="configure">The configuration method.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The service collection.</returns>
    [RequiresDynamicCode("This method uses reflection to find types from the asemblies defined in the options.")]
    [RequiresUnreferencedCode("This method uses reflection to find types from the asemblies defined in the options.")]
    public static IServiceCollection AddSnowberryMediator(this IServiceCollection services, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var options = new MediatorOptions();
        configure(options);

        var serviceContext = new MicrosoftServiceContext(services);
        DependencyInjectionHelper.AddSnowberryMediator(serviceContext, options, serviceLifetime switch
        {
            ServiceLifetime.Singleton => RegistrationServiceLifetime.Singleton,
            ServiceLifetime.Scoped => RegistrationServiceLifetime.Scoped,
            ServiceLifetime.Transient => RegistrationServiceLifetime.Transient,
            _ => throw new NotSupportedException($"The service lifetime '{serviceLifetime}' is not supported."),
        }, append: false);

        return services;
    }
}
