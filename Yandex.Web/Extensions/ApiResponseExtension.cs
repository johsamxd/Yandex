using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application.Dtos;

namespace Yandex.Web.Extensions;

public static class ApiResponseExtension
{
    public static IActionResult ToActionResult(this ApiResponse response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.OK => new OkObjectResult(response),
            HttpStatusCode.Created => new CreatedResult("", response),
            HttpStatusCode.NoContent => new NoContentResult(),
            _ => new ObjectResult(response) { StatusCode = (int)response.StatusCode }
        };
    }
}