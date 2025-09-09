using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

/// <summary>
/// Test request for mixed pipeline behavior testing
/// </summary>
public class MixedPipelineTestRequest : IRequest<MixedPipelineTestRequest, string>
{
    public string Message { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// Test request for mixed stream pipeline behavior testing
/// </summary>
public class MixedStreamPipelineTestRequest : IStreamRequest<MixedStreamPipelineTestRequest, int>
{
    public int Count { get; set; }
    public int StartValue { get; set; }
}