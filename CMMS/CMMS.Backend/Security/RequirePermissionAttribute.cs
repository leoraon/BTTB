using CMMS.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CMMS.Backend.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(params string[] anyOf) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn."));
            return;
        }

        if (anyOf.Length > 0 && !anyOf.Any(p => user.HasClaim("perm", p)))
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail($"Bạn không có quyền thực hiện thao tác này (cần quyền: {string.Join(" hoặc ", anyOf)})."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
