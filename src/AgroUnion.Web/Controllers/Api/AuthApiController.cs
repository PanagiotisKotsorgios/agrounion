using AgroUnion.Application.Contracts;
using AgroUnion.Application.Services;
using AgroUnion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AgroUnion.Web.Controllers.Api;

[ApiController, Route("api/auth"), IgnoreAntiforgeryToken]
public sealed class AuthApiController(UserManager<ApplicationUser> users, IJwtTokenService tokens) : ControllerBase
{
    [HttpPost("token")]
    [ProducesResponseType<JwtTokenResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Token(JwtLoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !await users.CheckPasswordAsync(user, request.Password)) return Unauthorized(new { message = "Μη έγκυρα στοιχεία σύνδεσης." });
        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "";
        return Ok(tokens.Create(user.Id, user.Email!, role));
    }
}
