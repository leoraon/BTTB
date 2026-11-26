using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
        => FromResult(await auth.LoginAsync(req));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
        => FromResult(await auth.LogoutAsync(CurrentTaiKhoanId, User.Identity?.Name ?? string.Empty));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        => FromResult(await auth.ChangePasswordAsync(CurrentTaiKhoanId, req));

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
        => FromResult(await auth.UpdateProfileAsync(CurrentNhanVienId, req));

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
        => FromResult(await auth.GetCurrentUserAsync(CurrentTaiKhoanId));
}
