using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class CancellationThrowingStreamRequest : IStreamRequest<CancellationThrowingStreamRequest, int>
{
    public int ThrowAfterCount { get; set; } = 5;
}