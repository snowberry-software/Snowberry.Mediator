namespace Snowberry.Mediator.DependencyInjection.Shared;

public interface IServiceContext
{
    void Register(Type serviceType, Type implementationType, RegistrationServiceLifetime lifetime);

    void Register(Type serviceType, object instance);
}
