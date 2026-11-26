using System.Net;

namespace Shared.Domain.Exceptions;

public class NotFoundException(string message) : DomainException(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
    public override string Title => "Resource not found";
}