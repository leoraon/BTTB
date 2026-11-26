using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/lenh")]
[Authorize]
public class LenhBaoTriController(ILenhBaoTriService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDanhSach(
        [FromQuery] string? loaiLenh,
        [FromQuery] string? trangThai,
        [FromQuery] string? thietBiId,
        [FromQuery] string? ktvId,
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay)
        => FromResult(await svc.GetDanhSachAsync(loaiLenh, trangThai, thietBiId, ktvId, tuNgay, denNgay));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(string id)
        => FromResult(await svc.GetDetailAsync(id));

    [HttpPost("tu-yeu-cau")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> TaoTuYeuCau([FromBody] TaoLenhTuYeuCauReq req)
        => FromResult(await svc.TaoLenhTuYeuCauAsync(req, CurrentNhanVienId, CurrentTaiKhoanId));

    [HttpPut("{id}/doi-lich")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> DoiLich(string id, [FromBody] DoiLichLenhReq req)
        => FromResult(await svc.DoiLichAsync(id, req));

    [HttpGet("ho-so-ktv")]
    public async Task<IActionResult> GetHoSoKtv()
        => FromResult(await svc.GetHoSoKtvAsync());

    [HttpPut("ktv/{nhanVienId}/chuyen-mon")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> CapNhatChuyenMon(string nhanVienId, [FromBody] CapNhatChuyenMonKtvReq req)
        => FromResult(await svc.CapNhatChuyenMonKtvAsync(nhanVienId, req));

    [HttpPost("{id}/phan-cong")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> PhanCong(string id, [FromBody] PhanCongKtvReq req)
        => FromResult(await svc.PhanCongKtvAsync(id, req, CurrentNhanVienId));

    [HttpDelete("phan-cong/{phanCongId:long}")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> HuyPhanCong(long phanCongId)
        => FromResult(await svc.HuyPhanCongKtvAsync(phanCongId));

    [HttpPut("{id}/bat-dau")]
    [RequirePermission(MaQuyen.WO_EXECUTE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> BatDau(string id)
        => FromResult(await svc.BatDauLenhAsync(id, CurrentNhanVienId));

    [HttpPut("{id}/tam-dung")]
    [RequirePermission(MaQuyen.WO_EXECUTE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> TamDung(string id, [FromBody] TamDungLenhReq req)
        => FromResult(await svc.TamDungLenhAsync(id, req));

    [HttpPut("{id}/tiep-tuc")]
    [RequirePermission(MaQuyen.WO_EXECUTE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> TiepTuc(string id)
        => FromResult(await svc.TiepTucLenhAsync(id));

    [HttpPut("{id}/ket-thuc")]
    [RequirePermission(MaQuyen.WO_EXECUTE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> KetThuc(string id, [FromBody] KetThucLenhReq req)
        => FromResult(await svc.KetThucLenhAsync(id, req));

    [HttpPost("{id}/checklist-ketqua")]
    [RequirePermission(MaQuyen.CHECKLIST_RECORD, MaQuyen.WO_EXECUTE)]
    public async Task<IActionResult> LuuChecklist(string id, [FromBody] List<KetQuaChecklistItemReq> items)
        => FromResult(await svc.LuuKetQuaChecklistAsync(id, items, CurrentNhanVienId));

    [HttpPost("{id}/baocao-suco")]
    [RequirePermission(MaQuyen.INCIDENT_REPORT_CREATE, MaQuyen.WO_EXECUTE)]
    public async Task<IActionResult> LuuBaoCaoSuCo(string id, [FromBody] BaoCaoSuCoUpsertReq req)
        => FromResult(await svc.LuuBaoCaoSuCoAsync(id, req, CurrentNhanVienId));

    [HttpPost("{id}/nghiem-thu")]
    [RequirePermission(MaQuyen.WO_ACCEPTANCE_SIGN, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> NghiemThu(string id, [FromBody] NghiemThuLenhReq req)
        => FromResult(await svc.NghiemThuLenhAsync(id, req));

    [HttpPost("{id}/dong-lenh")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> DongLenh(string id)
        => FromResult(await svc.DongLenhAsync(id, CurrentNhanVienId, CurrentTaiKhoanId));

    [HttpPut("{id}/huy")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> HuyLenh(string id, [FromBody] HuyLenhReq? req = null)
        => FromResult(await svc.HuyLenhAsync(id, req ?? new HuyLenhReq(), CurrentTaiKhoanId));
}
