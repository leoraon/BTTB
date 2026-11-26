using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class KeHoachService(
    CmmsDbContext db,
    AuditLogHelper audit,
    TimeProvider? timeProvider = null,
    PmSyncLock? syncLock = null) : IKeHoachService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly PmSyncLock _syncLock = syncLock ?? new PmSyncLock();

    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

    public async Task<ApiResponse<List<KeHoachBaoTriDto>>> GetAllAsync(string? thietBiId, string? trangThai, bool chiQuaHan)
    {
        var q = db.KE_HOACH_BAO_TRI.AsNoTracking()
            .Include(k => k.ThietBi)
            .Include(k => k.MauChecklist)
            .Include(k => k.LenhBaoTris)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(thietBiId)) q = q.Where(k => k.ThietBiId == thietBiId);
        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(k => k.TrangThaiKeHoach == trangThai);

        var items = await q.OrderBy(k => k.NgayDenHanTiepTheo).ToListAsync();
        var today = DateOnly.FromDateTime(LocalNow);

        var list = items.Select(k => MapDto(k, today)).ToList();
        if (chiQuaHan)
            list = list.Where(x => x.TrangThaiHan is "QuaHan_ChuaCoLenh" or "QuaHan_DangCoLenh").ToList();

        return ApiResponse<List<KeHoachBaoTriDto>>.Ok(list);
    }

    public async Task<ApiResponse<KeHoachBaoTriDto>> CreateAsync(KeHoachUpsertReq req)
    {
        var warnings = new List<string>();
        var err = await ValidateKeHoachAsync(req, isCreate: true, existing: null, warnings);
        if (err != null) return ApiResponse<KeHoachBaoTriDto>.Fail(err);

        var entity = new KE_HOACH_BAO_TRI
        {
            KeHoachId = req.KeHoachId.Trim(),
            TenKeHoach = req.TenKeHoach.Trim(),
            ThietBiId = req.ThietBiId,
            MauChecklistId = req.MauChecklistId,
            DonViChuKy = req.DonViChuKy,
            GiaTriChuKy = req.GiaTriChuKy,
            SoNgaySinhLenhTruoc = req.SoNgaySinhLenhTruoc,
            ThoiGianDuKienPhut = req.ThoiGianDuKienPhut,
            NgayBatDauApDung = req.NgayBatDauApDung,
            NgayDenHanTiepTheo = req.NgayDenHanTiepTheo ?? req.NgayBatDauApDung,
            TrangThaiKeHoach = req.TrangThaiKeHoach,
            MoTa = req.MoTa?.Trim()
        };
        db.KE_HOACH_BAO_TRI.Add(entity);
        await db.SaveChangesAsync();

        var saved = await db.KE_HOACH_BAO_TRI.AsNoTracking()
            .Include(k => k.ThietBi).Include(k => k.MauChecklist).Include(k => k.LenhBaoTris)
            .FirstAsync(k => k.KeHoachId == entity.KeHoachId);
        return ApiResponse<KeHoachBaoTriDto>.Ok(
            MapDto(saved, DateOnly.FromDateTime(LocalNow)),
            "Tạo kế hoạch bảo trì định kỳ thành công.",
            warnings);
    }

    public async Task<ApiResponse<KeHoachBaoTriDto>> UpdateAsync(string keHoachId, KeHoachUpsertReq req)
    {
        var entity = await db.KE_HOACH_BAO_TRI
            .Include(k => k.LenhBaoTris)
            .FirstOrDefaultAsync(k => k.KeHoachId == keHoachId);
        if (entity == null) return ApiResponse<KeHoachBaoTriDto>.Fail("Không tìm thấy kế hoạch bảo trì.");

        bool coLenhMo = entity.LenhBaoTris.Any(l =>
            l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong
            && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy);
        if (coLenhMo && (entity.ThietBiId != req.ThietBiId || entity.MauChecklistId != req.MauChecklistId))
            return ApiResponse<KeHoachBaoTriDto>.Fail("Không thể thay đổi thiết bị hoặc mẫu checklist khi kế hoạch đang có lệnh bảo trì mở.");

        req.KeHoachId = keHoachId;
        var warnings = new List<string>();
        var err = await ValidateKeHoachAsync(req, isCreate: false, existing: entity, warnings);
        if (err != null) return ApiResponse<KeHoachBaoTriDto>.Fail(err);

        bool cycleChanged = entity.DonViChuKy != req.DonViChuKy
            || entity.GiaTriChuKy != req.GiaTriChuKy
            || entity.NgayBatDauApDung != req.NgayBatDauApDung;

        if (req.NgayDenHanTiepTheo.HasValue)
        {
            entity.NgayDenHanTiepTheo = req.NgayDenHanTiepTheo.Value;
        }
        else if (entity.NgayHoanThanhGanNhat.HasValue && cycleChanged)
        {
            entity.NgayDenHanTiepTheo = MaintenanceScheduleHelper.AddCycle(
                entity.NgayHoanThanhGanNhat.Value,
                req.DonViChuKy,
                req.GiaTriChuKy,
                req.NgayBatDauApDung.Day);
            if (entity.NgayDenHanTiepTheo < req.NgayBatDauApDung)
                entity.NgayDenHanTiepTheo = req.NgayBatDauApDung;
        }
        else if (!entity.NgayHoanThanhGanNhat.HasValue && entity.NgayBatDauApDung != req.NgayBatDauApDung)
        {
            entity.NgayDenHanTiepTheo = req.NgayBatDauApDung;
        }

        entity.TenKeHoach = req.TenKeHoach.Trim();
        entity.ThietBiId = req.ThietBiId;
        entity.MauChecklistId = req.MauChecklistId;
        entity.DonViChuKy = req.DonViChuKy;
        entity.GiaTriChuKy = req.GiaTriChuKy;
        entity.SoNgaySinhLenhTruoc = req.SoNgaySinhLenhTruoc;
        entity.ThoiGianDuKienPhut = req.ThoiGianDuKienPhut;
        entity.NgayBatDauApDung = req.NgayBatDauApDung;
        entity.TrangThaiKeHoach = req.TrangThaiKeHoach;
        entity.MoTa = req.MoTa?.Trim();

        await db.SaveChangesAsync();
        var saved = await db.KE_HOACH_BAO_TRI.AsNoTracking()
            .Include(k => k.ThietBi).Include(k => k.MauChecklist).Include(k => k.LenhBaoTris)
            .FirstAsync(k => k.KeHoachId == keHoachId);
        return ApiResponse<KeHoachBaoTriDto>.Ok(
            MapDto(saved, DateOnly.FromDateTime(LocalNow)),
            "Cập nhật kế hoạch bảo trì thành công.",
            warnings);
    }

    public async Task<ApiResponse<bool>> UpdateTrangThaiAsync(string keHoachId, string trangThaiMoi)
    {
        if (trangThaiMoi is not (CmmsConstants.TrangThaiKeHoach.DangApDung or CmmsConstants.TrangThaiKeHoach.TamDung or CmmsConstants.TrangThaiKeHoach.NgungApDung))
            return ApiResponse<bool>.Fail("Trạng thái kế hoạch không hợp lệ.");

        var kh = await db.KE_HOACH_BAO_TRI
            .Include(k => k.ThietBi)
            .Include(k => k.MauChecklist)
            .FirstOrDefaultAsync(k => k.KeHoachId == keHoachId);
        if (kh == null) return ApiResponse<bool>.Fail("Không tìm thấy kế hoạch bảo trì.");

        if (trangThaiMoi == CmmsConstants.TrangThaiKeHoach.DangApDung)
        {
            if (kh.ThietBi.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
                return ApiResponse<bool>.Fail("Không thể kích hoạt kế hoạch bảo trì cho thiết bị đã ngừng sử dụng.");

            if (kh.MauChecklist.TrangThaiHieuLuc != CmmsConstants.TrangThaiHieuLuc.DangApDung)
                return ApiResponse<bool>.Fail("Mẫu checklist gắn với kế hoạch đã ngừng áp dụng, vui lòng cập nhật mẫu mới trước khi kích hoạt.");

            bool dupActive = await db.KE_HOACH_BAO_TRI.AnyAsync(k =>
                k.KeHoachId != keHoachId
                && k.ThietBiId == kh.ThietBiId
                && k.MauChecklistId == kh.MauChecklistId
                && k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung);
            if (dupActive)
                return ApiResponse<bool>.Fail("Thiết bị này đã có một kế hoạch đang áp dụng khác cho cùng mẫu checklist.");
        }

        kh.TrangThaiKeHoach = trangThaiMoi;
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, $"Đã chuyển kế hoạch sang trạng thái {trangThaiMoi}.");
    }

    public async Task<ApiResponse<List<LichSuBaoTriThietBiDto>>> GetLichSuKyAsync(string keHoachId)
    {
        if (!await db.KE_HOACH_BAO_TRI.AnyAsync(k => k.KeHoachId == keHoachId))
            return ApiResponse<List<LichSuBaoTriThietBiDto>>.Fail("Không tìm thấy kế hoạch bảo trì.");

        var list = await db.LENH_BAO_TRI.AsNoTracking()
            .Where(l => l.KeHoachId == keHoachId && l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaDong)
            .OrderByDescending(l => l.ThoiDiemThucTeKetThuc)
            .Select(l => new LichSuBaoTriThietBiDto
            {
                LenhBaoTriId = l.LenhBaoTriId,
                LoaiLenh = l.LoaiLenh,
                NoiDungCongViec = l.NoiDungCongViec,
                ThoiDiemThucTeBatDau = l.ThoiDiemThucTeBatDau,
                ThoiDiemThucTeKetThuc = l.ThoiDiemThucTeKetThuc,
                TrangThaiLenh = l.TrangThaiLenh,
                TinhTrangSauBaoTri = l.TinhTrangSauBaoTri,
                NhanXetNghiemThu = l.NhanXetNghiemThu
            })
            .ToListAsync();

        return ApiResponse<List<LichSuBaoTriThietBiDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<LichBaoTriDuKienItemDto>>> GetLichDuKienAsync(
        DateOnly tuNgay, DateOnly denNgay, string? thietBiId = null, string? nhomThietBiId = null)
    {
        if (tuNgay > denNgay)
            return ApiResponse<List<LichBaoTriDuKienItemDto>>.Fail("Từ ngày không được lớn hơn đến ngày.");
        if (denNgay.DayNumber - tuNgay.DayNumber > 731)
            return ApiResponse<List<LichBaoTriDuKienItemDto>>.Fail("Khoảng thời gian dự kiến lịch tối đa là 731 ngày (2 năm).");

        var q = db.KE_HOACH_BAO_TRI.AsNoTracking()
            .Include(k => k.ThietBi).ThenInclude(t => t.NhomThietBi)
            .Include(k => k.LenhBaoTris)
            .Where(k => k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung
                     && k.ThietBi.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung);

        if (!string.IsNullOrWhiteSpace(thietBiId)) q = q.Where(k => k.ThietBiId == thietBiId);
        if (!string.IsNullOrWhiteSpace(nhomThietBiId)) q = q.Where(k => k.ThietBi.NhomThietBiId == nhomThietBiId);

        var plans = await q.ToListAsync();
        var shiftsByGroup = await LoadQualifiedShiftsByGroupAsync();
        var today = DateOnly.FromDateTime(LocalNow);
        var items = new List<LichBaoTriDuKienItemDto>();

        foreach (var kh in plans)
        {
            var openWo = kh.LenhBaoTris
                .Where(l => l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy)
                .OrderByDescending(l => l.ThoiGianDuKienBatDau)
                .FirstOrDefault();

            var startTime = MaintenanceScheduleHelper.ResolveDefaultStartTime(
                shiftsByGroup.GetValueOrDefault(kh.ThietBi.NhomThietBiId, []));

            var mocKyHienTai = openWo != null
                ? DateOnly.FromDateTime(openWo.ThoiGianDuKienBatDau)
                : kh.NgayDenHanTiepTheo;
            var mocHan = openWo != null
                ? DateOnly.FromDateTime(openWo.ThoiGianDuKienKetThuc)
                : kh.NgayDenHanTiepTheo;

            bool isOverdue = today > mocHan;
            if ((mocKyHienTai >= tuNgay && mocKyHienTai <= denNgay) || (mocKyHienTai < tuNgay && isOverdue))
            {
                string trangThaiDuKien = isOverdue
                    ? "QuaHan"
                    : openWo != null
                        ? "DaCoLenh"
                        : today >= mocKyHienTai.AddDays(-kh.SoNgaySinhLenhTruoc)
                            ? "SapDenHan"
                            : "DuKienTuongLai";

                items.Add(new LichBaoTriDuKienItemDto
                {
                    KeHoachId = kh.KeHoachId,
                    TenKeHoach = kh.TenKeHoach,
                    ThietBiId = kh.ThietBiId,
                    TenThietBi = kh.ThietBi.TenThietBi,
                    NhomThietBiId = kh.ThietBi.NhomThietBiId,
                    TenNhomThietBi = kh.ThietBi.NhomThietBi.TenNhom,
                    DonViChuKy = kh.DonViChuKy,
                    GiaTriChuKy = kh.GiaTriChuKy,
                    NgayDuKien = mocKyHienTai,
                    NgaySinhLenhDuKien = mocKyHienTai.AddDays(-kh.SoNgaySinhLenhTruoc),
                    GioBatDauDuKien = openWo != null ? TimeOnly.FromDateTime(openWo.ThoiGianDuKienBatDau) : startTime,
                    ThoiGianDuKienPhut = kh.ThoiGianDuKienPhut,
                    LaKyHienTai = true,
                    LenhDangMoId = openWo?.LenhBaoTriId,
                    TrangThaiDuKien = trangThaiDuKien
                });
            }

            var cursor = mocHan < today ? today : mocHan;
            for (int iter = 0; iter < 1000; iter++)
            {
                cursor = MaintenanceScheduleHelper.AddCycle(
                    cursor, kh.DonViChuKy, kh.GiaTriChuKy, kh.NgayBatDauApDung.Day);
                if (cursor > denNgay) break;

                if (cursor >= tuNgay)
                {
                    items.Add(new LichBaoTriDuKienItemDto
                    {
                        KeHoachId = kh.KeHoachId,
                        TenKeHoach = kh.TenKeHoach,
                        ThietBiId = kh.ThietBiId,
                        TenThietBi = kh.ThietBi.TenThietBi,
                        NhomThietBiId = kh.ThietBi.NhomThietBiId,
                        TenNhomThietBi = kh.ThietBi.NhomThietBi.TenNhom,
                        DonViChuKy = kh.DonViChuKy,
                        GiaTriChuKy = kh.GiaTriChuKy,
                        NgayDuKien = cursor,
                        NgaySinhLenhDuKien = cursor.AddDays(-kh.SoNgaySinhLenhTruoc),
                        GioBatDauDuKien = startTime,
                        ThoiGianDuKienPhut = kh.ThoiGianDuKienPhut,
                        LaKyHienTai = false,
                        LenhDangMoId = null,
                        TrangThaiDuKien = "DuKienTuongLai"
                    });
                }
            }
        }

        var ordered = items
            .OrderBy(i => i.NgayDuKien)
            .ThenBy(i => i.ThietBiId)
            .ThenBy(i => i.KeHoachId)
            .ToList();
        return ApiResponse<List<LichBaoTriDuKienItemDto>>.Ok(ordered);
    }

    public async Task<ApiResponse<SyncWorkOrdersResultDto>> SyncWorkOrdersAsync(int? actorTaiKhoanId = null)
    {
        await _syncLock.Gate.WaitAsync();
        try
        {
            var now = LocalNow;
            var today = DateOnly.FromDateTime(now);

            var plans = await db.KE_HOACH_BAO_TRI
                .Include(k => k.ThietBi)
                .Include(k => k.LenhBaoTris)
                .Where(k => k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung)
                .ToListAsync();

            var shiftsByGroup = await LoadQualifiedShiftsByGroupAsync();
            bool hasPriority2 = await db.CAU_HINH_MUC_UU_TIEN.AnyAsync(u => u.MucUuTienId == 2);

            var head = $"WO-{now:yyyyMM}-";
            var existingCodes = await db.LENH_BAO_TRI
                .Where(l => l.LenhBaoTriId.StartsWith(head))
                .Select(l => l.LenhBaoTriId)
                .ToListAsync();

            int nextSeq = 0;
            foreach (var id in existingCodes)
                if (int.TryParse(id[head.Length..], out var n) && n > nextSeq) nextSeq = n;

            var newIds = new List<string>();
            foreach (var kh in plans)
            {
                if (kh.ThietBi.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
                    continue;

                var ngaySinhLenh = kh.NgayDenHanTiepTheo.AddDays(-kh.SoNgaySinhLenhTruoc);
                if (today < ngaySinhLenh)
                    continue;

                // 5.1.2: Đảm bảo mỗi kế hoạch chỉ tồn tại tối đa một lệnh đang mở tại cùng một thời điểm
                bool coLenhMo = kh.LenhBaoTris.Any(l =>
                    l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong
                    && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy);
                if (coLenhMo)
                    continue;

                nextSeq++;
                var woId = $"{head}{nextSeq:D3}";
                var startTime = MaintenanceScheduleHelper.ResolveDefaultStartTime(
                    shiftsByGroup.GetValueOrDefault(kh.ThietBi.NhomThietBiId, []));
                var batDau = kh.NgayDenHanTiepTheo.ToDateTime(startTime);
                int mucUuTienId = today > kh.NgayDenHanTiepTheo && hasPriority2 ? 2 : 3;

                db.LENH_BAO_TRI.Add(new LENH_BAO_TRI
                {
                    LenhBaoTriId = woId,
                    ThietBiId = kh.ThietBiId,
                    LoaiLenh = CmmsConstants.LoaiLenh.BaoTriPhongNgua,
                    KeHoachId = kh.KeHoachId,
                    NoiDungCongViec = $"Bảo trì định kỳ theo kế hoạch: {kh.TenKeHoach}",
                    MucUuTienId = mucUuTienId,
                    ThoiGianDuKienBatDau = batDau,
                    ThoiGianDuKienKetThuc = batDau.AddMinutes(kh.ThoiGianDuKienPhut),
                    TrangThaiLenh = CmmsConstants.TrangThaiLenh.MoiTao,
                    NguoiTaoId = null
                });
                newIds.Add(woId);
            }

            if (newIds.Count > 0)
            {
                using var tx = await db.Database.BeginTransactionAsync();
                try
                {
                    await db.SaveChangesAsync();
                    await audit.WriteAsync(
                        actorTaiKhoanId > 0 ? actorTaiKhoanId : null,
                        CmmsConstants.LoaiThaoTacLog.PhatLenhBaoTri,
                        string.Join(", ", newIds),
                        $"Đồng bộ kế hoạch bảo trì định kỳ, tự động sinh {newIds.Count} lệnh bảo trì mới.");
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }

            return ApiResponse<SyncWorkOrdersResultDto>.Ok(
                new SyncWorkOrdersResultDto { SoLenhMoiSinh = newIds.Count, DanhSachMaLenhMoi = newIds },
                $"Đã đồng bộ kế hoạch: tự động sinh {newIds.Count} lệnh bảo trì định kỳ.");
        }
        finally
        {
            _syncLock.Gate.Release();
        }
    }

    private async Task<Dictionary<string, List<string?>>> LoadQualifiedShiftsByGroupAsync()
    {
        var rows = await db.CHUYEN_MON_KTV.AsNoTracking()
            .Include(c => c.NhanVien)
            .Where(c => c.NhanVien.TrangThai == CmmsConstants.TrangThaiNhanVien.DangLamViec
                     && c.NhanVien.TrangThaiSanSang != CmmsConstants.TrangThaiSanSang.NghiPhep)
            .Select(c => new { c.NhomThietBiId, c.NhanVien.CaLamViec })
            .ToListAsync();

        return rows
            .GroupBy(x => x.NhomThietBiId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CaLamViec).ToList());
    }

    private async Task<string?> ValidateKeHoachAsync(
        KeHoachUpsertReq req, bool isCreate, KE_HOACH_BAO_TRI? existing, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(req.KeHoachId) || string.IsNullOrWhiteSpace(req.TenKeHoach))
            return "Mã và tên kế hoạch không được để trống.";
        var khId = req.KeHoachId.Trim();
        if (isCreate && await db.KE_HOACH_BAO_TRI.AnyAsync(k => k.KeHoachId == khId))
            return $"Mã kế hoạch '{khId}' đã tồn tại.";

        if (req.TrangThaiKeHoach is not (CmmsConstants.TrangThaiKeHoach.DangApDung or CmmsConstants.TrangThaiKeHoach.TamDung or CmmsConstants.TrangThaiKeHoach.NgungApDung))
            return "Trạng thái kế hoạch không hợp lệ.";

        var tb = await db.THIET_BI.FindAsync(req.ThietBiId);
        if (tb == null || tb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            return "Thiết bị không tồn tại hoặc đã ngừng sử dụng.";

        var mau = await db.MAU_CHECKLIST.FindAsync(req.MauChecklistId);
        if (mau == null)
            return "Mẫu checklist không tồn tại.";
        if (mau.TrangThaiHieuLuc != CmmsConstants.TrangThaiHieuLuc.DangApDung
            && (isCreate || existing?.MauChecklistId != req.MauChecklistId || req.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung))
            return "Mẫu checklist đã ngừng áp dụng, vui lòng chọn mẫu đang hiệu lực.";

        if (mau.NhomThietBiId != tb.NhomThietBiId)
            return $"Mẫu checklist '{mau.TenMau}' thuộc nhóm '{mau.NhomThietBiId}', không khớp với nhóm của thiết bị ('{tb.NhomThietBiId}').";

        if (req.DonViChuKy is not (CmmsConstants.DonViChuKy.Ngay or CmmsConstants.DonViChuKy.Tuan
            or CmmsConstants.DonViChuKy.Thang or CmmsConstants.DonViChuKy.Quy or CmmsConstants.DonViChuKy.Nam))
            return "Đơn vị chu kỳ không hợp lệ.";

        if (req.GiaTriChuKy <= 0 || req.SoNgaySinhLenhTruoc < 0 || req.ThoiGianDuKienPhut <= 0)
            return "Giá trị chu kỳ, thời gian dự kiến phải > 0 và số ngày sinh lệnh trước hạn >= 0.";

        if (req.NgayBatDauApDung == default)
            return "Ngày bắt đầu áp dụng kế hoạch không hợp lệ.";

        int estDays = MaintenanceScheduleHelper.EstimateCycleDays(req.DonViChuKy, req.GiaTriChuKy);
        if (req.SoNgaySinhLenhTruoc > estDays)
            return $"Số ngày sinh lệnh trước hạn ({req.SoNgaySinhLenhTruoc} ngày) không được vượt quá độ dài chu kỳ (~{estDays} ngày).";

        if (req.NgayDenHanTiepTheo.HasValue)
        {
            if (req.NgayDenHanTiepTheo.Value < req.NgayBatDauApDung)
                return "Ngày đến hạn tiếp theo không được trước ngày bắt đầu áp dụng.";
            if (!isCreate && existing?.NgayHoanThanhGanNhat.HasValue == true && req.NgayDenHanTiepTheo.Value < existing.NgayHoanThanhGanNhat.Value)
                return "Ngày đến hạn tiếp theo không được trước ngày hoàn thành gần nhất.";
        }

        if (req.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung)
        {
            bool dupActive = await db.KE_HOACH_BAO_TRI.AnyAsync(k =>
                k.KeHoachId != khId
                && k.ThietBiId == req.ThietBiId
                && k.MauChecklistId == req.MauChecklistId
                && k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung);
            if (dupActive)
                return $"Thiết bị '{req.ThietBiId}' đã có một kế hoạch đang áp dụng khác sử dụng mẫu checklist '{req.MauChecklistId}'.";
        }

        if (estDays > tb.ChuKyBaoTriKhuyenNghiNgay)
            warnings.Add($"Cảnh báo: Chu kỳ kế hoạch (~{estDays} ngày) dài hơn chu kỳ bảo trì khuyến nghị của thiết bị ({tb.ChuKyBaoTriKhuyenNghiNgay} ngày).");

        return null;
    }

    internal static KeHoachBaoTriDto MapDto(KE_HOACH_BAO_TRI k, DateOnly today)
    {
        var openWo = k.LenhBaoTris
            .Where(l => l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy)
            .OrderByDescending(l => l.ThoiGianDuKienBatDau)
            .FirstOrDefault();

        // 4.3.2: Phân biệt quá hạn chưa có lệnh vs đang có lệnh (đối chiếu theo mốc dự kiến mới nhất của lệnh mở)
        DateOnly mocHan = openWo != null
            ? DateOnly.FromDateTime(openWo.ThoiGianDuKienKetThuc)
            : k.NgayDenHanTiepTheo;

        string trangThaiHan = "BinhThuong";
        if (k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung
            && k.ThietBi.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung)
        {
            if (today > mocHan)
                trangThaiHan = openWo != null ? "QuaHan_DangCoLenh" : "QuaHan_ChuaCoLenh";
            else if (today >= mocHan.AddDays(-k.SoNgaySinhLenhTruoc))
                trangThaiHan = "SapDenHan";
        }

        return new KeHoachBaoTriDto
        {
            KeHoachId = k.KeHoachId,
            TenKeHoach = k.TenKeHoach,
            ThietBiId = k.ThietBiId,
            TenThietBi = k.ThietBi.TenThietBi,
            MauChecklistId = k.MauChecklistId,
            TenMauChecklist = k.MauChecklist.TenMau,
            DonViChuKy = k.DonViChuKy,
            GiaTriChuKy = k.GiaTriChuKy,
            SoNgaySinhLenhTruoc = k.SoNgaySinhLenhTruoc,
            ThoiGianDuKienPhut = k.ThoiGianDuKienPhut,
            NgayBatDauApDung = k.NgayBatDauApDung,
            NgayHoanThanhGanNhat = k.NgayHoanThanhGanNhat,
            NgayDenHanTiepTheo = k.NgayDenHanTiepTheo,
            TrangThaiKeHoach = k.TrangThaiKeHoach,
            MoTa = k.MoTa,
            LenhDangMoId = openWo?.LenhBaoTriId,
            MocHanMoiNhat = mocHan,
            TrangThaiHan = trangThaiHan
        };
    }
}
