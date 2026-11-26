using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class PhanQuyenService(CmmsDbContext db, AuditLogHelper audit) : IPhanQuyenService
{
    public async Task<ApiResponse<List<VaiTroDto>>> GetAllVaiTrosAsync()
    {
        var list = await db.VAI_TRO.AsNoTracking()
            .Include(v => v.VAI_TRO_QUYEN)
            .OrderBy(v => v.VaiTroId)
            .Select(v => new VaiTroDto
            {
                VaiTroId = v.VaiTroId,
                TenVaiTro = v.TenVaiTro,
                MoTa = v.MoTa,
                DanhSachQuyen = v.VAI_TRO_QUYEN.Select(q => q.MaQuyen).OrderBy(x => x).ToList()
            })
            .ToListAsync();

        return ApiResponse<List<VaiTroDto>>.Ok(list);
    }

    public async Task<ApiResponse<VaiTroDto>> CreateVaiTroAsync(VaiTroUpsertReq req)
    {
        if (req.VaiTroId <= 0 || string.IsNullOrWhiteSpace(req.TenVaiTro))
            return ApiResponse<VaiTroDto>.Fail("Mã vai trò và tên vai trò không hợp lệ.");

        if (await db.VAI_TRO.AnyAsync(v => v.VaiTroId == req.VaiTroId || v.TenVaiTro == req.TenVaiTro.Trim()))
            return ApiResponse<VaiTroDto>.Fail("Mã vai trò hoặc tên vai trò đã tồn tại.");

        var vt = new VAI_TRO { VaiTroId = req.VaiTroId, TenVaiTro = req.TenVaiTro.Trim(), MoTa = req.MoTa?.Trim() };
        db.VAI_TRO.Add(vt);
        await db.SaveChangesAsync();
        return ApiResponse<VaiTroDto>.Ok(new VaiTroDto { VaiTroId = vt.VaiTroId, TenVaiTro = vt.TenVaiTro, MoTa = vt.MoTa }, "Thêm vai trò thành công.");
    }

    public async Task<ApiResponse<VaiTroDto>> UpdateVaiTroAsync(int vaiTroId, VaiTroUpsertReq req)
    {
        var vt = await db.VAI_TRO.Include(v => v.VAI_TRO_QUYEN).FirstOrDefaultAsync(v => v.VaiTroId == vaiTroId);
        if (vt == null) return ApiResponse<VaiTroDto>.Fail("Không tìm thấy vai trò.");

        if (string.IsNullOrWhiteSpace(req.TenVaiTro))
            return ApiResponse<VaiTroDto>.Fail("Tên vai trò không được để trống.");

        if (await db.VAI_TRO.AnyAsync(v => v.TenVaiTro == req.TenVaiTro.Trim() && v.VaiTroId != vaiTroId))
            return ApiResponse<VaiTroDto>.Fail("Tên vai trò đã tồn tại.");

        vt.TenVaiTro = req.TenVaiTro.Trim();
        vt.MoTa = req.MoTa?.Trim();
        await db.SaveChangesAsync();

        return ApiResponse<VaiTroDto>.Ok(new VaiTroDto
        {
            VaiTroId = vt.VaiTroId,
            TenVaiTro = vt.TenVaiTro,
            MoTa = vt.MoTa,
            DanhSachQuyen = vt.VAI_TRO_QUYEN.Select(q => q.MaQuyen).ToList()
        }, "Cập nhật vai trò thành công.");
    }

    public IReadOnlyList<MaQuyen.QuyenInfo> GetDanhMucMaQuyen() => MaQuyen.All;

    public async Task<ApiResponse<bool>> CapNhatQuyenVaiTroAsync(int vaiTroId, CapNhatQuyenVaiTroReq req, int actorTaiKhoanId)
    {
        var vt = await db.VAI_TRO.FindAsync(vaiTroId);
        if (vt == null) return ApiResponse<bool>.Fail("Không tìm thấy vai trò.");

        var codes = req.DanhSachQuyen.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct().ToList();
        var invalid = codes.Where(c => !MaQuyen.IsValid(c)).ToList();
        if (invalid.Count > 0)
            return ApiResponse<bool>.Fail($"Mã quyền không hợp lệ theo Enum hệ thống: {string.Join(", ", invalid)}");

        if (vaiTroId == CmmsConstants.VaiTroIds.QuanTriHeThong && !codes.Contains(MaQuyen.ROLE_MANAGE))
        {
            bool otherHasRoleManage = await db.VAI_TRO_QUYEN.AnyAsync(q =>
                q.VaiTroId != vaiTroId && q.MaQuyen == MaQuyen.ROLE_MANAGE);
            if (!otherHasRoleManage)
                return ApiResponse<bool>.Fail("Không thể thu hồi quyền ROLE_MANAGE khỏi vai trò Quản trị hệ thống duy nhất.");
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var oldPerms = db.VAI_TRO_QUYEN.Where(q => q.VaiTroId == vaiTroId);
            db.VAI_TRO_QUYEN.RemoveRange(oldPerms);

            foreach (var code in codes)
                db.VAI_TRO_QUYEN.Add(new VAI_TRO_QUYEN { VaiTroId = vaiTroId, MaQuyen = code });

            await db.SaveChangesAsync();
            await audit.WriteAsync(actorTaiKhoanId, CmmsConstants.LoaiThaoTacLog.ThayDoiPhanQuyen,
                $"VaiTro:{vaiTroId}", $"Cập nhật {codes.Count} quyền cho vai trò {vt.TenVaiTro}: {string.Join(", ", codes)}");

            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã cập nhật phân quyền cho vai trò.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi cập nhật phân quyền: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> GanVaiTroTaiKhoanAsync(int taiKhoanId, int vaiTroId, int actorTaiKhoanId)
    {
        var tk = await db.TAI_KHOAN.FindAsync(taiKhoanId);
        if (tk == null) return ApiResponse<bool>.Fail("Không tìm thấy tài khoản.");

        var vt = await db.VAI_TRO.FindAsync(vaiTroId);
        if (vt == null) return ApiResponse<bool>.Fail("Vai trò không tồn tại.");

        if (tk.VaiTroId != vaiTroId && tk.TrangThaiTaiKhoan == CmmsConstants.TrangThaiTaiKhoan.DangHoatDong)
        {
            bool oldHasRoleManage = await db.VAI_TRO_QUYEN.AnyAsync(q => q.VaiTroId == tk.VaiTroId && q.MaQuyen == MaQuyen.ROLE_MANAGE);
            bool newHasRoleManage = await db.VAI_TRO_QUYEN.AnyAsync(q => q.VaiTroId == vaiTroId && q.MaQuyen == MaQuyen.ROLE_MANAGE);
            if (oldHasRoleManage && !newHasRoleManage)
            {
                bool anotherActiveAdmin = await db.TAI_KHOAN.AnyAsync(t =>
                    t.TaiKhoanId != taiKhoanId
                    && t.TrangThaiTaiKhoan == CmmsConstants.TrangThaiTaiKhoan.DangHoatDong
                    && t.NhanVien.TrangThai == CmmsConstants.TrangThaiNhanVien.DangLamViec
                    && db.VAI_TRO_QUYEN.Any(q => q.VaiTroId == t.VaiTroId && q.MaQuyen == MaQuyen.ROLE_MANAGE));
                if (!anotherActiveAdmin)
                    return ApiResponse<bool>.Fail("Không thể chuyển vai trò của tài khoản quản trị hoạt động cuối cùng đang giữ quyền ROLE_MANAGE.");
            }
        }

        tk.VaiTroId = vaiTroId;
        await db.SaveChangesAsync();
        await audit.WriteAsync(actorTaiKhoanId, CmmsConstants.LoaiThaoTacLog.GanVaiTroTaiKhoan,
            tk.TenDangNhap, $"Gán vai trò '{vt.TenVaiTro}' cho tài khoản {tk.TenDangNhap}.");

        return ApiResponse<bool>.Ok(true, $"Đã gán vai trò '{vt.TenVaiTro}' cho tài khoản.");
    }
}
