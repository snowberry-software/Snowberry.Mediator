using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Tests.Common.Notifications;

/// <summary>
/// Simple notification for testing
/// </summary>
public class SimpleNotification : INotification
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int Value { get; set; }
}

/// <summary>
/// User-related notification for testing
/// </summary>
public class UserRegisteredNotification : INotification
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Order-related notification for testing
/// </summary>
public class OrderCompletedNotification : INotification
{
    public decimal Amount { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string CustomerId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}

/// <summary>
/// System event notification for testing
/// </summary>
public class SystemEventNotification : INotification
{
    public string Description { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = [];
}