using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class BieuMauService(CmmsDbContext db, FileStorageHelper files) : IBieuMauService
{
    public async Task<ApiResponse<List<BieuMauDto>>> GetAllAsync()
    {
        var list = await db.THU_VIEN_BIEU_MAU.AsNoTracking()
            .OrderBy(b => b.BieuMauId)
            .Select(b => new BieuMauDto
            {
                BieuMauId = b.BieuMauId,
                TenBieuMau = b.TenBieuMau,
                LoaiBieuMau = b.LoaiBieuMau,
                DinhDangTep = b.DinhDangTep,
                DuongDanTep = b.DuongDanTep,
                MoTa = b.MoTa
            })
            .ToListAsync();

        return ApiResponse<List<BieuMauDto>>.Ok(list);
    }

    public async Task<(bool Found, byte[] Bytes, string ContentType, string FileName, string Error)> DownloadAsync(string bieuMauId)
    {
        var bm = await db.THU_VIEN_BIEU_MAU.AsNoTracking().FirstOrDefaultAsync(b => b.BieuMauId == bieuMauId);
        if (bm == null)
            return (false, [], string.Empty, string.Empty, "Không tìm thấy biểu mẫu.");

        var fullPath = files.ResolveExistingFile(bm.DuongDanTep, "bieu-mau", "templates");
        if (fullPath == null)
            return (false, [], string.Empty, string.Empty, $"Không tìm thấy tệp vật lý cho biểu mẫu '{bm.BieuMauId}'.");

        var bytes = await files.ReadFileBytesAsync(fullPath);
        var contentType = bm.DinhDangTep.Equals("PDF", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        return (true, bytes, contentType, Path.GetFileName(fullPath), string.Empty);
    }
}
