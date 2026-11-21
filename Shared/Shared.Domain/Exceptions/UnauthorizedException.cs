using System.Net;

namespace Shared.Domain.Exceptions;

public class UnauthorizedException(string message) : DomainException(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;
    public override string Title => "Unauthorized";
}