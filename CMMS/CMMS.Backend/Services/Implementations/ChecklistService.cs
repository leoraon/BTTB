using CMMS.Backend.Entities;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class ChecklistService(CmmsDbContext db) : IChecklistService
{
    public async Task<ApiResponse<List<MauChecklistDto>>> GetAllAsync(string? nhomThietBiId, string? trangThaiHieuLuc)
    {
        var q = db.MAU_CHECKLIST.AsNoTracking()
            .Include(m => m.NhomThietBi)
            .Include(m => m.HangMucs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nhomThietBiId)) q = q.Where(m => m.NhomThietBiId == nhomThietBiId);
        if (!string.IsNullOrWhiteSpace(trangThaiHieuLuc)) q = q.Where(m => m.TrangThaiHieuLuc == trangThaiHieuLuc);

        var items = await q.OrderBy(m => m.MauChecklistId).ToListAsync();
        var usedByPlan = await db.KE_HOACH_BAO_TRI.Select(k => k.MauChecklistId).Distinct().ToHashSetAsync();
        var usedByResult = await db.KET_QUA_CHECKLIST.Select(k => k.HangMuc.MauChecklistId).Distinct().ToHashSetAsync();

        var result = items.Select(m => MapDto(m, usedByPlan.Contains(m.MauChecklistId) || usedByResult.Contains(m.MauChecklistId))).ToList();
        return ApiResponse<List<MauChecklistDto>>.Ok(result);
    }

    public async Task<ApiResponse<MauChecklistDto>> GetByIdAsync(string mauChecklistId)
    {
        var entity = await db.MAU_CHECKLIST.AsNoTracking()
            .Include(m => m.NhomThietBi)
            .Include(m => m.HangMucs)
            .FirstOrDefaultAsync(m => m.MauChecklistId == mauChecklistId);

        if (entity == null) return ApiResponse<MauChecklistDto>.Fail("Không tìm thấy mẫu checklist.");
        bool locked = await IsLockedAsync(mauChecklistId);
        return ApiResponse<MauChecklistDto>.Ok(MapDto(entity, locked));
    }

    public async Task<ApiResponse<MauChecklistDto>> CreateAsync(MauChecklistUpsertReq req)
    {
        if (string.IsNullOrWhiteSpace(req.MauChecklistId) || string.IsNullOrWhiteSpace(req.TenMau))
            return ApiResponse<MauChecklistDto>.Fail("Mã và tên mẫu checklist không được trống.");
        if (await db.MAU_CHECKLIST.AnyAsync(m => m.MauChecklistId == req.MauChecklistId.Trim()))
            return ApiResponse<MauChecklistDto>.Fail($"Mã mẫu checklist '{req.MauChecklistId}' đã tồn tại.");
        if (!await db.NHOM_THIET_BI.AnyAsync(n => n.NhomThietBiId == req.NhomThietBiId))
            return ApiResponse<MauChecklistDto>.Fail("Nhóm thiết bị không tồn tại.");

        var itemErr = ValidateHangMucs(req.HangMuc);
        if (itemErr != null) return ApiResponse<MauChecklistDto>.Fail(itemErr);

        var entity = new MAU_CHECKLIST
        {
            MauChecklistId = req.MauChecklistId.Trim(),
            TenMau = req.TenMau.Trim(),
            NhomThietBiId = req.NhomThietBiId,
            MoTaTieuChuan = req.MoTaTieuChuan?.Trim(),
            TrangThaiHieuLuc = CmmsConstants.TrangThaiHieuLuc.DangApDung,
            HangMucs = req.HangMuc.Select(h => new HANG_MUC_CHECKLIST
            {
                NhomBoPhan = h.NhomBoPhan.Trim(),
                NoiDungCongViec = h.NoiDungCongViec.Trim(),
                KieuKetQua = h.KieuKetQua,
                TieuChuanYeuCau = h.TieuChuanYeuCau.Trim(),
                GiaTriMin = h.KieuKetQua == CmmsConstants.KieuKetQua.NhapSoDo ? h.GiaTriMin : null,
                GiaTriMax = h.KieuKetQua == CmmsConstants.KieuKetQua.NhapSoDo ? h.GiaTriMax : null,
                DonViDo = h.DonViDo?.Trim(),
                ThuTuThucHien = h.ThuTuThucHien
            }).ToList()
        };

        db.MAU_CHECKLIST.Add(entity);
        await db.SaveChangesAsync();
        return await GetByIdAsync(entity.MauChecklistId);
    }

    public async Task<ApiResponse<MauChecklistDto>> UpdateAsync(string mauChecklistId, MauChecklistUpsertReq req)
    {
        var entity = await db.MAU_CHECKLIST.Include(m => m.HangMucs).FirstOrDefaultAsync(m => m.MauChecklistId == mauChecklistId);
        if (entity == null) return ApiResponse<MauChecklistDto>.Fail("Không tìm thấy mẫu checklist.");
        if (entity.TrangThaiHieuLuc == CmmsConstants.TrangThaiHieuLuc.NgungApDung)
            return ApiResponse<MauChecklistDto>.Fail("Mẫu checklist đã ngừng áp dụng, không thể chỉnh sửa.");

        // 4.1.2: Đã áp dụng vào kế hoạch hoặc có kết quả kiểm tra -> khóa không cho sửa tiêu chuẩn/ngưỡng
        if (await IsLockedAsync(mauChecklistId))
            return ApiResponse<MauChecklistDto>.Fail("Mẫu checklist đã được đưa vào áp dụng nên bị khóa chỉnh sửa tiêu chuẩn và ngưỡng đo. Hãy ngừng áp dụng mẫu cũ và tạo mẫu mới.");

        if (string.IsNullOrWhiteSpace(req.TenMau))
            return ApiResponse<MauChecklistDto>.Fail("Tên mẫu checklist không được trống.");
        if (!await db.NHOM_THIET_BI.AnyAsync(n => n.NhomThietBiId == req.NhomThietBiId))
            return ApiResponse<MauChecklistDto>.Fail("Nhóm thiết bị không tồn tại.");

        var itemErr = ValidateHangMucs(req.HangMuc);
        if (itemErr != null) return ApiResponse<MauChecklistDto>.Fail(itemErr);

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            entity.TenMau = req.TenMau.Trim();
            entity.NhomThietBiId = req.NhomThietBiId;
            entity.MoTaTieuChuan = req.MoTaTieuChuan?.Trim();

            db.HANG_MUC_CHECKLIST.RemoveRange(entity.HangMucs);
            foreach (var h in req.HangMuc)
            {
                db.HANG_MUC_CHECKLIST.Add(new HANG_MUC_CHECKLIST
                {
                    MauChecklistId = mauChecklistId,
                    NhomBoPhan = h.NhomBoPhan.Trim(),
                    NoiDungCongViec = h.NoiDungCongViec.Trim(),
                    KieuKetQua = h.KieuKetQua,
                    TieuChuanYeuCau = h.TieuChuanYeuCau.Trim(),
                    GiaTriMin = h.KieuKetQua == CmmsConstants.KieuKetQua.NhapSoDo ? h.GiaTriMin : null,
                    GiaTriMax = h.KieuKetQua == CmmsConstants.KieuKetQua.NhapSoDo ? h.GiaTriMax : null,
                    DonViDo = h.DonViDo?.Trim(),
                    ThuTuThucHien = h.ThuTuThucHien
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return await GetByIdAsync(mauChecklistId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<MauChecklistDto>.Fail($"Lỗi cập nhật mẫu checklist: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> NgungApDungAsync(string mauChecklistId)
    {
        var entity = await db.MAU_CHECKLIST.FindAsync(mauChecklistId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy mẫu checklist.");

        int activePlans = await db.KE_HOACH_BAO_TRI.CountAsync(k =>
            k.MauChecklistId == mauChecklistId && k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung);

        entity.TrangThaiHieuLuc = CmmsConstants.TrangThaiHieuLuc.NgungApDung;
        await db.SaveChangesAsync();

        var warnings = activePlans > 0
            ? new List<string> { $"Cảnh báo: Có {activePlans} kế hoạch bảo trì đang áp dụng vẫn gắn mẫu checklist này. Vui lòng chuyển các kế hoạch sang mẫu mới." }
            : [];
        return ApiResponse<bool>.Ok(true, "Đã chuyển mẫu checklist sang trạng thái NgungApDung.", warnings);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string mauChecklistId)
    {
        var entity = await db.MAU_CHECKLIST.FindAsync(mauChecklistId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy mẫu checklist.");

        // 4.1.1: Không cho phép xóa mẫu đã từng được sử dụng để bảo toàn dữ liệu lịch sử
        if (await IsLockedAsync(mauChecklistId))
            return ApiResponse<bool>.Fail("Không thể xóa mẫu checklist đã từng được áp dụng. Vui lòng chuyển sang NgungApDung.");

        db.MAU_CHECKLIST.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa mẫu checklist.");
    }

    private async Task<bool> IsLockedAsync(string mauId)
    {
        if (await db.KE_HOACH_BAO_TRI.AnyAsync(k => k.MauChecklistId == mauId))
            return true;
        return await db.KET_QUA_CHECKLIST.AnyAsync(q => q.HangMuc.MauChecklistId == mauId);
    }

    private static string? ValidateHangMucs(List<HangMucChecklistReq> items)
    {
        if (items.Count == 0) return "Mẫu checklist phải có ít nhất 1 hạng mục kiểm tra.";
        foreach (var h in items)
        {
            if (string.IsNullOrWhiteSpace(h.NhomBoPhan) || string.IsNullOrWhiteSpace(h.NoiDungCongViec) || string.IsNullOrWhiteSpace(h.TieuChuanYeuCau))
                return "Mỗi hạng mục checklist phải có nhóm bộ phận, nội dung công việc và tiêu chuẩn yêu cầu.";
            if (h.ThuTuThucHien <= 0)
                return $"Hạng mục '{h.NoiDungCongViec}': Thứ tự thực hiện phải > 0.";
            if (h.KieuKetQua is not (CmmsConstants.KieuKetQua.DatKhongDat or CmmsConstants.KieuKetQua.NhapSoDo))
                return $"Kiểu kết quả '{h.KieuKetQua}' không hợp lệ.";
            if (h.KieuKetQua == CmmsConstants.KieuKetQua.NhapSoDo)
            {
                if (!h.GiaTriMin.HasValue && !h.GiaTriMax.HasValue)
                    return $"Hạng mục '{h.NoiDungCongViec}': Kiểu NhapSoDo bắt buộc phải nhập ít nhất ngưỡng Min hoặc ngưỡng Max.";
                if (h.GiaTriMin.HasValue && h.GiaTriMax.HasValue && h.GiaTriMin > h.GiaTriMax)
                    return $"Hạng mục '{h.NoiDungCongViec}': Ngưỡng Min ({h.GiaTriMin}) không được lớn hơn Max ({h.GiaTriMax}).";
            }
        }
        return null;
    }

    private static MauChecklistDto MapDto(MAU_CHECKLIST m, bool locked) => new()
    {
        MauChecklistId = m.MauChecklistId,
        TenMau = m.TenMau,
        NhomThietBiId = m.NhomThietBiId,
        TenNhomThietBi = m.NhomThietBi.TenNhom,
        MoTaTieuChuan = m.MoTaTieuChuan,
        TrangThaiHieuLuc = m.TrangThaiHieuLuc,
        DaKhoaChinhSua = locked,
        HangMuc = m.HangMucs.OrderBy(h => h.ThuTuThucHien).Select(h => new HangMucChecklistDto
        {
            HangMucId = h.HangMucId,
            MauChecklistId = h.MauChecklistId,
            NhomBoPhan = h.NhomBoPhan,
            NoiDungCongViec = h.NoiDungCongViec,
            KieuKetQua = h.KieuKetQua,
            TieuChuanYeuCau = h.TieuChuanYeuCau,
            GiaTriMin = h.GiaTriMin,
            GiaTriMax = h.GiaTriMax,
            DonViDo = h.DonViDo,
            ThuTuThucHien = h.ThuTuThucHien
        }).ToList()
    };
}
