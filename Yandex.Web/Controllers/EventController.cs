using Microsoft.AspNetCore.Mvc;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Events;

namespace Yandex.Web.Controllers;

[Route("events")]
public class EventController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Get events list
    /// </summary>
    /// <returns>List of events</returns>
    [HttpGet]
    public IEnumerable<EventDto> GetEvents() => eventService.GetEvents();

    /// <summary>
    /// Get event
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <returns>EventDto object</returns>
    [HttpGet("{id}")]
    public EventDto GetEvent(Guid id) => eventService.GetEvent(id);

    /// <summary>
    /// Create new event
    /// </summary>
    /// <param name="request">CreateEventRequest object</param>
    /// <returns>Created object</returns>
    [HttpPost]
    public IActionResult CreateEvent([FromBody] CreateEventRequest request)
    {
        eventService.CreateEvent(request);

        return Created();
    }

    /// <summary>
    /// Update existing event
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <param name="request">UpdateEventRequest object</param>
    /// <returns>Modified object</returns>
    [HttpPut("{id}")]
    public IActionResult UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        eventService.UpdateEvent(id, request);

        return NoContent();
    }

    /// <summary>
    /// Delete event
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(Guid id)
    {
        eventService.DeleteEvent(id);

        return NoContent();
    }
}