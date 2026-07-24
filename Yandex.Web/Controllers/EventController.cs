using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos;
using Yandex.Application.Models;
using Yandex.Application.Requests.Events;
using Yandex.Web.Extensions;

namespace Yandex.Web.Controllers;

[ApiController]
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
        var data = eventService.CreateEvent(request);
        var response = new ApiResponse<EventDto>(data, "Successfully created new event", HttpStatusCode.Created);

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
        var data = eventService.UpdateEvent(id, request);
        var response = new ApiResponse<EventDto>(data, "Successfully updated", HttpStatusCode.OK);

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

        var response = new ApiResponse("Successfully deleted", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }
}