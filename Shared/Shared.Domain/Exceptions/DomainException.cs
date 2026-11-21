using System.Net;

namespace Shared.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message)
{
    public abstract HttpStatusCode StatusCode { get; }
    public virtual string Title => StatusCode.ToString();
}