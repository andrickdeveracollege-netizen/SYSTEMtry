namespace EventManagement.Service;

public class ServiceException : Exception
{
    public int StatusCode { get; }
    public ServiceException(string message, int statusCode = 400) : base(message) => StatusCode = statusCode;
}