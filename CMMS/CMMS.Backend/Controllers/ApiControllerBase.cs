using CMMS.Backend.Security;
using CMMS.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentTaiKhoanId => User.GetTaiKhoanId();
    protected string CurrentNhanVienId => User.GetNhanVienId();

    protected IActionResult FromResult<T>(ApiResponse<T> res)
        => res.Success ? Ok(res) : BadRequest(res);
}
