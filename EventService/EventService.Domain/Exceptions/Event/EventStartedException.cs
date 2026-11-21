using System.Net;
using Shared.Domain.Exceptions;

namespace EventService.Domain.Exceptions.Event;

public class EventStartedException(string message) : DomainException(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
    public override string Title => "Event started";
}