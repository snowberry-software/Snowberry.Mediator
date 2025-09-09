using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class DelayedRequest : IRequest<DelayedRequest, string>
{
    public int DelayMs { get; set; } = 100;
    public string Message { get; set; } = "Default";
}