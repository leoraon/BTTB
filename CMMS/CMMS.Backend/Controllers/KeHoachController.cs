using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/kehoach")]
[Authorize]
public class KeHoachController(IKeHoachService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? thietBiId, [FromQuery] string? trangThai, [FromQuery] bool chiQuaHan = false)
        => FromResult(await svc.GetAllAsync(thietBiId, trangThai, chiQuaHan));

    [HttpPost]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> Create([FromBody] KeHoachUpsertReq req)
        => FromResult(await svc.CreateAsync(req));

    [HttpPut("{id}")]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> Update(string id, [FromBody] KeHoachUpsertReq req)
        => FromResult(await svc.UpdateAsync(id, req));

    [HttpPut("{id}/trang-thai")]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE, MaQuyen.WO_CREATE_DISPATCH)]
    public async Task<IActionResult> UpdateTrangThai(string id, [FromQuery] string trangThaiMoi)
        => FromResult(await svc.UpdateTrangThaiAsync(id, trangThaiMoi));

    [HttpGet("{id}/lich-su")]
    public async Task<IActionResult> GetLichSu(string id)
        => FromResult(await svc.GetLichSuKyAsync(id));

    [HttpGet("du-kien")]
    public async Task<IActionResult> GetLichDuKien(
        [FromQuery] DateOnly? tuNgay,
        [FromQuery] DateOnly? denNgay,
        [FromQuery] string? thietBiId,
        [FromQuery] string? nhomThietBiId,
        [FromServices] TimeProvider? timeProvider = null)
    {
        var today = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetLocalNow().DateTime);
        var start = tuNgay ?? new DateOnly(today.Year, today.Month, 1);
        var end = denNgay ?? start.AddMonths(1).AddDays(-1);
        return FromResult(await svc.GetLichDuKienAsync(start, end, thietBiId, nhomThietBiId));
    }

    [HttpPost("sync-work-orders")]
    [RequirePermission(MaQuyen.WO_CREATE_DISPATCH, MaQuyen.CHECKLIST_MANAGE, MaQuyen.KPI_MONITOR)]
    public async Task<IActionResult> SyncWorkOrders()
        => FromResult(await svc.SyncWorkOrdersAsync(CurrentTaiKhoanId));
}
