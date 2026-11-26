using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class YeuCauService(
    CmmsDbContext db,
    FileStorageHelper files,
    TimeProvider? timeProvider = null) : IYeuCauService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;
    public async Task<ApiResponse<List<MucUuTienDto>>> GetMucUuTienAsync()
    {
        var list = await db.CAU_HINH_MUC_UU_TIEN.AsNoTracking()
            .OrderBy(m => m.ThuTuUuTien)
            .Select(m => new MucUuTienDto
            {
                MucUuTienId = m.MucUuTienId,
                TenMucUuTien = m.TenMucUuTien,
                ThuTuUuTien = m.ThuTuUuTien,
                MaMauHienThi = m.MaMauHienThi,
                ThoiHanPhanHoiPhut = m.ThoiHanPhanHoiPhut
            })
            .ToListAsync();
        return ApiResponse<List<MucUuTienDto>>.Ok(list);
    }

    public async Task<ApiResponse<MucUuTienDto>> UpdateMucUuTienAsync(int id, MucUuTienDto req)
    {
        var entity = await db.CAU_HINH_MUC_UU_TIEN.FindAsync(id);
        if (entity == null) return ApiResponse<MucUuTienDto>.Fail("Không tìm thấy cấu hình mức ưu tiên.");
        if (req.ThuTuUuTien <= 0)
            return ApiResponse<MucUuTienDto>.Fail("Thứ tự ưu tiên phải > 0.");
        if (string.IsNullOrWhiteSpace(req.TenMucUuTien) || string.IsNullOrWhiteSpace(req.MaMauHienThi) || req.ThoiHanPhanHoiPhut <= 0)
            return ApiResponse<MucUuTienDto>.Fail("Tên, mã màu và thời hạn phản hồi (> 0 phút) là bắt buộc.");

        if (await db.CAU_HINH_MUC_UU_TIEN.AnyAsync(m => m.MucUuTienId != id && (m.TenMucUuTien == req.TenMucUuTien.Trim() || m.ThuTuUuTien == req.ThuTuUuTien)))
            return ApiResponse<MucUuTienDto>.Fail("Tên hoặc thứ tự ưu tiên bị trùng.");

        entity.TenMucUuTien = req.TenMucUuTien.Trim();
        entity.ThuTuUuTien = req.ThuTuUuTien;
        entity.MaMauHienThi = req.MaMauHienThi.Trim();
        entity.ThoiHanPhanHoiPhut = req.ThoiHanPhanHoiPhut;
        await db.SaveChangesAsync();

        req.MucUuTienId = id;
        return ApiResponse<MucUuTienDto>.Ok(req, "Cập nhật mức ưu tiên SLA thành công.");
    }

    public async Task<ApiResponse<List<YeuCauBaoTriDto>>> GetDanhSachAsync(string? trangThai, string? nguoiBaoId, bool chiQuaHan)
    {
        var q = db.YEU_CAU_BAO_TRI.AsNoTracking()
            .Include(y => y.ThietBi)
            .Include(y => y.NguoiBao)
            .Include(y => y.MucUuTienDeXuat)
            .Include(y => y.MucUuTienChinhThuc)
            .Include(y => y.MaLoi)
            .Include(y => y.NguoiDuyet)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
            q = q.Where(y => y.TrangThaiYeuCau == trangThai);

        if (!string.IsNullOrWhiteSpace(nguoiBaoId))
            q = q.Where(y => y.NguoiBaoId == nguoiBaoId);

        var now = LocalNow;
        if (chiQuaHan)
            q = q.Where(y => y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.ChoTiepNhan && y.HanPhanHoi < now);

        var raw = await q.ToListAsync();
        var result = raw
            .Select(y => MapDto(y, now))
            .OrderBy(y => y.ThuTuUuTienHienThi)
            .ThenByDescending(y => y.IsQuaHanPhanHoi)
            .ThenByDescending(y => y.ThoiDiemGui)
            .ToList();

        return ApiResponse<List<YeuCauBaoTriDto>>.Ok(result);
    }

    public async Task<ApiResponse<YeuCauBaoTriDto>> GetByIdAsync(string yeuCauId)
    {
        var yc = await LoadEntityAsync(yeuCauId);
        if (yc == null) return ApiResponse<YeuCauBaoTriDto>.Fail("Không tìm thấy yêu cầu báo hỏng.");
        return ApiResponse<YeuCauBaoTriDto>.Ok(MapDto(yc, LocalNow));
    }

    public async Task<ApiResponse<YeuCauBaoTriDto>> TaoYeuCauAsync(TaoSuaYeuCauReq req, string nguoiBaoId)
    {
        if (string.IsNullOrWhiteSpace(req.MoTaTrieuChung))
            return ApiResponse<YeuCauBaoTriDto>.Fail("Vui lòng mô tả triệu chứng hư hỏng.");
        if (!IsValidRelativeAttachmentPath(req.DuongDanAnhSuCo))
            return ApiResponse<YeuCauBaoTriDto>.Fail("Đường dẫn ảnh sự cố không hợp lệ.");

        var tb = await db.THIET_BI.FindAsync(req.ThietBiId);
        if (tb == null || tb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            return ApiResponse<YeuCauBaoTriDto>.Fail("Thiết bị không tồn tại hoặc đã ngừng sử dụng.");

        var mucUuTien = await db.CAU_HINH_MUC_UU_TIEN.FindAsync(req.MucUuTienDeXuatId);
        if (mucUuTien == null) return ApiResponse<YeuCauBaoTriDto>.Fail("Mức ưu tiên đề xuất không hợp lệ.");

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var now = LocalNow;
            var maYc = await CodeGeneratorHelper.NextMonthlyCodeAsync(db.YEU_CAU_BAO_TRI.Select(y => y.YeuCauId), "YC", now);

            var entity = new YEU_CAU_BAO_TRI
            {
                YeuCauId = maYc,
                ThietBiId = req.ThietBiId,
                NguoiBaoId = nguoiBaoId,
                ThoiDiemGui = now,
                MoTaTrieuChung = req.MoTaTrieuChung.Trim(),
                DuongDanAnhSuCo = string.IsNullOrWhiteSpace(req.DuongDanAnhSuCo) ? null : req.DuongDanAnhSuCo.Trim(),
                MayDangDung = req.MayDangDung,
                MucUuTienDeXuatId = req.MucUuTienDeXuatId,
                HanPhanHoi = now.AddMinutes(mucUuTien.ThoiHanPhanHoiPhut),
                TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.ChoTiepNhan
            };

            db.YEU_CAU_BAO_TRI.Add(entity);
            if (req.MayDangDung && tb.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.DangBaoTri)
                tb.TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DungDoSuCo;

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            var created = await LoadEntityAsync(maYc);
            return ApiResponse<YeuCauBaoTriDto>.Ok(MapDto(created!, now), "Đã gửi yêu cầu báo hỏng thiết bị.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<YeuCauBaoTriDto>.Fail($"Lỗi khi tạo yêu cầu báo hỏng: {ex.Message}");
        }
    }

    public async Task<ApiResponse<YeuCauBaoTriDto>> SuaYeuCauAsync(
        string yeuCauId, TaoSuaYeuCauReq req, string nguoiSuaId, bool laTruongBp)
    {
        var yc = await db.YEU_CAU_BAO_TRI.FindAsync(yeuCauId);
        if (yc == null) return ApiResponse<YeuCauBaoTriDto>.Fail("Không tìm thấy yêu cầu báo hỏng.");
        if (yc.TrangThaiYeuCau != CmmsConstants.TrangThaiYeuCau.ChoTiepNhan)
            return ApiResponse<YeuCauBaoTriDto>.Fail("Chỉ được chỉnh sửa yêu cầu khi đang ở trạng thái ChoTiepNhan.");
        if (!laTruongBp && yc.NguoiBaoId != nguoiSuaId)
            return ApiResponse<YeuCauBaoTriDto>.Fail("Bạn chỉ được chỉnh sửa yêu cầu do chính mình gửi.");
        if (string.IsNullOrWhiteSpace(req.MoTaTrieuChung))
            return ApiResponse<YeuCauBaoTriDto>.Fail("Vui lòng nhập mô tả triệu chứng.");
        if (!IsValidRelativeAttachmentPath(req.DuongDanAnhSuCo))
            return ApiResponse<YeuCauBaoTriDto>.Fail("Đường dẫn ảnh sự cố không hợp lệ.");

        var mucUuTien = await db.CAU_HINH_MUC_UU_TIEN.FindAsync(req.MucUuTienDeXuatId);
        if (mucUuTien == null) return ApiResponse<YeuCauBaoTriDto>.Fail("Mức ưu tiên đề xuất không hợp lệ.");

        var newThietBiId = req.ThietBiId.Trim();
        var newTb = await db.THIET_BI.FindAsync(newThietBiId);
        if (newTb == null || newTb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            return ApiResponse<YeuCauBaoTriDto>.Fail("Thiết bị được chọn không tồn tại hoặc đã ngừng sử dụng.");

        string oldThietBiId = yc.ThietBiId;
        string? oldAnh = yc.DuongDanAnhSuCo;
        string? newAnh = string.IsNullOrWhiteSpace(req.DuongDanAnhSuCo) ? null : req.DuongDanAnhSuCo.Trim();

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            yc.ThietBiId = newThietBiId;
            yc.MoTaTrieuChung = req.MoTaTrieuChung.Trim();
            yc.DuongDanAnhSuCo = newAnh;
            yc.MayDangDung = req.MayDangDung;
            if (yc.MucUuTienDeXuatId != req.MucUuTienDeXuatId)
            {
                yc.MucUuTienDeXuatId = req.MucUuTienDeXuatId;
                yc.HanPhanHoi = yc.ThoiDiemGui.AddMinutes(mucUuTien.ThoiHanPhanHoiPhut);
            }

            if (oldThietBiId != newThietBiId)
            {
                var oldTb = await db.THIET_BI.FindAsync(oldThietBiId);
                if (oldTb != null && oldTb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.DungDoSuCo)
                    await RestoreMachineIfNoOtherStoppageAsync(oldTb, yeuCauId);
            }

            if (req.MayDangDung)
            {
                if (newTb.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.DangBaoTri)
                    newTb.TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DungDoSuCo;
            }
            else if (newTb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.DungDoSuCo)
                await RestoreMachineIfNoOtherStoppageAsync(newTb, yeuCauId);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            if (!string.IsNullOrEmpty(oldAnh) && !string.Equals(oldAnh, newAnh, StringComparison.OrdinalIgnoreCase))
                files.DeleteRelativeFile(oldAnh);

            var updated = await LoadEntityAsync(yeuCauId);
            return ApiResponse<YeuCauBaoTriDto>.Ok(MapDto(updated!, LocalNow), "Đã cập nhật yêu cầu báo hỏng.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<YeuCauBaoTriDto>.Fail($"Lỗi cập nhật yêu cầu: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> HuyYeuCauAsync(string yeuCauId, string nguoiHuyId, bool laTruongBp)
    {
        var yc = await db.YEU_CAU_BAO_TRI.FindAsync(yeuCauId);
        if (yc == null) return ApiResponse<bool>.Fail("Không tìm thấy yêu cầu báo hỏng.");
        if (yc.TrangThaiYeuCau != CmmsConstants.TrangThaiYeuCau.ChoTiepNhan)
            return ApiResponse<bool>.Fail("Chỉ được hủy yêu cầu đang ở trạng thái ChoTiepNhan.");
        if (!laTruongBp && yc.NguoiBaoId != nguoiHuyId)
            return ApiResponse<bool>.Fail("Bạn chỉ được hủy yêu cầu do chính mình gửi.");

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            yc.TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.DaHuy;
            var tb = await db.THIET_BI.FindAsync(yc.ThietBiId);
            if (tb != null && tb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.DungDoSuCo)
                await RestoreMachineIfNoOtherStoppageAsync(tb, yeuCauId);

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã hủy yêu cầu báo hỏng.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi hủy yêu cầu: {ex.Message}");
        }
    }

    public async Task<ApiResponse<YeuCauBaoTriDto>> DuyetHoacTuChoiAsync(
        string yeuCauId, DuyetYeuCauReq req, string nguoiDuyetId)
    {
        var yc = await db.YEU_CAU_BAO_TRI.Include(y => y.ThietBi).FirstOrDefaultAsync(y => y.YeuCauId == yeuCauId);
        if (yc == null) return ApiResponse<YeuCauBaoTriDto>.Fail("Không tìm thấy yêu cầu báo hỏng.");
        if (yc.TrangThaiYeuCau != CmmsConstants.TrangThaiYeuCau.ChoTiepNhan)
            return ApiResponse<YeuCauBaoTriDto>.Fail("Yêu cầu này đã được đánh giá/xử lý trước đó.");

        var now = LocalNow;
        yc.NguoiDuyetId = nguoiDuyetId;
        yc.ThoiDiemDuyet = now;

        if (req.LaPheDuyet)
        {
            if (yc.ThietBi.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
                return ApiResponse<YeuCauBaoTriDto>.Fail("Thiết bị đã ngừng sử dụng, không thể phê duyệt yêu cầu báo hỏng.");
            if (string.IsNullOrWhiteSpace(req.MaLoiId) || !req.MucUuTienChinhThucId.HasValue || string.IsNullOrWhiteSpace(req.HuongXuLy))
                return ApiResponse<YeuCauBaoTriDto>.Fail("Khi phê duyệt phải chọn Mã lỗi, Mức ưu tiên chính thức và Hướng xử lý.");

            if (!await db.DANH_MUC_MA_LOI.AnyAsync(m => m.MaLoiId == req.MaLoiId))
                return ApiResponse<YeuCauBaoTriDto>.Fail("Mã lỗi không tồn tại.");

            if (!await db.CAU_HINH_MUC_UU_TIEN.AnyAsync(u => u.MucUuTienId == req.MucUuTienChinhThucId.Value))
                return ApiResponse<YeuCauBaoTriDto>.Fail("Mức ưu tiên chính thức không hợp lệ.");

            if (req.HuongXuLy is not (CmmsConstants.HuongXuLy.SuaChuaKhanCap or CmmsConstants.HuongXuLy.SuaChuaTheoKeHoach))
                return ApiResponse<YeuCauBaoTriDto>.Fail("Hướng xử lý không hợp lệ.");

            yc.MaLoiId = req.MaLoiId;
            yc.NhanDinhNguyenNhanSoBo = req.NhanDinhNguyenNhanSoBo?.Trim();
            yc.MucUuTienChinhThucId = req.MucUuTienChinhThucId;
            yc.HuongXuLy = req.HuongXuLy;
            yc.TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.DaPheDuyet;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(req.LyDoTuChoi))
                return ApiResponse<YeuCauBaoTriDto>.Fail("Bắt buộc nhập lý do khi từ chối yêu cầu báo hỏng.");

            yc.LyDoTuChoi = req.LyDoTuChoi.Trim();
            yc.TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.TuChoi;
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            if (!req.LaPheDuyet)
            {
                var tb = await db.THIET_BI.FindAsync(yc.ThietBiId);
                if (tb != null && tb.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.DungDoSuCo)
                    await RestoreMachineIfNoOtherStoppageAsync(tb, yeuCauId);
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            var saved = await LoadEntityAsync(yeuCauId);
            return ApiResponse<YeuCauBaoTriDto>.Ok(MapDto(saved!, now), req.LaPheDuyet ? "Đã phê duyệt yêu cầu báo hỏng." : "Đã từ chối yêu cầu báo hỏng.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<YeuCauBaoTriDto>.Fail($"Lỗi duyệt/từ chối yêu cầu: {ex.Message}");
        }
    }

    public async Task<ApiResponse<string>> UploadAnhSuCoAsync(IFormFile file)
    {
        var saved = await files.SaveFileAsync(file, "suco_anh", "SC", [".jpg", ".jpeg", ".png", ".webp"]);
        return saved.Ok
            ? ApiResponse<string>.Ok(saved.MessageOrRelativePath, "Tải ảnh sự cố thành công.")
            : ApiResponse<string>.Fail(saved.MessageOrRelativePath);
    }

    public async Task<(bool Found, byte[] Bytes, string ContentType, string FileName)> DownloadAnhSuCoAsync(string yeuCauId)
    {
        var yc = await db.YEU_CAU_BAO_TRI.AsNoTracking().FirstOrDefaultAsync(y => y.YeuCauId == yeuCauId);
        if (yc == null || string.IsNullOrWhiteSpace(yc.DuongDanAnhSuCo))
            return (false, [], string.Empty, string.Empty);

        var fullPath = files.ResolveExistingFile(yc.DuongDanAnhSuCo, "uploads/suco_anh", "anh-su-co");
        if (fullPath == null)
            return (false, [], string.Empty, string.Empty);

        var bytes = await files.ReadFileBytesAsync(fullPath);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return (true, bytes, contentType, Path.GetFileName(fullPath));
    }

    private async Task RestoreMachineIfNoOtherStoppageAsync(THIET_BI tb, string excludeYeuCauId)
    {
        bool otherStoppingYc = await db.YEU_CAU_BAO_TRI.AnyAsync(y =>
            y.ThietBiId == tb.ThietBiId && y.YeuCauId != excludeYeuCauId
            && y.MayDangDung
            && (y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.ChoTiepNhan
                || y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DaPheDuyet
                || y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DangXuLy));

        bool activeWo = await db.LENH_BAO_TRI.AnyAsync(l =>
            l.ThietBiId == tb.ThietBiId
            && (l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien || l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.TamDung));

        if (!otherStoppingYc)
        {
            tb.TrangThaiVanHanh = activeWo
                ? CmmsConstants.TrangThaiThietBi.DangBaoTri
                : CmmsConstants.TrangThaiThietBi.DangHoatDong;
        }
    }

    private static bool IsValidRelativeAttachmentPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var p = path.Trim();
        if (p.IndexOf('\0') >= 0 || p.Contains(':') || p.StartsWith("//") || p.StartsWith(@"\\"))
            return false;

        var segs = p.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segs.Any(s => s is "." or ".."))
            return false;

        return !Path.IsPathRooted(p.TrimStart('/', '\\'));
    }

    private Task<YEU_CAU_BAO_TRI?> LoadEntityAsync(string id) => db.YEU_CAU_BAO_TRI.AsNoTracking()
        .Include(y => y.ThietBi)
        .Include(y => y.NguoiBao)
        .Include(y => y.MucUuTienDeXuat)
        .Include(y => y.MucUuTienChinhThuc)
        .Include(y => y.MaLoi)
        .Include(y => y.NguoiDuyet)
        .FirstOrDefaultAsync(y => y.YeuCauId == id);

    internal static YeuCauBaoTriDto MapDto(YEU_CAU_BAO_TRI y) => MapDto(y, null);

    internal static YeuCauBaoTriDto MapDto(YEU_CAU_BAO_TRI y, DateTime? now) => new()
    {
        YeuCauId = y.YeuCauId,
        ThietBiId = y.ThietBiId,
        TenThietBi = y.ThietBi.TenThietBi,
        NguoiBaoId = y.NguoiBaoId,
        TenNguoiBao = y.NguoiBao.HoTen,
        ThoiDiemGui = y.ThoiDiemGui,
        MoTaTrieuChung = y.MoTaTrieuChung,
        DuongDanAnhSuCo = y.DuongDanAnhSuCo,
        MayDangDung = y.MayDangDung,
        MucUuTienDeXuatId = y.MucUuTienDeXuatId,
        TenMucUuTienDeXuat = y.MucUuTienDeXuat.TenMucUuTien,
        MaMauDeXuat = y.MucUuTienDeXuat.MaMauHienThi,
        ThuTuUuTienHienThi = y.MucUuTienChinhThuc?.ThuTuUuTien ?? y.MucUuTienDeXuat.ThuTuUuTien,
        MucUuTienChinhThucId = y.MucUuTienChinhThucId,
        TenMucUuTienChinhThuc = y.MucUuTienChinhThuc?.TenMucUuTien,
        MaMauChinhThuc = y.MucUuTienChinhThuc?.MaMauHienThi,
        HanPhanHoi = y.HanPhanHoi,
        TrangThaiYeuCau = y.TrangThaiYeuCau,
        MaLoiId = y.MaLoiId,
        TenLoaiSuCo = y.MaLoi?.TenLoaiSuCo,
        NhanDinhNguyenNhanSoBo = y.NhanDinhNguyenNhanSoBo,
        HuongXuLy = y.HuongXuLy,
        NguoiDuyetId = y.NguoiDuyetId,
        TenNguoiDuyet = y.NguoiDuyet?.HoTen,
        ThoiDiemDuyet = y.ThoiDiemDuyet,
        LyDoTuChoi = y.LyDoTuChoi,
        IsQuaHanPhanHoi = y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.ChoTiepNhan && (now ?? DateTime.Now) > y.HanPhanHoi
    };
}
