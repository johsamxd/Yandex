using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;
using Yandex.Application;
using Yandex.Application.Exceptions;
using Yandex.Web.Extensions;

namespace Yandex.Web.Filters;

public class ApiExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        ApiResponse response;

        switch (context.Exception)
        {
            case NotFoundException exception:
                response = new ApiResponse(exception.Message, false, HttpStatusCode.NotFound);
                break;

            default:
                response = new ApiResponse(
                    "An unexpected error occurred. Please try again later.",
                    false,
                    HttpStatusCode.InternalServerError
                );
                break;
        }

        context.HttpContext.Response.StatusCode = (int)response.StatusCode;
        context.Result = response.ToActionResult();
        context.ExceptionHandled = true;
    }
}