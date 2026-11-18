using System.Net;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Abstractions.Services;
using UserService.Application.Dtos;
using UserService.Application.Dtos.Users;
using UserService.Application.Requests.Users;
using UserService.Presentation.Extensions;

namespace UserService.Presentation.Controllers;

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
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var data = await userService.RegisterAsync(request, cancellationToken);
        var response = new ApiResponse<UserDto>(data, "User registered", HttpStatusCode.Created);
        return response.ToActionResult();
    }

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    /// <response code="200">Returns JWT token</response>
    /// <response code="400">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var data = await userService.LoginAsync(request, cancellationToken);
        var response = new ApiResponse<LoginResponse>(data);
        return response.ToActionResult();
    }
}