namespace Snowberry.Mediator.Tests.Common;

public class CustomBusinessException : Exception
{
    public CustomBusinessException(string message) : base(message) { }
    public CustomBusinessException(string message, Exception innerException) : base(message, innerException) { }
}