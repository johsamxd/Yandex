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
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await service.GetBookingByIdAsync(id, cancellationToken);
        var response = new ApiResponse<BookingDto>(data);

        return response.ToActionResult();
    }
}