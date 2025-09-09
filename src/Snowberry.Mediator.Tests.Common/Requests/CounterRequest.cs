using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class CounterRequest : IRequest<CounterRequest, int>
{
    public const int c_InitialValue = 5;

    public int InitialValue { get; set; } = c_InitialValue;
}
