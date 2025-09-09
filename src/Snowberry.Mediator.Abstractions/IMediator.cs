using Snowberry.Mediator.Abstractions.Mediator;

namespace Snowberry.Mediator.Abstractions;

/// <summary>
/// The mediator contract that combines both sending requests and publishing notifications.
/// </summary>
public interface IMediator : IMediatorSender, IMediatorPublisher
{
}