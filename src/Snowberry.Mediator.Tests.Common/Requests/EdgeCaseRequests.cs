using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class NullableRequest : IRequest<NullableRequest, string>
{
    public string? NullableString { get; set; }
    public string RequiredString { get; set; } = string.Empty;
}

public class ExceptionThrowingRequest : IRequest<ExceptionThrowingRequest, string>
{
    public bool ShouldThrow { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ExceptionThrowingStreamRequest : IStreamRequest<ExceptionThrowingStreamRequest, int>
{
    public int ThrowAfterCount { get; set; }
    public string ExceptionMessage { get; set; } = "Stream exception";
}

public class LargeDataRequest : IRequest<LargeDataRequest, int>
{
    public byte[] Data { get; set; } = [];
}

public class ConcurrentTestRequest : IRequest<ConcurrentTestRequest, string>
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class DefaultValueRequest : IRequest<DefaultValueRequest, string>
{
    public string Text { get; set; } = "Default";
    public int Number { get; set; } = 0;
    public bool Flag { get; set; } = false;
    public DateTime? OptionalDate { get; set; }
}

public class UnicodeRequest : IRequest<UnicodeRequest, string>
{
    public string Text { get; set; } = string.Empty;
}

public class MutableRequest : IRequest<MutableRequest, string>
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}