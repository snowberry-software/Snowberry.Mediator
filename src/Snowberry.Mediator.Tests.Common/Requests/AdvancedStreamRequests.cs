using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class ComplexDataStreamRequest : IStreamRequest<ComplexDataStreamRequest, ComplexDataItem>
{
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public int Count { get; set; } = 5;
    public string Prefix { get; set; } = "Item";
}

public class ComplexDataItem
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Sequence { get; set; }
}

public class DisposableStreamRequest : IStreamRequest<DisposableStreamRequest, DisposableResource>
{
    public int ResourceCount { get; set; } = 10;
}

public class DisposableResource : IAsyncDisposable
{
    public string Name { get; set; } = string.Empty;
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

public class FilterableStreamRequest : IStreamRequest<FilterableStreamRequest, int>
{
    public int Count { get; set; } = 10;
    public int StartValue { get; set; } = 1;
    public Func<int, bool> FilterCondition { get; set; } = _ => true;
}

public class FaultyStreamRequest : IStreamRequest<FaultyStreamRequest, int>
{
    public int Count { get; set; } = 10;
    public HashSet<int> FaultAtPositions { get; set; } = [];
}