using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api")]
[Authorize]
public class YeuCauController(IYeuCauService svc) : ApiControllerBase
{
    [HttpGet("mucuutien")]
    public async Task<IActionResult> GetMucUuTien()
        => FromResult(await svc.GetMucUuTienAsync());

    [HttpPut("mucuutien/{id:int}")]
    [RequirePermission(MaQuyen.SYSTEM_CONFIG, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> UpdateMucUuTien(int id, [FromBody] MucUuTienDto req)
        => FromResult(await svc.UpdateMucUuTienAsync(id, req));

    [HttpGet("yeucau")]
    public async Task<IActionResult> GetDanhSach(
        [FromQuery] string? trangThai,
        [FromQuery] string? nguoiBaoId,
        [FromQuery] bool chiQuaHan = false)
        => FromResult(await svc.GetDanhSachAsync(trangThai, nguoiBaoId, chiQuaHan));

    [HttpGet("yeucau/{id}")]
    public async Task<IActionResult> GetById(string id)
        => FromResult(await svc.GetByIdAsync(id));

    [HttpPost("yeucau")]
    [RequirePermission(MaQuyen.REQUEST_CREATE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> TaoYeuCau([FromBody] TaoSuaYeuCauReq req)
        => FromResult(await svc.TaoYeuCauAsync(req, CurrentNhanVienId));

    [HttpPut("yeucau/{id}")]
    [RequirePermission(MaQuyen.REQUEST_CREATE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> SuaYeuCau(string id, [FromBody] TaoSuaYeuCauReq req)
        => FromResult(await svc.SuaYeuCauAsync(id, req, CurrentNhanVienId, User.HasPermission(MaQuyen.REQUEST_APPROVE)));

    [HttpPut("yeucau/{id}/huy")]
    [RequirePermission(MaQuyen.REQUEST_CREATE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> HuyYeuCau(string id)
        => FromResult(await svc.HuyYeuCauAsync(id, CurrentNhanVienId, User.HasPermission(MaQuyen.REQUEST_APPROVE)));

    [HttpPut("yeucau/{id}/duyet")]
    [RequirePermission(MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> DuyetHoacTuChoi(string id, [FromBody] DuyetYeuCauReq req)
        => FromResult(await svc.DuyetHoacTuChoiAsync(id, req, CurrentNhanVienId));

    [HttpPost("yeucau/upload-anh")]
    [Consumes("multipart/form-data")]
    [RequirePermission(MaQuyen.REQUEST_CREATE, MaQuyen.REQUEST_APPROVE)]
    public async Task<IActionResult> UploadAnh(IFormFile file)
        => FromResult(await svc.UploadAnhSuCoAsync(file));

    [HttpGet("yeucau/{id}/anh")]
    public async Task<IActionResult> DownloadAnh(string id)
    {
        var res = await svc.DownloadAnhSuCoAsync(id);
        return res.Found
            ? File(res.Bytes, res.ContentType, res.FileName)
            : NotFound(ApiResponse<object>.Fail("Không tìm thấy ảnh sự cố."));
    }
}
