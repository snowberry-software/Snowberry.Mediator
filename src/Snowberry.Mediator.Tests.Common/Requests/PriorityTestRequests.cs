using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class PriorityTestRequest : IRequest<PriorityTestRequest, string>
{
    public string Message { get; set; } = "Default";
}

public class MultiBehaviorRequest : IRequest<MultiBehaviorRequest, int>
{
    public int Value { get; set; } = 0;
}

public class PerformanceTestRequest : IRequest<PerformanceTestRequest, int>
{
    public int BaseValue { get; set; } = 0;
}

public class InheritanceTestRequest : IRequest<InheritanceTestRequest, string>
{
    public string Data { get; set; } = "default";
}