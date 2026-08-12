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
    /// <param name="id">Identifier</param>
    [HttpGet("{id}")]
    public IActionResult GetBooking(Guid id)
    {
        var data = service.GetBookingByIdAsync(id);
        var response = new ApiResponse<BookingDto>(data);

        return response.ToActionResult();
    }
}