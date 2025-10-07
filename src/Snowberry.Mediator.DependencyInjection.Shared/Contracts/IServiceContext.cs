namespace Snowberry.Mediator.DependencyInjection.Shared.Contracts;

/// <summary>
/// Defines a context for registering services with specific lifetimes.
/// </summary>
public interface IServiceContext
{
    /// <summary>
    /// Tries to get a singleton instance of the service type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="found">Indicates whether the singleton was found.</param>
    /// <returns>The singleton.</returns>
    T? TryToGetSingleton<T>(out bool found);

    /// <summary>
    /// Checks if the service type <typeparamref name="T"/> is already registered.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The result.</returns>
    bool IsServiceRegistered<T>();

    /// <summary>
    /// Registers a service with the specified lifetime.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="lifetime">The service lifetime.</param>
    void TryRegister(Type serviceType, Type implementationType, RegistrationServiceLifetime lifetime);

    /// <summary>
    /// Registers a singleton instance of a service.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="instance">The instance type.</param>
    void TryRegister(Type serviceType, object instance);
}
