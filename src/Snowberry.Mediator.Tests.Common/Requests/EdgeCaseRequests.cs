using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class NullableRequest : IRequest<NullableRequest, string>
{
    public string? NullableString { get; set; }
    public string RequiredString { get; set; } = string.Empty;
}

public class ExceptionThrowingRequest : IRequest<ExceptionThrowingRequest, string>
{
    public string Message { get; set; } = string.Empty;
    public bool ShouldThrow { get; set; }
}

public class ExceptionThrowingStreamRequest : IStreamRequest<ExceptionThrowingStreamRequest, int>
{
    public string ExceptionMessage { get; set; } = "Stream exception";
    public int ThrowAfterCount { get; set; }
}

public class LargeDataRequest : IRequest<LargeDataRequest, int>
{
    public byte[] Data { get; set; } = [];
}

public class ConcurrentTestRequest : IRequest<ConcurrentTestRequest, string>
{
    public string Data { get; set; } = string.Empty;
    public int Id { get; set; }
}

public class DefaultValueRequest : IRequest<DefaultValueRequest, string>
{
    public bool Flag { get; set; } = false;
    public int Number { get; set; } = 0;
    public DateTime? OptionalDate { get; set; }
    public string Text { get; set; } = "Default";
}

public class UnicodeRequest : IRequest<UnicodeRequest, string>
{
    public string Text { get; set; } = string.Empty;
}

public class MutableRequest : IRequest<MutableRequest, string>
{
    public string Text { get; set; } = string.Empty;
    public int Value { get; set; }
}