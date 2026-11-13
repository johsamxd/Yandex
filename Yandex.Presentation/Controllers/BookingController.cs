using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Bookings;
using Yandex.Presentation.Extensions;

namespace Yandex.Presentation.Controllers;

[ApiController]
[Route("bookings")]
public class BookingController(IBookingService service) : ControllerBase
{
    /// <summary>
    /// Get booking
    /// </summary>
    /// <param name="id">Booking identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns the event</response>
    /// <response code="404">Event not found</response>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var data = await service.GetBookingByIdAsync(id, userId, cancellationToken);
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
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await service.CancelBookingAsync(id, userId, cancellationToken);
        var response = new ApiResponse("Successfully deleted", true, HttpStatusCode.NoContent);

        return response.ToActionResult();
    }
}