using Microsoft.AspNetCore.Mvc;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Bookings;
using Yandex.Web.Extensions;

namespace Yandex.Web.Controllers;

[ApiController]
[Route("bookings")]
public class BookingController(IBookingService service) : ControllerBase
{
    /// <summary>
    /// Get booking
    /// </summary>
    /// <param name="id">Booking identifier</param>
    /// <response code="200">Returns the event</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var data = await service.GetBookingByIdAsync(id);
        var response = new ApiResponse<BookingDto>(data);

        return response.ToActionResult();
    }
}