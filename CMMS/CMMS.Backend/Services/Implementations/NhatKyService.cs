using CMMS.Backend.Entities;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class NhatKyService(CmmsDbContext db) : INhatKyService
{
    public async Task<ApiResponse<List<NhatKyDto>>> SearchAsync(NhatKyFilterReq filter)
    {
        if (filter.TuNgay.HasValue && filter.DenNgay.HasValue)
        {
            var denCheck = filter.DenNgay.Value.TimeOfDay == TimeSpan.Zero
                ? filter.DenNgay.Value.Date.AddDays(1).AddTicks(-1)
                : filter.DenNgay.Value;
            if (filter.TuNgay.Value.Date > filter.DenNgay.Value.Date || filter.TuNgay.Value > denCheck)
                return ApiResponse<List<NhatKyDto>>.Fail("Từ ngày không được lớn hơn đến ngày.");
        }

        var q = db.NHAT_KY_HE_THONG.AsNoTracking()
            .Include(n => n.TaiKhoan)
            .AsQueryable();

        if (filter.TaiKhoanId.HasValue)
            q = q.Where(n => n.TaiKhoanId == filter.TaiKhoanId.Value);

        if (!string.IsNullOrWhiteSpace(filter.LoaiThaoTac))
            q = q.Where(n => n.LoaiThaoTac == filter.LoaiThaoTac.Trim());

        if (filter.TuNgay.HasValue)
            q = q.Where(n => n.ThoiDiem >= filter.TuNgay.Value);

        if (filter.DenNgay.HasValue)
        {
            var den = filter.DenNgay.Value.TimeOfDay == TimeSpan.Zero
                ? filter.DenNgay.Value.Date.AddDays(1).AddTicks(-1)
                : filter.DenNgay.Value;
            q = q.Where(n => n.ThoiDiem <= den);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            q = q.Where(n => n.MoTaChiTiet.Contains(kw)
                          || (n.DoiTuongTacDong != null && n.DoiTuongTacDong.Contains(kw))
                          || (n.TaiKhoan != null && n.TaiKhoan.TenDangNhap.Contains(kw)));
        }

        var list = await q
            .OrderByDescending(n => n.ThoiDiem)
            .ThenByDescending(n => n.NhatKyId)
            .Take(500)
            .Select(n => new NhatKyDto
            {
                NhatKyId = n.NhatKyId,
                TaiKhoanId = n.TaiKhoanId,
                TenDangNhap = n.TaiKhoan != null ? n.TaiKhoan.TenDangNhap : null,
                ThoiDiem = n.ThoiDiem,
                LoaiThaoTac = n.LoaiThaoTac,
                DoiTuongTacDong = n.DoiTuongTacDong,
                MoTaChiTiet = n.MoTaChiTiet
            })
            .ToListAsync();

        return ApiResponse<List<NhatKyDto>>.Ok(list);
    }
}
