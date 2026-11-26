using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api")]
[Authorize]
public class NhanSuController(INhanSuService svc) : ApiControllerBase
{
    [HttpGet("nhanvien")]
    public async Task<IActionResult> GetNhanViens([FromQuery] string? keyword, [FromQuery] string? donViId, [FromQuery] string? trangThai)
        => FromResult(await svc.GetNhanViensAsync(keyword, donViId, trangThai));

    [HttpGet("nhanvien/ky-thuat-vien")]
    public async Task<IActionResult> GetKyThuatViens()
        => FromResult(await svc.GetKyThuatViensAsync());

    [HttpPost("nhanvien")]
    [RequirePermission(MaQuyen.USER_MANAGE)]
    public async Task<IActionResult> CreateNhanVien([FromBody] NhanVienUpsertReq req)
        => FromResult(await svc.CreateNhanVienAsync(req));

    [HttpPut("nhanvien/{id}")]
    [RequirePermission(MaQuyen.USER_MANAGE)]
    public async Task<IActionResult> UpdateNhanVien(string id, [FromBody] NhanVienUpsertReq req)
        => FromResult(await svc.UpdateNhanVienAsync(id, req, CurrentTaiKhoanId));

    [HttpDelete("nhanvien/{id}")]
    [RequirePermission(MaQuyen.USER_MANAGE)]
    public async Task<IActionResult> NgungCongTac(string id)
        => FromResult(await svc.NgungCongTacNhanVienAsync(id, CurrentTaiKhoanId));

    [HttpGet("taikhoan")]
    [RequirePermission(MaQuyen.USER_MANAGE, MaQuyen.ACCOUNT_APPROVE)]
    public async Task<IActionResult> GetTaiKhoans([FromQuery] string? trangThai)
        => FromResult(await svc.GetTaiKhoansAsync(trangThai));

    [HttpPost("taikhoan/de-xuat")]
    [RequirePermission(MaQuyen.USER_MANAGE)]
    public async Task<IActionResult> DeXuatTaiKhoan([FromBody] DeXuatTaiKhoanReq req)
        => FromResult(await svc.DeXuatTaiKhoanAsync(req, CurrentTaiKhoanId));

    [HttpPut("taikhoan/{id:int}/phe-duyet")]
    [RequirePermission(MaQuyen.ACCOUNT_APPROVE)]
    public async Task<IActionResult> PheDuyetTaiKhoan(int id, [FromBody] PheDuyetTaiKhoanReq req)
        => FromResult(await svc.PheDuyetTaiKhoanAsync(id, req, CurrentNhanVienId, CurrentTaiKhoanId));

    [HttpPut("taikhoan/{id:int}/khoa-mo")]
    [RequirePermission(MaQuyen.USER_MANAGE)]
    public async Task<IActionResult> KhoaMoTaiKhoan(int id, [FromBody] KhoaMoTaiKhoanReq req)
        => FromResult(await svc.KhoaMoTaiKhoanAsync(id, req, CurrentTaiKhoanId));
}
