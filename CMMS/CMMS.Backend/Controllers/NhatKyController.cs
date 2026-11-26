using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/nhatky")]
[Authorize]
[RequirePermission(MaQuyen.AUDIT_VIEW)]
public class NhatKyController(INhatKyService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int? taiKhoanId,
        [FromQuery] string? loaiThaoTac,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        [FromQuery] string? keyword)
        => FromResult(await svc.SearchAsync(new NhatKyFilterReq
        {
            TaiKhoanId = taiKhoanId,
            LoaiThaoTac = loaiThaoTac,
            TuNgay = tuNgay,
            DenNgay = denNgay,
            Keyword = keyword
        }));
}
