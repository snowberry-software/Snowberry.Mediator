namespace Snowberry.Mediator.DependencyInjection.Shared;

/// <summary>
/// The lifetime of a service.
/// </summary>
public enum RegistrationServiceLifetime
{
    /// <summary>
    /// A single instance is created and shared throughout the application's lifetime.
    /// </summary>
    Singleton,

    /// <summary>
    /// A new instance is created each time the service is requested.
    /// </summary>
    Transient,

    /// <summary>
    /// A new instance is created for each scope.
    /// </summary>
    Scoped
}
