using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Requests;

public class ComplexDataStreamRequest : IStreamRequest<ComplexDataStreamRequest, ComplexDataItem>
{
    public int Count { get; set; } = 5;
    public string Prefix { get; set; } = "Item";
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
}

public class ComplexDataItem
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
}

public class DisposableStreamRequest : IStreamRequest<DisposableStreamRequest, DisposableResource>
{
    public int ResourceCount { get; set; } = 10;
}

public class DisposableResource : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }

    public bool IsDisposed { get; private set; }
    public string Name { get; set; } = string.Empty;
}

public class FilterableStreamRequest : IStreamRequest<FilterableStreamRequest, int>
{
    public int Count { get; set; } = 10;
    public Func<int, bool> FilterCondition { get; set; } = _ => true;
    public int StartValue { get; set; } = 1;
}

public class FaultyStreamRequest : IStreamRequest<FaultyStreamRequest, int>
{
    public int Count { get; set; } = 10;
    public HashSet<int> FaultAtPositions { get; set; } = [];
}