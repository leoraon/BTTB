using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/vaitro")]
[Authorize]
public class PhanQuyenController(IPhanQuyenService svc) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(MaQuyen.ROLE_MANAGE, MaQuyen.USER_MANAGE)]
    public async Task<IActionResult> GetAll()
        => FromResult(await svc.GetAllVaiTrosAsync());

    [HttpGet("quyen-catalog")]
    [RequirePermission(MaQuyen.ROLE_MANAGE)]
    public IActionResult GetCatalog()
        => Ok(ApiResponse<IReadOnlyList<MaQuyen.QuyenInfo>>.Ok(svc.GetDanhMucMaQuyen()));

    [HttpPost]
    [RequirePermission(MaQuyen.ROLE_MANAGE)]
    public async Task<IActionResult> Create([FromBody] VaiTroUpsertReq req)
        => FromResult(await svc.CreateVaiTroAsync(req));

    [HttpPut("{id:int}")]
    [RequirePermission(MaQuyen.ROLE_MANAGE)]
    public async Task<IActionResult> Update(int id, [FromBody] VaiTroUpsertReq req)
        => FromResult(await svc.UpdateVaiTroAsync(id, req));

    [HttpPut("{id:int}/quyen")]
    [RequirePermission(MaQuyen.ROLE_MANAGE)]
    public async Task<IActionResult> UpdateQuyen(int id, [FromBody] CapNhatQuyenVaiTroReq req)
        => FromResult(await svc.CapNhatQuyenVaiTroAsync(id, req, CurrentTaiKhoanId));

    [HttpPut("/api/taikhoan/{taiKhoanId:int}/vai-tro")]
    [RequirePermission(MaQuyen.ROLE_MANAGE)]
    public async Task<IActionResult> GanVaiTro(int taiKhoanId, [FromBody] GanVaiTroReq req)
        => FromResult(await svc.GanVaiTroTaiKhoanAsync(taiKhoanId, req.VaiTroId, CurrentTaiKhoanId));
}
