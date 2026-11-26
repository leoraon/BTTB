using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/mauchecklist")]
[Authorize]
public class ChecklistController(IChecklistService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? nhomThietBiId, [FromQuery] string? trangThaiHieuLuc)
        => FromResult(await svc.GetAllAsync(nhomThietBiId, trangThaiHieuLuc));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
        => FromResult(await svc.GetByIdAsync(id));

    [HttpPost]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE)]
    public async Task<IActionResult> Create([FromBody] MauChecklistUpsertReq req)
        => FromResult(await svc.CreateAsync(req));

    [HttpPut("{id}")]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE)]
    public async Task<IActionResult> Update(string id, [FromBody] MauChecklistUpsertReq req)
        => FromResult(await svc.UpdateAsync(id, req));

    [HttpPut("{id}/ngung-ap-dung")]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE)]
    public async Task<IActionResult> NgungApDung(string id)
        => FromResult(await svc.NgungApDungAsync(id));

    [HttpDelete("{id}")]
    [RequirePermission(MaQuyen.CHECKLIST_MANAGE)]
    public async Task<IActionResult> Delete(string id)
        => FromResult(await svc.DeleteAsync(id));
}
