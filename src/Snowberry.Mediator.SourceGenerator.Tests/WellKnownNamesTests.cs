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
        Assert.Equal(nameof(RequestHandlerInfo.HandlerType), WellKnown.PropHandlerType);
        Assert.Equal(nameof(NotificationHandlerInfo.HandlerType), WellKnown.PropHandlerType);
        Assert.Equal(nameof(RequestHandlerInfo.RequestType), WellKnown.PropRequestType);
        Assert.Equal(nameof(RequestHandlerInfo.ResponseType), WellKnown.PropResponseType);
        Assert.Equal(nameof(NotificationHandlerInfo.NotificationType), WellKnown.PropNotificationType);
        Assert.Equal(nameof(PipelineBehaviorHandlerInfo.PriorityOverride), WellKnown.PropPriorityOverride);
    }

    [Fact]
    public void MethodNameConstants_MatchRuntimeMembers()
    {
        Assert.Equal(nameof(GlobalPipelineRegistry.Register), WellKnown.MethodRegister);
        Assert.Equal(nameof(GlobalNotificationHandlerRegistry.Register), WellKnown.MethodRegister);
        Assert.Equal(nameof(GlobalPipelineRegistry.Build), WellKnown.MethodBuild);
        Assert.Equal(nameof(GlobalNotificationHandlerRegistry.Build), WellKnown.MethodBuild);
        Assert.Equal(nameof(IServiceContext.IsServiceRegistered), WellKnown.MethodIsServiceRegistered);
        Assert.Equal(nameof(IServiceContext.TryRegister), WellKnown.MethodTryRegister);
        Assert.Equal(nameof(IServiceContext.TryToGetSingleton), WellKnown.MethodTryToGetSingleton);
        Assert.Equal(nameof(MicrosoftServiceContext.Create), WellKnown.MethodCreate);
        Assert.Equal(nameof(SnowberryServiceContext.Create), WellKnown.MethodCreate);
        Assert.Equal(nameof(System.Type.MakeGenericType), WellKnown.MethodMakeGenericType);
    }
}
