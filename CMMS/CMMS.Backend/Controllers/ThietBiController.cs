using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/thietbi")]
[Authorize]
public class ThietBiController(IThietBiService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? viTriId,
        [FromQuery] string? boPhanId,
        [FromQuery] string? nhomId,
        [FromQuery] string? trangThai)
        => FromResult(await svc.SearchAsync(keyword, viTriId, boPhanId, nhomId, trangThai));

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(
        [FromQuery] string? keyword,
        [FromQuery] string? viTriId,
        [FromQuery] string? boPhanId,
        [FromQuery] string? nhomId,
        [FromQuery] string? trangThai)
        => FromResult(await svc.GetTreeAsync(keyword, viTriId, boPhanId, nhomId, trangThai));

    [HttpGet("{id}/ly-lich")]
    public async Task<IActionResult> GetLyLich(string id)
        => FromResult(await svc.GetPhieuLyLichAsync(id));

    [HttpGet("{id}/ly-lich/export")]
    public async Task<IActionResult> ExportLyLich(string id, [FromQuery] string format = "pdf")
    {
        var res = await svc.ExportPhieuLyLichAsync(id, format);
        return res.Ok ? File(res.Bytes, res.ContentType, res.FileName) : BadRequest(ApiResponse<object>.Fail(res.Error));
    }

    [HttpGet("{id}/lich-su")]
    public async Task<IActionResult> GetLichSu(string id)
        => FromResult(await svc.GetLichSuBaoTriAsync(id));

    [HttpPost]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> Create([FromBody] ThietBiUpsertReq req)
        => FromResult(await svc.CreateAsync(req));

    [HttpPut("{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> Update(string id, [FromBody] ThietBiUpsertReq req)
        => FromResult(await svc.UpdateAsync(id, req));

    [HttpDelete("{id}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> Delete(string id)
        => FromResult(await svc.DeleteAsync(id));

    [HttpPut("{id}/trang-thai")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> UpdateTrangThai(string id, [FromBody] UpdateTrangThaiThietBiReq req)
        => FromResult(await svc.UpdateTrangThaiAsync(id, req.TrangThaiVanHanh));

    [HttpGet("{id}/thongso")]
    public async Task<IActionResult> GetThongSo(string id)
        => FromResult(await svc.GetThongSoAsync(id));

    [HttpPost("{id}/thongso")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> AddThongSo(string id, [FromBody] ThongSoUpsertReq req)
        => FromResult(await svc.AddThongSoAsync(id, req));

    [HttpPut("thongso/{thongSoId:long}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> UpdateThongSo(long thongSoId, [FromBody] ThongSoUpsertReq req)
        => FromResult(await svc.UpdateThongSoAsync(thongSoId, req));

    [HttpDelete("thongso/{thongSoId:long}")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> DeleteThongSo(long thongSoId)
        => FromResult(await svc.DeleteThongSoAsync(thongSoId));

    [HttpPost("{id}/upload-anh")]
    [Consumes("multipart/form-data")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> UploadAnh(string id, IFormFile file)
        => FromResult(await svc.UploadAnhAsync(id, file));

    [HttpPost("{id}/upload-pdf")]
    [Consumes("multipart/form-data")]
    [RequirePermission(MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> UploadPdf(string id, IFormFile file)
        => FromResult(await svc.UploadTaiLieuPdfAsync(id, file));

    [HttpGet("{id}/anh")]
    public async Task<IActionResult> DownloadAnh(string id)
    {
        var res = await svc.DownloadTaiLieuAsync(id, isImage: true);
        return res.Found ? File(res.Bytes, res.ContentType, res.FileName) : NotFound(ApiResponse<object>.Fail("Không tìm thấy ảnh thiết bị."));
    }

    [HttpGet("{id}/tai-lieu")]
    public async Task<IActionResult> DownloadPdf(string id)
    {
        var res = await svc.DownloadTaiLieuAsync(id, isImage: false);
        return res.Found ? File(res.Bytes, res.ContentType, res.FileName) : NotFound(ApiResponse<object>.Fail("Không tìm thấy tài liệu hướng dẫn PDF."));
    }
}
