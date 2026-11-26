using System.Security.Claims;

namespace CMMS.Backend.Security;

public static class ClaimsPrincipalExtensions
{
    public static int GetTaiKhoanId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue("taiKhoanId"), out var id) ? id : 0;

    public static string GetNhanVienId(this ClaimsPrincipal user)
        => user.FindFirstValue("nhanVienId") ?? string.Empty;

    public static int GetVaiTroId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue("vaiTroId"), out var id) ? id : 0;

    public static bool HasPermission(this ClaimsPrincipal user, params string[] anyOf)
        => anyOf.Any(p => user.HasClaim("perm", p));
}
