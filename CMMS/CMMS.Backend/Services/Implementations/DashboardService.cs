using CMMS.Backend.Entities;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class DashboardService(CmmsDbContext db, TimeProvider? timeProvider = null) : IDashboardService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync()
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now.Date);
        var dauThang = new DateTime(now.Year, now.Month, 1);
        var dauThangSau = dauThang.AddMonths(1);

        var allDevices = await db.THIET_BI.AsNoTracking()
            .Include(t => t.ThietBiCha)
            .Include(t => t.NhomThietBi)
            .Include(t => t.ViTriLapDat)
            .Include(t => t.BoPhanQuanLy)
            .ToListAsync();

        var activeDevices = allDevices.Where(t => t.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung).ToList();
        var activeDeviceIds = activeDevices.Select(t => t.ThietBiId).ToHashSet();

        var maintainedRaw = await db.LENH_BAO_TRI.AsNoTracking()
            .Where(l => l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaDong
                     && (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) >= dauThang
                     && (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) < dauThangSau)
            .Select(l => l.ThietBiId)
            .Distinct()
            .ToListAsync();
        var maintainedDeviceIds = maintainedRaw.Where(id => activeDeviceIds.Contains(id)).ToHashSet();

        var allOrders = await db.LENH_BAO_TRI.AsNoTracking()
            .Select(l => l.TrangThaiLenh)
            .ToListAsync();

        var coCauLenh = allOrders
            .GroupBy(s => s)
            .Select(g => new TrangThaiCountDto { TrangThai = g.Key, SoLuong = g.Count() })
            .OrderBy(x => x.TrangThai)
            .ToList();

        var incidentsThisMonth = await db.BAO_CAO_SU_CO.AsNoTracking()
            .Where(b => b.CoDungMay && b.ThoiDiemXayRa.HasValue && b.ThoiDiemKhoiPhuc.HasValue
                     && b.LenhBaoTri.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy
                     && b.ThoiDiemXayRa.Value >= dauThang
                     && b.ThoiDiemXayRa.Value < dauThangSau)
            .Select(b => new { Start = b.ThoiDiemXayRa!.Value, End = b.ThoiDiemKhoiPhuc!.Value })
            .ToListAsync();

        double totalDowntime = Math.Round(incidentsThisMonth.Sum(x => Math.Max(0, (x.End - x.Start).TotalHours)), 2);

        var keHoachs = await db.KE_HOACH_BAO_TRI.AsNoTracking()
            .Include(k => k.ThietBi)
            .Include(k => k.MauChecklist)
            .Include(k => k.LenhBaoTris)
            .Where(k => k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung
                     && k.ThietBi.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung)
            .ToListAsync();

        var overduePlans = keHoachs
            .Select(k => KeHoachService.MapDto(k, today))
            .Where(k => k.TrangThaiHan is "QuaHan_ChuaCoLenh" or "QuaHan_DangCoLenh")
            .ToList();

        var overdueRequestsRaw = await db.YEU_CAU_BAO_TRI.AsNoTracking()
            .Include(y => y.ThietBi)
            .Include(y => y.NguoiBao)
            .Include(y => y.MucUuTienDeXuat)
            .Include(y => y.MucUuTienChinhThuc)
            .Include(y => y.MaLoi)
            .Include(y => y.NguoiDuyet)
            .Where(y => y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.ChoTiepNhan && y.HanPhanHoi < now)
            .OrderBy(y => y.MucUuTienDeXuat.ThuTuUuTien)
            .ThenBy(y => y.HanPhanHoi)
            .ToListAsync();

        var parts = await db.PHU_TUNG.AsNoTracking().ToListAsync();
        var lowStockParts = parts
            .Where(p => p.SoLuongTonHienCo <= p.MucTonToiThieu)
            .Select(KhoService.MapPhuTungDto)
            .OrderBy(p => p.SoLuongTonHienCo)
            .ToList();

        var stoppedMachines = allDevices
            .Where(t => t.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.DungDoSuCo)
            .Select(ThietBiService.MapDto)
            .ToList();

        return ApiResponse<DashboardSummaryDto>.Ok(new DashboardSummaryDto
        {
            TongSoThietBi = allDevices.Count,
            SoMayDaBaoTriTrongThang = maintainedDeviceIds.Count,
            SoMayChuaBaoTriTrongThang = Math.Max(0, activeDevices.Count - maintainedDeviceIds.Count),
            SoMayQuaHanPM = overduePlans.Select(k => k.ThietBiId).Distinct().Count(),
            TongPhuTung = parts.Count,
            SoPhuTungDuoiToiThieu = lowStockParts.Count,
            TongGioDungMayTrongThang = totalDowntime,
            CoCauLenhTheoTrangThai = coCauLenh,
            MayDangDungSuCo = stoppedMachines,
            YeuCauQuaHanPhanHoi = overdueRequestsRaw.Select(y => YeuCauService.MapDto(y, now)).ToList(),
            KeHoachQuaHan = overduePlans,
            PhuTungDuoiToiThieu = lowStockParts
        });
    }
}
