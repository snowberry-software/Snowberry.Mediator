using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class CancellationThrowingRequest : IRequest<CancellationThrowingRequest, string>
{
    public string Message { get; set; } = "Test";
}