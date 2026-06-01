using System.Diagnostics.CodeAnalysis;

namespace Snowberry.Mediator.DependencyInjection.Shared.Contracts;

/// <summary>
/// Defines a context for registering services with specific lifetimes.
/// </summary>
public interface IServiceContext
{
    /// <summary>
    /// Checks if the service type <typeparamref name="T"/> is already registered.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns><see langword="true"/> if a registration for <typeparamref name="T"/> exists; otherwise, <see langword="false"/>.</returns>
    bool IsServiceRegistered<T>();

    /// <summary>
    /// Registers a service with the specified lifetime.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="lifetime">The service lifetime.</param>
    void TryRegister(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type implementationType, RegistrationServiceLifetime lifetime);

    /// <summary>
    /// Registers a singleton instance of a service.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="instance">The singleton instance to register for <paramref name="serviceType"/>.</param>
    void TryRegister([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type serviceType, object instance);

    /// <summary>
    /// Tries to get a singleton instance of the service type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="found">When this method returns, contains <see langword="true"/> if the singleton was found; otherwise, <see langword="false"/>.</param>
    /// <returns>The singleton instance of <typeparamref name="T"/>, or <see langword="null"/> if none was found.</returns>
    T? TryToGetSingleton<T>(out bool found);
}