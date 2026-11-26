using System.Net;

namespace Shared.Domain.Exceptions;

public class ForbiddenException(string message) : DomainException(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
    public override string Title => "Forbidden";
}