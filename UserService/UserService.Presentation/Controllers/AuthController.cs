using System.Net;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Dtos;
using Shared.Presentation.Extensions;
using UserService.Application.Abstractions.Services;
using UserService.Application.Dtos.Users;
using UserService.Application.Requests.Users;

namespace UserService.Presentation.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Register new user
    /// </summary>
    /// <remarks>
    /// Роль <c>Admin</c> может назначить только авторизованный администратор; первый администратор создаётся сидом
    /// (секция конфигурации <c>AdminSeed</c>).
    /// </remarks>
    /// <response code="201">User registered</response>
    /// <response code="400">Invalid data or login already exists</response>
    /// <response code="403">Admin role requested by a non-admin</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var data = await userService.RegisterAsync(request, User.IsInRole("Admin"), cancellationToken);
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