using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application;

namespace Yandex.Web.Extensions;

public static class ApiResponseExtensions
{
    public static IActionResult ToActionResult(this ApiResponse response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.OK => new OkObjectResult(response),
            HttpStatusCode.Created => new CreatedResult("", response),
            HttpStatusCode.NoContent => new NoContentResult(),
            HttpStatusCode.BadRequest => new BadRequestObjectResult(response),
            HttpStatusCode.NotFound => new NotFoundObjectResult(response),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(response),
            _ => new ObjectResult(response) { StatusCode = (int)response.StatusCode }
        };
    }
}