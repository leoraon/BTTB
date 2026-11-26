using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Backend.Controllers;

[Route("api/bieumau")]
[Authorize] // 1.4: Mở chung cho toàn bộ người dùng đăng nhập, không phân quyền tải riêng
public class BieuMauController(IBieuMauService svc) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => FromResult(await svc.GetAllAsync());

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id)
    {
        var res = await svc.DownloadAsync(id);
        if (!res.Found) return NotFound(ApiResponse<object>.Fail(res.Error));
        return File(res.Bytes, res.ContentType, res.FileName);
    }
}
