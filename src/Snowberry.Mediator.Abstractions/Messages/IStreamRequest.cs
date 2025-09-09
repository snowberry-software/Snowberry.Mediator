namespace Snowberry.Mediator.Abstractions.Messages;

/// <summary>
/// Contract marking a request that expects a stream of responses of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IStreamRequest<in TRequest, out TResponse>;
