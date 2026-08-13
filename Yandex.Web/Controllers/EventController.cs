using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Requests.Events;
using Yandex.Web.Extensions;

namespace Yandex.Web.Controllers;

[ApiController]
[Route("events")]
public class EventController(IEventService eventService, IBookingService bookingService) : ControllerBase
{
    /// <summary>
    /// Get events list
    /// </summary>
    /// <param name="filter">Filter parameters (title, from, to, page, pageSize)</param>
    /// <response code="200">Returns list of events</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<EventDto>>), StatusCodes.Status200OK)]
    public IActionResult GetEvents([FromQuery] EventFilter filter)
    {
        var data = eventService.GetEvents(filter);
        var response = new ApiResponse<PaginatedResult<EventDto>>(data);

        return response.ToActionResult();
    }

    /// <summary>
    /// Get event
    /// </summary>
    /// <param name="id">Event identifier</param>
    /// <response code="200">Returns the event</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
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
    /// <response code="201">Event created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public IActionResult CreateEvent([FromBody] CreateEventRequest request)
    {
        var data = eventService.CreateEvent(request);
        var response = new ApiResponse<EventDto>(data, "Successfully created new event", HttpStatusCode.Created);

        return response.ToActionResult();
    }

    /// <summary>
    /// Update existing event
    /// </summary>
    /// <param name="id">Event identifier</param>
    /// <param name="request">UpdateEventRequest object</param>
    /// <response code="200">Event updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Event not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public IActionResult UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        var data = eventService.UpdateEvent(id, request);
        var response = new ApiResponse<EventDto>(data, "Successfully updated", HttpStatusCode.OK);

        return response.ToActionResult();
    }

    /// <summary>
    /// Delete event
    /// </summary>
    /// <param name="id">Event identifier</param>
    /// <response code="204">Event deleted successfully</response>
    /// <response code="404">Event not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public IActionResult DeleteEvent(Guid id)
    {
        eventService.DeleteEvent(id);

        var response = new ApiResponse("Successfully deleted", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }

    /// <summary>
    /// Create booking
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <response code="202">Booking accepted for processing</response>
    /// <response code="404">Event not found</response>
    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBooking(Guid id)
    {
        var data = await bookingService.CreateBookingAsync(id);
        var location = Url.Action(
            nameof(BookingController.GetBooking),
            nameof(BookingController).Replace("Controller", ""),
            new { id = data.Id },
            Request.Scheme,
            Request.Host.ToUriComponent()
        );
        var response = new ApiResponse<BookingDto>(data, "Successfully created booking", HttpStatusCode.Accepted);

        Response.Headers.Append("Location", location);

        return response.ToActionResult();
    }
}