using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class ComplexRequest : IRequest<ComplexRequest, string>
{
    public int Factor { get; set; } = 1;
    public string Message { get; set; } = "Hello";
}