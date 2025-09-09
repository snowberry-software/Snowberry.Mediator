using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class NumberStreamRequest : IStreamRequest<NumberStreamRequest, int>
{
    public int Count { get; set; } = 5;
    public int StartValue { get; set; } = 1;
}