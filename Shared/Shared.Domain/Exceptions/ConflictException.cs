using System.Net;

namespace Shared.Domain.Exceptions;

public class ConflictException(string message) : DomainException(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
    public override string Title => "Conflict";
}