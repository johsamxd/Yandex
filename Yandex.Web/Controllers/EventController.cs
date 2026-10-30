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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns list of events</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<EventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventsAsync([FromQuery] EventFilter filter, CancellationToken cancellationToken)
    {
        var data = await eventService.GetEventsAsync(filter, cancellationToken);
        var response = new ApiResponse<PaginatedResult<EventDto>>(data);

        return response.ToActionResult();
    }

    /// <summary>
    /// Get event
    /// </summary>
    /// <param name="id">Event identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the event</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await eventService.GetEventAsync(id, cancellationToken);
        var response = new ApiResponse<EventDto>(data);

        return response.ToActionResult();
    }

    /// <summary>
    /// Create new event
    /// </summary>
    /// <param name="request">CreateEventRequest object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Event created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var data = await eventService.CreateEventAsync(request, cancellationToken);
        var response = new ApiResponse<EventDto>(data, "Successfully created new event", HttpStatusCode.Created);

        return response.ToActionResult();
    }

    /// <summary>
    /// Update existing event
    /// </summary>
    /// <param name="id">Event identifier</param>
    /// <param name="request">UpdateEventRequest object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Event updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Event not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEventAsync(Guid id, [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var data = await eventService.UpdateEventAsync(id, request, cancellationToken);
        var response = new ApiResponse<EventDto>(data, "Successfully updated", HttpStatusCode.OK);

        return response.ToActionResult();
    }

    /// <summary>
    /// Delete event
    /// </summary>
    /// <param name="id">Event identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Event deleted successfully</response>
    /// <response code="404">Event not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
    {
        await eventService.DeleteEventAsync(id, cancellationToken);

        var response = new ApiResponse("Successfully deleted", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }

    /// <summary>
    /// Create booking
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="202">Booking accepted for processing</response>
    /// <response code="404">Event not found</response>
    /// <response code="409">Event has no available seats</response>
    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBookingAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await bookingService.CreateBookingAsync(id, cancellationToken);
        var location = Url.Action(
            nameof(BookingController.GetBookingAsync),
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