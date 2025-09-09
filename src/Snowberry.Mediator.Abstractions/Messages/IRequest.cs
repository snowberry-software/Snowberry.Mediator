namespace Snowberry.Mediator.Abstractions.Messages;

/// <summary>
/// Contract marking a request that expects a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequest<in TRequest, out TResponse> where TRequest : class;