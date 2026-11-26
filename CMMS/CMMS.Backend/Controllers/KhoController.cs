using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api")]
[Authorize]
public class KhoController(IKhoService svc) : ApiControllerBase
{
    [HttpGet("phutung")]
    public async Task<IActionResult> GetPhuTung([FromQuery] string? keyword, [FromQuery] string? loai, [FromQuery] bool chiDuoiToiThieu = false)
        => FromResult(await svc.GetPhuTungAsync(keyword, loai, chiDuoiToiThieu));

    [HttpPost("phutung")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE)]
    public async Task<IActionResult> CreatePhuTung([FromBody] PhuTungUpsertReq req)
        => FromResult(await svc.CreatePhuTungAsync(req));

    [HttpPut("phutung/{id}")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE)]
    public async Task<IActionResult> UpdatePhuTung(string id, [FromBody] PhuTungUpsertReq req)
        => FromResult(await svc.UpdatePhuTungAsync(id, req));

    [HttpDelete("phutung/{id}")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE)]
    public async Task<IActionResult> DeletePhuTung(string id)
        => FromResult(await svc.DeletePhuTungAsync(id));

    [HttpGet("dinhmuc/{thietBiId}")]
    public async Task<IActionResult> GetDinhMuc(string thietBiId, [FromQuery] bool chiThayTheDinhKy = false)
        => FromResult(await svc.GetDinhMucByThietBiAsync(thietBiId, chiThayTheDinhKy));

    [HttpPost("dinhmuc")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE, MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> UpsertDinhMuc([FromBody] DinhMucUpsertReq req)
        => FromResult(await svc.UpsertDinhMucAsync(req));

    [HttpDelete("dinhmuc/{id:long}")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE, MaQuyen.DEVICE_MANAGE)]
    public async Task<IActionResult> DeleteDinhMuc(long id)
        => FromResult(await svc.DeleteDinhMucAsync(id));

    [HttpGet("capnhatkho")]
    public async Task<IActionResult> GetPhieuCapNhat([FromQuery] string? trangThai)
        => FromResult(await svc.GetPhieuCapNhatTonKhoAsync(trangThai));

    [HttpPost("capnhatkho")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE)]
    public async Task<IActionResult> TaoPhieuCapNhat([FromBody] TaoPhieuCapNhatTonKhoReq req)
        => FromResult(await svc.TaoPhieuCapNhatTonKhoAsync(req, CurrentNhanVienId));

    [HttpPut("capnhatkho/{id}/xac-nhan")]
    [RequirePermission(MaQuyen.STOCK_ADJUST_APPROVE)]
    public async Task<IActionResult> XacNhanCapNhat(string id, [FromQuery] bool laXacNhan = true)
        => FromResult(await svc.XacNhanCapNhatTonKhoAsync(id, laXacNhan, CurrentNhanVienId, CurrentTaiKhoanId));

    [HttpGet("phieuxuat")]
    public async Task<IActionResult> GetPhieuXuat([FromQuery] string? lenhBaoTriId, [FromQuery] string? trangThai)
        => FromResult(await svc.GetPhieuXuatByLenhAsync(lenhBaoTriId, trangThai));

    [HttpPost("phieuxuat/du-tru")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> TaoPhieuDuTru([FromBody] TaoPhieuDuTruReq req)
        => FromResult(await svc.TaoPhieuDuTruAsync(req, CurrentNhanVienId));

    [HttpPut("phieuxuat/{id}/huy")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> HuyPhieuDuTru(string id)
        => FromResult(await svc.HuyPhieuDuTruAsync(id));

    [HttpPut("phieuxuat/{id}/khai-bao")]
    [RequirePermission(MaQuyen.MATERIAL_DECLARE, MaQuyen.WO_EXECUTE)]
    public async Task<IActionResult> KhaiBaoThucDung(string id, [FromBody] KhaiBaoThucDungReq req)
        => FromResult(await svc.KhaiBaoThucDungAsync(id, req, CurrentNhanVienId));

    [HttpPut("phieuxuat/{id}/duyet-vuot")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE, MaQuyen.STOCK_ADJUST_APPROVE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> DuyetVuot(string id, [FromBody] DuyetVuotDuTruReq? req = null)
        => FromResult(await svc.DuyetVuotDuTruAsync(id, req ?? new DuyetVuotDuTruReq { LaPheDuyet = true }, CurrentNhanVienId));

    [HttpPut("phieuxuat/{id}/xac-nhan-xuat")]
    [RequirePermission(MaQuyen.STOCK_PROPOSAL_CREATE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> XacNhanXuat(string id)
        => FromResult(await svc.XacNhanXuatDungAsync(id, CurrentNhanVienId, CurrentTaiKhoanId));
}
