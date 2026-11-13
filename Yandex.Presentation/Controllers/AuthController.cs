using Microsoft.AspNetCore.Mvc;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Users;
using Yandex.Application.Requests.Users;
using Yandex.Presentation.Extensions;

namespace Yandex.Presentation.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Register new user
    /// </summary>
    /// <response code="201">User registered</response>
    /// <response code="409">Login already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var data = await userService.RegisterAsync(request, cancellationToken);
        var response = new ApiResponse<UserDto>(data, "User registered", System.Net.HttpStatusCode.Created);
        return response.ToActionResult();
    }

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    /// <response code="200">Returns JWT token</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var data = await userService.LoginAsync(request, cancellationToken);
        var response = new ApiResponse<LoginResponse>(data);
        return response.ToActionResult();
    }
}