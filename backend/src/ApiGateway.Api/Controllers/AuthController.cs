using ApiGateway.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Api.Controllers;

[Route("api/auth")]
[Tags("Auth")]
public sealed class AuthController(AuthService auth) : ApiControllerBase
{
    /// <summary>Create an owner or consumer account and sign in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        Created(await auth.RegisterAsync(request, cancellationToken));

    /// <summary>Exchange email and password for a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    /// <summary>The signed-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await auth.GetCurrentAsync(cancellationToken));
}
