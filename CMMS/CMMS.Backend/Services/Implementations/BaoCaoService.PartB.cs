using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

/// <summary>
/// Phần B (TV B): Báo cáo BC2, BC3, BC6, BC7 (tính động chênh lệch dự trù - thực dùng &amp; tồn khả dụng).
/// </summary>
public partial class BaoCaoService
{
    public async Task<ApiResponse<List<ThietBiListDto>>> GetBc2DanhSachThietBiAsync(ReportFilterReq filter)
    {
        var q = db.THIET_BI.AsNoTracking()
            .Include(t => t.ThietBiCha)
            .Include(t => t.NhomThietBi)
            .Include(t => t.ViTriLapDat)
            .Include(t => t.BoPhanQuanLy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.ViTriLapDatId)) q = q.Where(t => t.ViTriLapDatId == filter.ViTriLapDatId);
        if (!string.IsNullOrWhiteSpace(filter.BoPhanQuanLyId)) q = q.Where(t => t.BoPhanQuanLyId == filter.BoPhanQuanLyId);
        if (!string.IsNullOrWhiteSpace(filter.NhomThietBiId)) q = q.Where(t => t.NhomThietBiId == filter.NhomThietBiId);
        if (!string.IsNullOrWhiteSpace(filter.TrangThaiVanHanh)) q = q.Where(t => t.TrangThaiVanHanh == filter.TrangThaiVanHanh);

        var items = await q.OrderBy(t => t.ThietBiId).ToListAsync();
        return ApiResponse<List<ThietBiListDto>>.Ok(items.Select(ThietBiService.MapDto).ToList());
    }

    public async Task<ApiResponse<List<Bc3TienDoPmItemDto>>> GetBc3TienDoPmAsync(ReportFilterReq filter)
    {
        if (!TryNormalizeRange(filter, out var start, out var end, out var rangeErr))
            return ApiResponse<List<Bc3TienDoPmItemDto>>.Fail(rangeErr!);

        var startDate = DateOnly.FromDateTime(start);
        var endDate = DateOnly.FromDateTime(end);
        var today = DateOnly.FromDateTime(LocalNow);

        var plans = await db.KE_HOACH_BAO_TRI.AsNoTracking()
            .Include(k => k.ThietBi)
            .Include(k => k.LenhBaoTris)
            .Where(k => k.TrangThaiKeHoach != CmmsConstants.TrangThaiKeHoach.NgungApDung)
            .ToListAsync();

        var result = new List<Bc3TienDoPmItemDto>();
        foreach (var kh in plans)
        {
            var closedInRange = kh.LenhBaoTris
                .Where(l => l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaDong
                         && (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) >= start
                         && (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) <= end)
                .OrderByDescending(l => l.ThoiDiemThucTeKetThuc)
                .ToList();

            foreach (var l in closedInRange)
            {
                var xong = l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc;
                result.Add(new Bc3TienDoPmItemDto
                {
                    KeHoachId = kh.KeHoachId,
                    TenKeHoach = kh.TenKeHoach,
                    ThietBiId = kh.ThietBiId,
                    TenThietBi = kh.ThietBi.TenThietBi,
                    NgayDenHanTiepTheo = DateOnly.FromDateTime(l.ThoiGianDuKienKetThuc),
                    LenhBaoTriId = l.LenhBaoTriId,
                    NhomKetQua = xong <= l.ThoiGianDuKienKetThuc ? "HoanThanhDungHan" : "HoanThanhTreHan",
                    ThoiDiemHoanThanh = xong
                });
            }

            if (kh.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung
                && kh.ThietBi.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung)
            {
                var openWo = kh.LenhBaoTris
                    .Where(l => l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy)
                    .OrderByDescending(l => l.ThoiGianDuKienBatDau)
                    .FirstOrDefault();
                var mocHan = openWo != null
                    ? DateOnly.FromDateTime(openWo.ThoiGianDuKienKetThuc)
                    : kh.NgayDenHanTiepTheo;

                if (mocHan <= endDate)
                {
                    if (today > mocHan)
                    {
                        result.Add(new Bc3TienDoPmItemDto
                        {
                            KeHoachId = kh.KeHoachId,
                            TenKeHoach = kh.TenKeHoach,
                            ThietBiId = kh.ThietBiId,
                            TenThietBi = kh.ThietBi.TenThietBi,
                            NgayDenHanTiepTheo = mocHan,
                            LenhBaoTriId = openWo?.LenhBaoTriId,
                            NhomKetQua = "DangQuaHan"
                        });
                    }
                    else if (mocHan >= startDate)
                    {
                        result.Add(new Bc3TienDoPmItemDto
                        {
                            KeHoachId = kh.KeHoachId,
                            TenKeHoach = kh.TenKeHoach,
                            ThietBiId = kh.ThietBiId,
                            TenThietBi = kh.ThietBi.TenThietBi,
                            NgayDenHanTiepTheo = mocHan,
                            LenhBaoTriId = openWo?.LenhBaoTriId,
                            NhomKetQua = "ChuaThucHien"
                        });
                    }
                }
            }
        }

        return ApiResponse<List<Bc3TienDoPmItemDto>>.Ok(result.OrderBy(r => r.NhomKetQua).ThenBy(r => r.NgayDenHanTiepTheo).ToList());
    }

    public async Task<ApiResponse<List<PhuTungDto>>> GetBc6TonKhoToiThieuAsync(ReportFilterReq filter)
    {
        var q = db.PHU_TUNG.AsNoTracking().AsQueryable();
        if (filter.ChiDuoiToiThieu)
            q = q.Where(p => p.SoLuongTonHienCo <= p.MucTonToiThieu);

        var items = await q.OrderBy(p => p.SoLuongTonHienCo - p.MucTonToiThieu).ToListAsync();
        return ApiResponse<List<PhuTungDto>>.Ok(items.Select(KhoService.MapPhuTungDto).ToList());
    }

    public async Task<ApiResponse<Bc7XuatTonDoiChieuDto>> GetBc7XuatTonDoiChieuAsync(ReportFilterReq filter)
    {
        if (!TryNormalizeRange(filter, out var start, out var end, out var rangeErr))
            return ApiResponse<Bc7XuatTonDoiChieuDto>.Fail(rangeErr!);

        var parts = await db.PHU_TUNG.AsNoTracking().ToListAsync();

        var stockAdds = await db.CHI_TIET_CAP_NHAT_TON_KHO.AsNoTracking()
            .Where(c => c.PhieuCapNhat.TrangThaiPhieu == CmmsConstants.TrangThaiCapNhatKho.DaXacNhan
                     && c.PhieuCapNhat.ThoiDiemXacNhan.HasValue
                     && c.PhieuCapNhat.ThoiDiemXacNhan.Value >= start && c.PhieuCapNhat.ThoiDiemXacNhan.Value <= end)
            .Select(c => new { c.PhuTungId, c.SoLuongThayDoi })
            .ToListAsync();

        var issuedDetails = await db.CHI_TIET_PHIEU_XUAT.AsNoTracking()
            .Include(c => c.PhuTung)
            .Include(c => c.PhieuXuat).ThenInclude(p => p.NguoiKhaiBao)
            .Where(c => c.PhieuXuat.TrangThaiPhieu == CmmsConstants.TrangThaiPhieuXuat.DaXacNhanTruTon
                     && c.PhieuXuat.ThoiDiemXacNhan.HasValue
                     && c.PhieuXuat.ThoiDiemXacNhan.Value >= start && c.PhieuXuat.ThoiDiemXacNhan.Value <= end)
            .ToListAsync();

        var addByPart = stockAdds.GroupBy(x => x.PhuTungId).ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuongThayDoi));
        var outByPart = issuedDetails.GroupBy(x => x.PhuTungId).ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuongThucDung ?? 0));

        var bienDong = parts.Select(p => new Bc7BienDongPhuTungDto
        {
            PhuTungId = p.PhuTungId,
            TenPhuTung = p.TenPhuTung,
            DonViTinh = p.DonViTinh,
            TongBoSung = addByPart.GetValueOrDefault(p.PhuTungId, 0),
            TongXuatDung = outByPart.GetValueOrDefault(p.PhuTungId, 0),
            TonCuoiKyHienCo = p.SoLuongTonHienCo
        }).ToList();

        var doiChieu = issuedDetails.Select(c => new Bc7DoiChieuLenhDto
        {
            PhieuXuatId = c.PhieuXuatId,
            LenhBaoTriId = c.PhieuXuat.LenhBaoTriId,
            PhuTungId = c.PhuTungId,
            TenPhuTung = c.PhuTung.TenPhuTung,
            NguoiKhaiBaoId = c.PhieuXuat.NguoiKhaiBaoId,
            TenNguoiKhaiBao = c.PhieuXuat.NguoiKhaiBao?.HoTen,
            SoLuongDuTru = c.SoLuongDuTru,
            SoLuongThucDung = c.SoLuongThucDung ?? 0,
            GhiChuGiaiTrinh = c.GhiChuGiaiTrinh
        })
        .OrderByDescending(x => Math.Abs(x.ChenhLech))
        .ThenBy(x => x.PhieuXuatId)
        .ToList();

        return ApiResponse<Bc7XuatTonDoiChieuDto>.Ok(new Bc7XuatTonDoiChieuDto
        {
            BienDongTheoPhuTung = bienDong,
            DoiChieuDuTruThucDung = doiChieu
        });
    }
}
