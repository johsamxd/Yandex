using System.Security.Claims;
using UserService.Domain.Exceptions;

namespace UserService.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedException("User id claim is missing or invalid");
        }

        return userId;
    }
}