using System.Net;

namespace Shared.Domain.Exceptions;

public class ValidationException(string message) : DomainException(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
    public override string Title => "Validation error";
}