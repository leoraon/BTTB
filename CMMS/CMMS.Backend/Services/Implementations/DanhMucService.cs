using CMMS.Backend.Entities;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class DanhMucService(CmmsDbContext db) : IDanhMucService
{
    public async Task<ApiResponse<List<DonViDto>>> GetDonViAsync(string? loaiDonVi, string? keyword)
    {
        var q = db.PHAN_XUONG_BO_PHAN.AsNoTracking()
            .Include(d => d.DonViCha)
            .Include(d => d.NguoiPhuTrach)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(loaiDonVi)) q = q.Where(d => d.LoaiDonVi == loaiDonVi);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(d => d.DonViId.Contains(kw) || d.TenDonVi.Contains(kw));
        }

        var list = await q.OrderBy(d => d.DonViId)
            .Select(d => new DonViDto
            {
                DonViId = d.DonViId,
                TenDonVi = d.TenDonVi,
                DonViChaId = d.DonViChaId,
                TenDonViCha = d.DonViCha != null ? d.DonViCha.TenDonVi : null,
                LoaiDonVi = d.LoaiDonVi,
                NguoiPhuTrachId = d.NguoiPhuTrachId,
                TenNguoiPhuTrach = d.NguoiPhuTrach != null ? d.NguoiPhuTrach.HoTen : null,
                MoTa = d.MoTa
            })
            .ToListAsync();

        return ApiResponse<List<DonViDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<DonViDto>>> GetDonViTreeAsync()
    {
        var all = (await GetDonViAsync(null, null)).Data ?? [];
        var map = all.ToDictionary(d => d.DonViId);
        var roots = new List<DonViDto>();

        foreach (var node in all)
        {
            if (!string.IsNullOrEmpty(node.DonViChaId) && map.TryGetValue(node.DonViChaId, out var parent))
                parent.DonViCon.Add(node);
            else
                roots.Add(node);
        }

        return ApiResponse<List<DonViDto>>.Ok(roots);
    }

    public async Task<ApiResponse<DonViDto>> CreateDonViAsync(DonViUpsertReq req)
    {
        var err = await ValidateDonViAsync(req, isCreate: true);
        if (err != null) return ApiResponse<DonViDto>.Fail(err);

        var entity = new PHAN_XUONG_BO_PHAN
        {
            DonViId = req.DonViId.Trim(),
            TenDonVi = req.TenDonVi.Trim(),
            DonViChaId = string.IsNullOrWhiteSpace(req.DonViChaId) ? null : req.DonViChaId.Trim(),
            LoaiDonVi = req.LoaiDonVi,
            NguoiPhuTrachId = string.IsNullOrWhiteSpace(req.NguoiPhuTrachId) ? null : req.NguoiPhuTrachId.Trim(),
            MoTa = req.MoTa?.Trim()
        };
        db.PHAN_XUONG_BO_PHAN.Add(entity);
        await db.SaveChangesAsync();
        return ApiResponse<DonViDto>.Ok(new DonViDto
        {
            DonViId = entity.DonViId,
            TenDonVi = entity.TenDonVi,
            DonViChaId = entity.DonViChaId,
            LoaiDonVi = entity.LoaiDonVi,
            NguoiPhuTrachId = entity.NguoiPhuTrachId,
            MoTa = entity.MoTa
        }, "Thêm đơn vị thành công.");
    }

    public async Task<ApiResponse<DonViDto>> UpdateDonViAsync(string donViId, DonViUpsertReq req)
    {
        var entity = await db.PHAN_XUONG_BO_PHAN.FindAsync(donViId);
        if (entity == null) return ApiResponse<DonViDto>.Fail("Không tìm thấy đơn vị.");

        req.DonViId = donViId;
        var err = await ValidateDonViAsync(req, isCreate: false);
        if (err != null) return ApiResponse<DonViDto>.Fail(err);

        // Chặn vòng lặp đệ quy cha - con
        if (!string.IsNullOrWhiteSpace(req.DonViChaId))
        {
            var cur = req.DonViChaId.Trim();
            var visited = new HashSet<string> { donViId };
            while (!string.IsNullOrEmpty(cur))
            {
                if (!visited.Add(cur))
                    return ApiResponse<DonViDto>.Fail("Cấu trúc đơn vị cha-con bị tạo vòng lặp.");
                cur = await db.PHAN_XUONG_BO_PHAN
                    .Where(d => d.DonViId == cur)
                    .Select(d => d.DonViChaId)
                    .FirstOrDefaultAsync();
            }
        }

        entity.TenDonVi = req.TenDonVi.Trim();
        entity.DonViChaId = string.IsNullOrWhiteSpace(req.DonViChaId) ? null : req.DonViChaId.Trim();
        entity.LoaiDonVi = req.LoaiDonVi;
        entity.NguoiPhuTrachId = string.IsNullOrWhiteSpace(req.NguoiPhuTrachId) ? null : req.NguoiPhuTrachId.Trim();
        entity.MoTa = req.MoTa?.Trim();
        await db.SaveChangesAsync();

        return ApiResponse<DonViDto>.Ok(new DonViDto
        {
            DonViId = entity.DonViId,
            TenDonVi = entity.TenDonVi,
            DonViChaId = entity.DonViChaId,
            LoaiDonVi = entity.LoaiDonVi,
            NguoiPhuTrachId = entity.NguoiPhuTrachId,
            MoTa = entity.MoTa
        }, "Cập nhật đơn vị thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteDonViAsync(string donViId)
    {
        var entity = await db.PHAN_XUONG_BO_PHAN.FindAsync(donViId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy đơn vị.");

        if (await db.PHAN_XUONG_BO_PHAN.AnyAsync(d => d.DonViChaId == donViId))
            return ApiResponse<bool>.Fail("Không thể xóa đơn vị khi còn dây chuyền/đơn vị con trực thuộc.");
        if (await db.NHAN_VIEN.AnyAsync(n => n.DonViId == donViId))
            return ApiResponse<bool>.Fail("Không thể xóa đơn vị đã có nhân viên trực thuộc.");
        if (await db.THIET_BI.AnyAsync(t => t.ViTriLapDatId == donViId || t.BoPhanQuanLyId == donViId))
            return ApiResponse<bool>.Fail("Không thể xóa đơn vị đang gắn với hồ sơ thiết bị.");

        db.PHAN_XUONG_BO_PHAN.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa đơn vị.");
    }

    public async Task<ApiResponse<List<NhomThietBiDto>>> GetNhomThietBiAsync(string? keyword)
    {
        var q = db.NHOM_THIET_BI.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(n => n.NhomThietBiId.Contains(kw) || n.TenNhom.Contains(kw));
        }

        var list = await q.OrderBy(n => n.LaCumChiTiet).ThenBy(n => n.NhomThietBiId)
            .Select(n => new NhomThietBiDto
            {
                NhomThietBiId = n.NhomThietBiId,
                TenNhom = n.TenNhom,
                LaCumChiTiet = n.LaCumChiTiet,
                MoTa = n.MoTa
            })
            .ToListAsync();

        return ApiResponse<List<NhomThietBiDto>>.Ok(list);
    }

    public async Task<ApiResponse<NhomThietBiDto>> CreateNhomThietBiAsync(NhomThietBiDto req)
    {
        if (string.IsNullOrWhiteSpace(req.NhomThietBiId) || string.IsNullOrWhiteSpace(req.TenNhom))
            return ApiResponse<NhomThietBiDto>.Fail("Mã và tên nhóm thiết bị không được trống.");
        if (await db.NHOM_THIET_BI.AnyAsync(n => n.NhomThietBiId == req.NhomThietBiId.Trim()))
            return ApiResponse<NhomThietBiDto>.Fail("Mã nhóm thiết bị đã tồn tại.");

        var entity = new NHOM_THIET_BI
        {
            NhomThietBiId = req.NhomThietBiId.Trim(),
            TenNhom = req.TenNhom.Trim(),
            LaCumChiTiet = req.LaCumChiTiet,
            MoTa = req.MoTa?.Trim()
        };
        db.NHOM_THIET_BI.Add(entity);
        await db.SaveChangesAsync();
        return ApiResponse<NhomThietBiDto>.Ok(req, "Thêm nhóm thiết bị thành công.");
    }

    public async Task<ApiResponse<NhomThietBiDto>> UpdateNhomThietBiAsync(string id, NhomThietBiDto req)
    {
        var entity = await db.NHOM_THIET_BI.FindAsync(id);
        if (entity == null) return ApiResponse<NhomThietBiDto>.Fail("Không tìm thấy nhóm thiết bị.");
        if (string.IsNullOrWhiteSpace(req.TenNhom))
            return ApiResponse<NhomThietBiDto>.Fail("Tên nhóm thiết bị không được trống.");

        if (entity.LaCumChiTiet != req.LaCumChiTiet && await db.THIET_BI.AnyAsync(t => t.NhomThietBiId == id))
            return ApiResponse<NhomThietBiDto>.Fail("Không thể thay đổi phân loại (Máy chính / Cụm chi tiết) của nhóm đã có thiết bị sử dụng.");

        entity.TenNhom = req.TenNhom.Trim();
        entity.LaCumChiTiet = req.LaCumChiTiet;
        entity.MoTa = req.MoTa?.Trim();
        await db.SaveChangesAsync();
        req.NhomThietBiId = id;
        return ApiResponse<NhomThietBiDto>.Ok(req, "Cập nhật nhóm thiết bị thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteNhomThietBiAsync(string id)
    {
        var entity = await db.NHOM_THIET_BI.FindAsync(id);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy nhóm thiết bị.");

        if (await db.THIET_BI.AnyAsync(t => t.NhomThietBiId == id)
            || await db.MAU_CHECKLIST.AnyAsync(m => m.NhomThietBiId == id)
            || await db.CHUYEN_MON_KTV.AnyAsync(c => c.NhomThietBiId == id))
            return ApiResponse<bool>.Fail("Không thể xóa nhóm thiết bị đã được sử dụng trong hồ sơ máy/checklist/KTV.");

        db.NHOM_THIET_BI.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa nhóm thiết bị.");
    }

    public async Task<ApiResponse<List<MaLoiDto>>> GetMaLoiAsync(string? keyword)
    {
        var q = db.DANH_MUC_MA_LOI.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(m => m.MaLoiId.Contains(kw) || m.TenLoaiSuCo.Contains(kw));
        }

        var list = await q.OrderBy(m => m.MaLoiId)
            .Select(m => new MaLoiDto
            {
                MaLoiId = m.MaLoiId,
                TenLoaiSuCo = m.TenLoaiSuCo,
                MoTaDauHieu = m.MoTaDauHieu
            })
            .ToListAsync();

        return ApiResponse<List<MaLoiDto>>.Ok(list);
    }

    public async Task<ApiResponse<MaLoiDto>> CreateMaLoiAsync(MaLoiDto req)
    {
        if (string.IsNullOrWhiteSpace(req.MaLoiId) || string.IsNullOrWhiteSpace(req.TenLoaiSuCo) || string.IsNullOrWhiteSpace(req.MoTaDauHieu))
            return ApiResponse<MaLoiDto>.Fail("Vui lòng nhập đầy đủ mã lỗi, tên loại sự cố và dấu hiệu nhận biết.");
        if (await db.DANH_MUC_MA_LOI.AnyAsync(m => m.MaLoiId == req.MaLoiId.Trim()))
            return ApiResponse<MaLoiDto>.Fail("Mã lỗi đã tồn tại.");

        db.DANH_MUC_MA_LOI.Add(new DANH_MUC_MA_LOI
        {
            MaLoiId = req.MaLoiId.Trim(),
            TenLoaiSuCo = req.TenLoaiSuCo.Trim(),
            MoTaDauHieu = req.MoTaDauHieu.Trim()
        });
        await db.SaveChangesAsync();
        return ApiResponse<MaLoiDto>.Ok(req, "Thêm mã lỗi chuẩn hóa thành công.");
    }

    public async Task<ApiResponse<MaLoiDto>> UpdateMaLoiAsync(string id, MaLoiDto req)
    {
        var entity = await db.DANH_MUC_MA_LOI.FindAsync(id);
        if (entity == null) return ApiResponse<MaLoiDto>.Fail("Không tìm thấy mã lỗi.");
        if (string.IsNullOrWhiteSpace(req.TenLoaiSuCo) || string.IsNullOrWhiteSpace(req.MoTaDauHieu))
            return ApiResponse<MaLoiDto>.Fail("Tên loại sự cố và mô tả dấu hiệu không được trống.");

        entity.TenLoaiSuCo = req.TenLoaiSuCo.Trim();
        entity.MoTaDauHieu = req.MoTaDauHieu.Trim();
        await db.SaveChangesAsync();
        req.MaLoiId = id;
        return ApiResponse<MaLoiDto>.Ok(req, "Cập nhật mã lỗi thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteMaLoiAsync(string id)
    {
        var entity = await db.DANH_MUC_MA_LOI.FindAsync(id);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy mã lỗi.");

        if (await db.YEU_CAU_BAO_TRI.AnyAsync(y => y.MaLoiId == id)
            || await db.BAO_CAO_SU_CO.AnyAsync(b => b.MaLoiChinhThucId == id))
            return ApiResponse<bool>.Fail("Không thể xóa mã lỗi đã được sử dụng trong yêu cầu hoặc báo cáo sự cố.");

        db.DANH_MUC_MA_LOI.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa mã lỗi.");
    }

    private async Task<string?> ValidateDonViAsync(DonViUpsertReq req, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(req.DonViId) || string.IsNullOrWhiteSpace(req.TenDonVi))
            return "Mã đơn vị và tên đơn vị không được trống.";

        if (req.LoaiDonVi is not (CmmsConstants.LoaiDonVi.PhanXuong or CmmsConstants.LoaiDonVi.DayChuyen or CmmsConstants.LoaiDonVi.BoPhanQuanLy))
            return "Loại đơn vị không hợp lệ.";

        var dvId = req.DonViId.Trim();
        if (isCreate && await db.PHAN_XUONG_BO_PHAN.AnyAsync(d => d.DonViId == dvId))
            return $"Mã đơn vị '{dvId}' đã tồn tại.";

        if (!string.IsNullOrWhiteSpace(req.DonViChaId))
        {
            var chaId = req.DonViChaId.Trim();
            if (chaId == dvId)
                return "Đơn vị không thể tự làm đơn vị cha của chính nó.";

            var cha = await db.PHAN_XUONG_BO_PHAN.AsNoTracking().FirstOrDefaultAsync(d => d.DonViId == chaId);
            if (cha == null)
                return "Đơn vị cha không tồn tại.";

            if (req.LoaiDonVi == CmmsConstants.LoaiDonVi.PhanXuong && cha.LoaiDonVi == CmmsConstants.LoaiDonVi.DayChuyen)
                return "Dây chuyền sản xuất không thể làm đơn vị cha của Phân xưởng.";
        }

        if (!string.IsNullOrWhiteSpace(req.NguoiPhuTrachId) && !await db.NHAN_VIEN.AnyAsync(n => n.NhanVienId == req.NguoiPhuTrachId.Trim()))
            return "Người phụ trách không tồn tại.";

        return null;
    }
}
