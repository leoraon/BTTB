using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api")]
[Authorize]
public class DanhMucController(IDanhMucService svc) : ApiControllerBase
{
    [HttpGet("donvi")]
    public async Task<IActionResult> GetDonVi([FromQuery] string? loaiDonVi, [FromQuery] string? keyword)
        => FromResult(await svc.GetDonViAsync(loaiDonVi, keyword));

    [HttpGet("donvi/tree")]
    public async Task<IActionResult> GetDonViTree()
        => FromResult(await svc.GetDonViTreeAsync());

    [HttpPost("donvi")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.SYSTEM_CONFIG)]
    public async Task<IActionResult> CreateDonVi([FromBody] DonViUpsertReq req)
        => FromResult(await svc.CreateDonViAsync(req));

    [HttpPut("donvi/{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.SYSTEM_CONFIG)]
    public async Task<IActionResult> UpdateDonVi(string id, [FromBody] DonViUpsertReq req)
        => FromResult(await svc.UpdateDonViAsync(id, req));

    [HttpDelete("donvi/{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.SYSTEM_CONFIG)]
    public async Task<IActionResult> DeleteDonVi(string id)
        => FromResult(await svc.DeleteDonViAsync(id));

    [HttpGet("nhomthietbi")]
    public async Task<IActionResult> GetNhomThietBi([FromQuery] string? keyword)
        => FromResult(await svc.GetNhomThietBiAsync(keyword));

    [HttpPost("nhomthietbi")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.SYSTEM_CONFIG)]
    public async Task<IActionResult> CreateNhomThietBi([FromBody] NhomThietBiDto req)
        => FromResult(await svc.CreateNhomThietBiAsync(req));

    [HttpPut("nhomthietbi/{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.SYSTEM_CONFIG)]
    public async Task<IActionResult> UpdateNhomThietBi(string id, [FromBody] NhomThietBiDto req)
        => FromResult(await svc.UpdateNhomThietBiAsync(id, req));

    [HttpDelete("nhomthietbi/{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.SYSTEM_CONFIG)]
    public async Task<IActionResult> DeleteNhomThietBi(string id)
        => FromResult(await svc.DeleteNhomThietBiAsync(id));

    [HttpGet("maloi")]
    public async Task<IActionResult> GetMaLoi([FromQuery] string? keyword)
        => FromResult(await svc.GetMaLoiAsync(keyword));

    [HttpPost("maloi")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> CreateMaLoi([FromBody] MaLoiDto req)
        => FromResult(await svc.CreateMaLoiAsync(req));

    [HttpPut("maloi/{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> UpdateMaLoi(string id, [FromBody] MaLoiDto req)
        => FromResult(await svc.UpdateMaLoiAsync(id, req));

    [HttpDelete("maloi/{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> DeleteMaLoi(string id)
        => FromResult(await svc.DeleteMaLoiAsync(id));
}
