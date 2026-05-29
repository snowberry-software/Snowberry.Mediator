using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.DependencyInjection.Shared.Contracts;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries;

namespace Snowberry.Mediator.SourceGenerator.Tests;

/// <summary>
/// Pins the generator's emitted member-name constants (<see cref="WellKnown"/>) to the real runtime members.
/// The generator emits these names as text and cannot reference the runtime types directly (that would breach
/// its self-contained analyzer design / RS1038), so these <c>nameof</c> assertions are the compile-time guard:
/// renaming a referenced property or method stops this file compiling, surfacing the break at build time
/// instead of in generated consumer code.
/// </summary>
public class WellKnownNamesTests
{
    [Fact]
    public void PropertyNameConstants_MatchRuntimeMembers()
    {
        Assert.Equal(nameof(RequestHandlerInfo.HandlerType), WellKnown.c_PropHandlerType);
        Assert.Equal(nameof(NotificationHandlerInfo.HandlerType), WellKnown.c_PropHandlerType);
        Assert.Equal(nameof(RequestHandlerInfo.RequestType), WellKnown.c_PropRequestType);
        Assert.Equal(nameof(RequestHandlerInfo.ResponseType), WellKnown.c_PropResponseType);
        Assert.Equal(nameof(NotificationHandlerInfo.NotificationType), WellKnown.c_PropNotificationType);
        Assert.Equal(nameof(PipelineBehaviorHandlerInfo.PriorityOverride), WellKnown.c_PropPriorityOverride);
    }

    [Fact]
    public void MethodNameConstants_MatchRuntimeMembers()
    {
        Assert.Equal(nameof(GlobalPipelineRegistry.Register), WellKnown.c_MethodRegister);
        Assert.Equal(nameof(GlobalNotificationHandlerRegistry.Register), WellKnown.c_MethodRegister);
        Assert.Equal(nameof(GlobalPipelineRegistry.Build), WellKnown.c_MethodBuild);
        Assert.Equal(nameof(GlobalNotificationHandlerRegistry.Build), WellKnown.c_MethodBuild);
        Assert.Equal(nameof(IServiceContext.IsServiceRegistered), WellKnown.c_MethodIsServiceRegistered);
        Assert.Equal(nameof(IServiceContext.TryRegister), WellKnown.c_MethodTryRegister);
        Assert.Equal(nameof(IServiceContext.TryToGetSingleton), WellKnown.c_MethodTryToGetSingleton);
        Assert.Equal(nameof(MicrosoftServiceContext.Create), WellKnown.c_MethodCreate);
        Assert.Equal(nameof(SnowberryServiceContext.Create), WellKnown.c_MethodCreate);
        Assert.Equal(nameof(System.Type.MakeGenericType), WellKnown.c_MethodMakeGenericType);
    }
}
