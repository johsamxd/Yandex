using System.Net;
using Microsoft.AspNetCore.Mvc;
using Yandex.Application;
using Yandex.Application.Models;

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
            _ => new ObjectResult(response) { StatusCode = (int)response.StatusCode }
        };
    }
}