using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/dashboard")]
[Authorize]
[RequirePermission(MaQuyen.KPI_MONITOR, MaQuyen.REPORT_VIEW_ALL, MaQuyen.REPORT_MAINTENANCE_VIEW, MaQuyen.WO_CREATE_DISPATCH)]
public class DashboardController(IDashboardService svc) : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => FromResult(await svc.GetSummaryAsync());
}
