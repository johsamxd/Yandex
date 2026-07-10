using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Events;
using Yandex.Web.Extensions;

namespace Yandex.Web.Controllers;

[Route("events")]
public class EventController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Get events list
    /// </summary>
    [HttpGet]
    public IActionResult GetEvents()
    {
        var data = eventService.GetEvents();
        var response = new ApiResponse<IEnumerable<EventDto>>(data);

        return response.ToActionResult();
    }


    /// <summary>
    /// Get event
    /// </summary>
    /// <param name="id">Identifier</param>
    [HttpGet("{id}")]
    public IActionResult GetEvent(Guid id)
    {
        var data = eventService.GetEvent(id);
        var response = new ApiResponse<EventDto>(data);

        return response.ToActionResult();
    }

    /// <summary>
    /// Create new event
    /// </summary>
    /// <param name="request">CreateEventRequest object</param>
    [HttpPost]
    public IActionResult CreateEvent([FromBody] CreateEventRequest request)
    {
        eventService.CreateEvent(request);

        var response = new ApiResponse("Succesfully created new event", true, HttpStatusCode.Created);

        return response.ToActionResult();
    }

    /// <summary>
    /// Update existing event
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <param name="request">UpdateEventRequest object</param>
    [HttpPut("{id}")]
    public IActionResult UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        eventService.UpdateEvent(id, request);

        var response = new ApiResponse("Succesfully updated", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }

    /// <summary>
    /// Delete event
    /// </summary>
    /// <param name="id">Identifier</param>
    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(Guid id)
    {
        eventService.DeleteEvent(id);

        var response = new ApiResponse("Succesfully deleted", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }
}