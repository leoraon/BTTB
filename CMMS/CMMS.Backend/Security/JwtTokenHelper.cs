using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CMMS.Backend.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.Backend.Security;

public class JwtTokenHelper(IOptions<JwtSettings> options, TimeProvider? timeProvider = null)
{
    private readonly JwtSettings _settings = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public (string Token, DateTime ExpiresAt) GenerateToken(TAI_KHOAN tk, IEnumerable<string> permissions)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = utcNow.AddMinutes(_settings.ExpireMinutes);
        var claims = new List<Claim>
        {
            new("taiKhoanId", tk.TaiKhoanId.ToString()),
            new("nhanVienId", tk.NhanVienId),
            new("vaiTroId", tk.VaiTroId.ToString()),
            new(ClaimTypes.Name, tk.TenDangNhap),
            new("hoTen", tk.NhanVien.HoTen),
            new(ClaimTypes.Role, tk.VaiTro.TenVaiTro)
        };

        foreach (var perm in permissions.Distinct())
            claims.Add(new Claim("perm", perm));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAt,
            signingCredentials: creds);

        var localExpires = TimeZoneInfo.ConvertTimeFromUtc(expiresAt, _timeProvider.LocalTimeZone);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), localExpires);
    }
}
