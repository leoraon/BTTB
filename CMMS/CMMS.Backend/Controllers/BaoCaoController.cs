using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/baocao")]
[Authorize]
[RequirePermission(MaQuyen.REPORT_VIEW_ALL, MaQuyen.REPORT_MAINTENANCE_VIEW, MaQuyen.WO_CREATE_DISPATCH)]
public class BaoCaoController(IBaoCaoService svc) : ApiControllerBase
{
    [HttpGet("bc1")]
    public async Task<IActionResult> Bc1([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc1TongHopAsync(filter));

    [HttpGet("bc2")]
    public async Task<IActionResult> Bc2([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc2DanhSachThietBiAsync(filter));

    [HttpGet("bc3")]
    public async Task<IActionResult> Bc3([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc3TienDoPmAsync(filter));

    [HttpGet("bc4")]
    public async Task<IActionResult> Bc4([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc4SuCoTheoMaLoiAsync(filter));

    [HttpGet("bc5")]
    public async Task<IActionResult> Bc5([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc5KhoiLuongKtvAsync(filter));

    [HttpGet("bc6")]
    public async Task<IActionResult> Bc6([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc6TonKhoToiThieuAsync(filter));

    [HttpGet("bc7")]
    public async Task<IActionResult> Bc7([FromQuery] ReportFilterReq filter)
        => FromResult(await svc.GetBc7XuatTonDoiChieuAsync(filter));

    [HttpGet("{reportCode}/export")]
    public async Task<IActionResult> Export(string reportCode, [FromQuery] string format = "excel", [FromQuery] ReportFilterReq? filter = null)
    {
        var res = await svc.ExportAsync(reportCode, format, filter ?? new ReportFilterReq());
        if (!res.Ok) return BadRequest(ApiResponse<object>.Fail(res.Error));
        return File(res.Bytes, res.ContentType, res.FileName);
    }
}
