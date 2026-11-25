using System.Net;
using BookingService.Application.Abstractions.Services;
using BookingService.Application.Dtos.Bookings;
using BookingService.Application.Requests.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Dtos;
using Shared.Presentation.Extensions;

namespace BookingService.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("bookings")]
public class BookingController(IBookingService service) : ControllerBase
{
    /// <summary>
    /// Create booking
    /// </summary>
    /// <remarks>
    /// Бронь создаётся в статусе Pending. Фоновый обработчик подтверждает её и публикует событие
    /// BookingConfirmed в Kafka; сервис Events уменьшает количество свободных мест асинхронно.
    /// </remarks>
    /// <param name="request">Event identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="202">Booking accepted for processing</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">Active bookings limit exceeded</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBookingAsync([FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var data = await service.CreateBookingAsync(request.EventId, userId, cancellationToken);
        var response = new ApiResponse<BookingDto>(data, "Successfully created booking", HttpStatusCode.Accepted);

        return AcceptedAtAction(nameof(GetBookingAsync), new { id = data.Id }, response);
    }

    /// <summary>
    /// Get booking
    /// </summary>
    /// <param name="id">Booking identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the event</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id}")]
    [ActionName(nameof(GetBookingAsync))]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        var data = await service.GetBookingByIdAsync(id, userId, isAdmin, cancellationToken);
        var response = new ApiResponse<BookingDto>(data);

        return response.ToActionResult();
    }

    /// <summary>
    /// Cancel booking
    /// </summary>
    /// <param name="id">Booking identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Booking cancelled successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Booking not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBookingAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");
        await service.CancelBookingAsync(id, userId, isAdmin, cancellationToken);
        var response = new ApiResponse("Successfully deleted", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }
}