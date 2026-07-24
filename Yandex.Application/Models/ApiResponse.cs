using System.Net;

namespace Yandex.Application.Models;

/// <summary>
/// Api response with data
/// </summary>
public class ApiResponse<T> : ApiResponse
{
    public T Data { get; set; }

    public ApiResponse(T data, string message = "Success")
        : base(message, true, HttpStatusCode.OK)
    {
        Data = data;
    }

    public ApiResponse(T data, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message, true, statusCode)
    {
        Data = data;
    }

    public ApiResponse(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message, false, statusCode)
    {
        Data = default!;
    }
}

/// <summary>
/// Api response without data
/// </summary>
public class ApiResponse
{
    public string Message { get; set; }
    public bool Success { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public DateTime DateTime { get; set; }

    public ApiResponse()
    {
        Message = "Success";
        Success = true;
        StatusCode = HttpStatusCode.OK;
        DateTime = DateTime.UtcNow;
    }

    public ApiResponse(string message, bool success, HttpStatusCode statusCode)
    {
        Message = message;
        Success = success;
        StatusCode = statusCode;
        DateTime = DateTime.UtcNow;
    }
}