using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Helpers.Exporters;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class ThietBiService(
    CmmsDbContext db,
    FileStorageHelper files,
    IEnumerable<IReportExporter> exporters,
    TimeProvider? timeProvider = null) : IThietBiService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;
    public async Task<ApiResponse<List<ThietBiListDto>>> SearchAsync(
        string? keyword, string? viTriId, string? boPhanId, string? nhomId, string? trangThai)
    {
        var q = db.THIET_BI.AsNoTracking()
            .Include(t => t.ThietBiCha)
            .Include(t => t.NhomThietBi)
            .Include(t => t.ViTriLapDat)
            .Include(t => t.BoPhanQuanLy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(t => t.ThietBiId.Contains(kw) || t.TenThietBi.Contains(kw) || t.Serial.Contains(kw) || t.Model.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(viTriId)) q = q.Where(t => t.ViTriLapDatId == viTriId);
        if (!string.IsNullOrWhiteSpace(boPhanId)) q = q.Where(t => t.BoPhanQuanLyId == boPhanId);
        if (!string.IsNullOrWhiteSpace(nhomId)) q = q.Where(t => t.NhomThietBiId == nhomId);
        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(t => t.TrangThaiVanHanh == trangThai);

        var items = await q.OrderBy(t => t.ThietBiChaId != null).ThenBy(t => t.ThietBiId).ToListAsync();
        return ApiResponse<List<ThietBiListDto>>.Ok(items.Select(MapDto).ToList());
    }

    public async Task<ApiResponse<List<ThietBiListDto>>> GetTreeAsync(
        string? keyword, string? viTriId, string? boPhanId, string? nhomId, string? trangThai)
    {
        var list = (await SearchAsync(keyword, viTriId, boPhanId, nhomId, trangThai)).Data ?? [];
        var map = list.ToDictionary(t => t.ThietBiId);
        var roots = new List<ThietBiListDto>();

        foreach (var tb in list)
        {
            if (!string.IsNullOrEmpty(tb.ThietBiChaId) && map.TryGetValue(tb.ThietBiChaId, out var parent))
                parent.CumCon.Add(tb);
            else
                roots.Add(tb);
        }

        return ApiResponse<List<ThietBiListDto>>.Ok(roots);
    }

    public async Task<ApiResponse<PhieuLyLichThietBiDto>> GetPhieuLyLichAsync(string thietBiId)
    {
        var tb = await db.THIET_BI.AsNoTracking()
            .Include(t => t.ThietBiCha)
            .Include(t => t.NhomThietBi)
            .Include(t => t.ViTriLapDat)
            .Include(t => t.BoPhanQuanLy)
            .Include(t => t.CumCon).ThenInclude(c => c.NhomThietBi)
            .Include(t => t.CumCon).ThenInclude(c => c.ViTriLapDat)
            .Include(t => t.CumCon).ThenInclude(c => c.BoPhanQuanLy)
            .FirstOrDefaultAsync(t => t.ThietBiId == thietBiId);

        if (tb == null) return ApiResponse<PhieuLyLichThietBiDto>.Fail("Không tìm thấy thiết bị.");

        var dto = MapDto(tb);
        dto.CumCon = tb.CumCon.Select(MapDto).ToList();

        var specs = (await GetThongSoAsync(thietBiId)).Data ?? [];
        var boms = await db.DINH_MUC_LINH_KIEN.AsNoTracking()
            .Include(d => d.PhuTung)
            .Where(d => d.ThietBiId == thietBiId)
            .Select(d => new DinhMucLinhKienDto
            {
                DinhMucId = d.DinhMucId,
                ThietBiId = d.ThietBiId,
                PhuTungId = d.PhuTungId,
                TenPhuTung = d.PhuTung.TenPhuTung,
                DonViTinh = d.PhuTung.DonViTinh,
                SoLuongDinhMuc = d.SoLuongDinhMuc,
                ThayTheDinhKy = d.ThayTheDinhKy,
                ViTriLapDat = d.ViTriLapDat,
                TonKhoKhaDung = d.PhuTung.SoLuongTonHienCo - d.PhuTung.SoLuongDangDuTru
            })
            .ToListAsync();
        var history = (await GetLichSuBaoTriAsync(thietBiId)).Data ?? [];

        return ApiResponse<PhieuLyLichThietBiDto>.Ok(new PhieuLyLichThietBiDto
        {
            ThongTinChung = dto,
            CongDungMoTa = tb.CongDungMoTa,
            LuuYAnToan = tb.LuuYAnToan,
            ThongSoKyThuat = specs,
            DinhMucLinhKien = boms,
            LichSuBaoTri = history
        });
    }

    public async Task<ApiResponse<ThietBiListDto>> CreateAsync(ThietBiUpsertReq req)
    {
        var err = await ValidateThietBiAsync(req, isCreate: true);
        if (err != null) return ApiResponse<ThietBiListDto>.Fail(err);

        var entity = new THIET_BI
        {
            ThietBiId = req.ThietBiId.Trim(),
            TenThietBi = req.TenThietBi.Trim(),
            ThietBiChaId = string.IsNullOrWhiteSpace(req.ThietBiChaId) ? null : req.ThietBiChaId.Trim(),
            NhomThietBiId = req.NhomThietBiId.Trim(),
            ViTriLapDatId = req.ViTriLapDatId.Trim(),
            BoPhanQuanLyId = req.BoPhanQuanLyId.Trim(),
            Model = req.Model.Trim(),
            Serial = req.Serial.Trim(),
            NhaSanXuat = req.NhaSanXuat.Trim(),
            NgayDuaVaoSuDung = req.NgayDuaVaoSuDung,
            ChuKyBaoTriKhuyenNghiNgay = req.ChuKyBaoTriKhuyenNghiNgay,
            CongDungMoTa = req.CongDungMoTa.Trim(),
            LuuYAnToan = req.LuuYAnToan?.Trim(),
            TrangThaiVanHanh = req.TrangThaiVanHanh
        };
        db.THIET_BI.Add(entity);
        await db.SaveChangesAsync();
        return await LoadSingleDtoAsync(entity.ThietBiId, "Thêm hồ sơ thiết bị thành công.");
    }

    public async Task<ApiResponse<ThietBiListDto>> UpdateAsync(string thietBiId, ThietBiUpsertReq req)
    {
        var entity = await db.THIET_BI.FindAsync(thietBiId);
        if (entity == null) return ApiResponse<ThietBiListDto>.Fail("Không tìm thấy thiết bị.");

        req.ThietBiId = thietBiId;
        var err = await ValidateThietBiAsync(req, isCreate: false);
        if (err != null) return ApiResponse<ThietBiListDto>.Fail(err);

        var newNhomId = req.NhomThietBiId.Trim();
        if (entity.NhomThietBiId != newNhomId)
        {
            bool hasActivePlanOrWo = await db.KE_HOACH_BAO_TRI.AnyAsync(k =>
                    k.ThietBiId == thietBiId
                    && (k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung
                        || k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.TamDung))
                || await db.LENH_BAO_TRI.AnyAsync(l =>
                    l.ThietBiId == thietBiId
                    && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong
                    && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy);
            if (hasActivePlanOrWo)
                return ApiResponse<ThietBiListDto>.Fail("Không thể đổi nhóm thiết bị khi máy đang có kế hoạch bảo trì hiệu lực hoặc lệnh bảo trì chưa đóng.");
        }

        if (entity.TrangThaiVanHanh != req.TrangThaiVanHanh)
        {
            var transitionErr = await ValidateStatusTransitionAsync(thietBiId, req.TrangThaiVanHanh);
            if (transitionErr != null) return ApiResponse<ThietBiListDto>.Fail(transitionErr);
        }

        entity.TenThietBi = req.TenThietBi.Trim();
        entity.ThietBiChaId = string.IsNullOrWhiteSpace(req.ThietBiChaId) ? null : req.ThietBiChaId.Trim();
        entity.NhomThietBiId = newNhomId;
        entity.ViTriLapDatId = req.ViTriLapDatId.Trim();
        entity.BoPhanQuanLyId = req.BoPhanQuanLyId.Trim();
        entity.Model = req.Model.Trim();
        entity.Serial = req.Serial.Trim();
        entity.NhaSanXuat = req.NhaSanXuat.Trim();
        entity.NgayDuaVaoSuDung = req.NgayDuaVaoSuDung;
        entity.ChuKyBaoTriKhuyenNghiNgay = req.ChuKyBaoTriKhuyenNghiNgay;
        entity.CongDungMoTa = req.CongDungMoTa.Trim();
        entity.LuuYAnToan = req.LuuYAnToan?.Trim();
        entity.TrangThaiVanHanh = req.TrangThaiVanHanh;

        if (req.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            await StopActivePlansForDeviceAsync(thietBiId);

        await db.SaveChangesAsync();
        return await LoadSingleDtoAsync(entity.ThietBiId, "Cập nhật hồ sơ thiết bị thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string thietBiId)
    {
        var entity = await db.THIET_BI.FindAsync(thietBiId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy thiết bị.");

        // 2.2.1: Chặn xóa thiết bị cha khi còn cụm con trực thuộc
        if (await db.THIET_BI.AnyAsync(t => t.ThietBiChaId == thietBiId))
            return ApiResponse<bool>.Fail("Không thể xóa thiết bị cha khi vẫn còn cụm bộ phận con trực thuộc.");

        if (await db.YEU_CAU_BAO_TRI.AnyAsync(y => y.ThietBiId == thietBiId)
            || await db.LENH_BAO_TRI.AnyAsync(l => l.ThietBiId == thietBiId)
            || await db.KE_HOACH_BAO_TRI.AnyAsync(k => k.ThietBiId == thietBiId))
            return ApiResponse<bool>.Fail("Thiết bị đã phát sinh lịch sử bảo trì/kế hoạch. Hãy chuyển sang trạng thái NgungSuDung.");

        files.DeleteRelativeFile(entity.DuongDanAnh);
        files.DeleteRelativeFile(entity.DuongDanTaiLieu);

        db.THIET_BI.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa thiết bị.");
    }

    public async Task<ApiResponse<bool>> UpdateTrangThaiAsync(string thietBiId, string trangThaiMoi)
    {
        if (!IsValidTrangThaiThietBi(trangThaiMoi))
            return ApiResponse<bool>.Fail("Trạng thái vận hành thiết bị không hợp lệ.");

        var tb = await db.THIET_BI.FindAsync(thietBiId);
        if (tb == null) return ApiResponse<bool>.Fail("Không tìm thấy thiết bị.");

        if (tb.TrangThaiVanHanh != trangThaiMoi)
        {
            var transitionErr = await ValidateStatusTransitionAsync(thietBiId, trangThaiMoi);
            if (transitionErr != null) return ApiResponse<bool>.Fail(transitionErr);
        }

        // 2.3.1: Quản lý độc lập trạng thái giữa máy cha với các cụm con
        tb.TrangThaiVanHanh = trangThaiMoi;
        if (trangThaiMoi == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            await StopActivePlansForDeviceAsync(thietBiId);

        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, $"Đã cập nhật trạng thái thiết bị sang {trangThaiMoi}.");
    }

    public async Task<ApiResponse<List<LichSuBaoTriThietBiDto>>> GetLichSuBaoTriAsync(string thietBiId)
    {
        if (!await db.THIET_BI.AnyAsync(t => t.ThietBiId == thietBiId))
            return ApiResponse<List<LichSuBaoTriThietBiDto>>.Fail("Không tìm thấy thiết bị.");

        var list = await db.LENH_BAO_TRI.AsNoTracking()
            .Include(l => l.BaoCaoSuCo)
            .Where(l => l.ThietBiId == thietBiId && l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaDong)
            .OrderByDescending(l => l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc)
            .Select(l => new LichSuBaoTriThietBiDto
            {
                LenhBaoTriId = l.LenhBaoTriId,
                LoaiLenh = l.LoaiLenh,
                NoiDungCongViec = l.NoiDungCongViec,
                ThoiDiemThucTeBatDau = l.ThoiDiemThucTeBatDau,
                ThoiDiemThucTeKetThuc = l.ThoiDiemThucTeKetThuc,
                TrangThaiLenh = l.TrangThaiLenh,
                TinhTrangSauBaoTri = l.TinhTrangSauBaoTri,
                NhanXetNghiemThu = l.NhanXetNghiemThu,
                MaLoiChinhThucId = l.BaoCaoSuCo != null ? l.BaoCaoSuCo.MaLoiChinhThucId : null,
                BienPhapKhacPhuc = l.BaoCaoSuCo != null ? l.BaoCaoSuCo.BienPhapKhacPhuc : null
            })
            .ToListAsync();

        return ApiResponse<List<LichSuBaoTriThietBiDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<ThongSoThietBiDto>>> GetThongSoAsync(string thietBiId)
    {
        var list = await db.THONG_SO_THIET_BI.AsNoTracking()
            .Where(s => s.ThietBiId == thietBiId)
            .OrderBy(s => s.ThongSoId)
            .Select(s => new ThongSoThietBiDto
            {
                ThongSoId = s.ThongSoId,
                ThietBiId = s.ThietBiId,
                TenThongSo = s.TenThongSo,
                GiaTri = s.GiaTri,
                DonViDo = s.DonViDo,
                MoTa = s.MoTa
            })
            .ToListAsync();
        return ApiResponse<List<ThongSoThietBiDto>>.Ok(list);
    }

    public async Task<ApiResponse<ThongSoThietBiDto>> AddThongSoAsync(string thietBiId, ThongSoUpsertReq req)
    {
        if (!await db.THIET_BI.AnyAsync(t => t.ThietBiId == thietBiId))
            return ApiResponse<ThongSoThietBiDto>.Fail("Không tìm thấy thiết bị.");
        if (string.IsNullOrWhiteSpace(req.TenThongSo) || string.IsNullOrWhiteSpace(req.GiaTri))
            return ApiResponse<ThongSoThietBiDto>.Fail("Tên thông số và giá trị không được để trống.");

        var entity = new THONG_SO_THIET_BI
        {
            ThietBiId = thietBiId,
            TenThongSo = req.TenThongSo.Trim(),
            GiaTri = req.GiaTri.Trim(),
            DonViDo = req.DonViDo?.Trim(),
            MoTa = req.MoTa?.Trim()
        };
        db.THONG_SO_THIET_BI.Add(entity);
        await db.SaveChangesAsync();

        return ApiResponse<ThongSoThietBiDto>.Ok(new ThongSoThietBiDto
        {
            ThongSoId = entity.ThongSoId,
            ThietBiId = entity.ThietBiId,
            TenThongSo = entity.TenThongSo,
            GiaTri = entity.GiaTri,
            DonViDo = entity.DonViDo,
            MoTa = entity.MoTa
        }, "Đã thêm thông số kỹ thuật.");
    }

    public async Task<ApiResponse<ThongSoThietBiDto>> UpdateThongSoAsync(long thongSoId, ThongSoUpsertReq req)
    {
        var entity = await db.THONG_SO_THIET_BI.FindAsync(thongSoId);
        if (entity == null) return ApiResponse<ThongSoThietBiDto>.Fail("Không tìm thấy thông số kỹ thuật.");
        if (string.IsNullOrWhiteSpace(req.TenThongSo) || string.IsNullOrWhiteSpace(req.GiaTri))
            return ApiResponse<ThongSoThietBiDto>.Fail("Tên thông số và giá trị không được để trống.");

        entity.TenThongSo = req.TenThongSo.Trim();
        entity.GiaTri = req.GiaTri.Trim();
        entity.DonViDo = req.DonViDo?.Trim();
        entity.MoTa = req.MoTa?.Trim();
        await db.SaveChangesAsync();

        return ApiResponse<ThongSoThietBiDto>.Ok(new ThongSoThietBiDto
        {
            ThongSoId = entity.ThongSoId,
            ThietBiId = entity.ThietBiId,
            TenThongSo = entity.TenThongSo,
            GiaTri = entity.GiaTri,
            DonViDo = entity.DonViDo,
            MoTa = entity.MoTa
        }, "Đã cập nhật thông số kỹ thuật.");
    }

    public async Task<ApiResponse<bool>> DeleteThongSoAsync(long thongSoId)
    {
        var entity = await db.THONG_SO_THIET_BI.FindAsync(thongSoId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy thông số.");

        db.THONG_SO_THIET_BI.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa thông số kỹ thuật.");
    }

    public async Task<ApiResponse<string>> UploadAnhAsync(string thietBiId, IFormFile file)
    {
        var tb = await db.THIET_BI.FindAsync(thietBiId);
        if (tb == null) return ApiResponse<string>.Fail("Không tìm thấy thiết bị.");

        var saved = await files.SaveFileAsync(file, "thietbi_anh", thietBiId, [".jpg", ".jpeg", ".png", ".webp"]);
        if (!saved.Ok) return ApiResponse<string>.Fail(saved.MessageOrRelativePath);

        files.DeleteRelativeFile(tb.DuongDanAnh);
        tb.DuongDanAnh = saved.MessageOrRelativePath;
        await db.SaveChangesAsync();
        return ApiResponse<string>.Ok(tb.DuongDanAnh, "Tải ảnh đại diện thiết bị thành công.");
    }

    public async Task<ApiResponse<string>> UploadTaiLieuPdfAsync(string thietBiId, IFormFile file)
    {
        var tb = await db.THIET_BI.FindAsync(thietBiId);
        if (tb == null) return ApiResponse<string>.Fail("Không tìm thấy thiết bị.");

        var saved = await files.SaveFileAsync(file, "thietbi_pdf", thietBiId, [".pdf"]);
        if (!saved.Ok) return ApiResponse<string>.Fail(saved.MessageOrRelativePath);

        files.DeleteRelativeFile(tb.DuongDanTaiLieu);
        tb.DuongDanTaiLieu = saved.MessageOrRelativePath;
        await db.SaveChangesAsync();
        return ApiResponse<string>.Ok(tb.DuongDanTaiLieu, "Tải tài liệu hướng dẫn PDF thành công.");
    }

    public async Task<(bool Found, byte[] Bytes, string ContentType, string FileName)> DownloadTaiLieuAsync(string thietBiId, bool isImage)
    {
        var tb = await db.THIET_BI.AsNoTracking().FirstOrDefaultAsync(t => t.ThietBiId == thietBiId);
        if (tb == null) return (false, [], string.Empty, string.Empty);

        var rel = isImage ? tb.DuongDanAnh : tb.DuongDanTaiLieu;
        var fullPath = files.ResolveExistingFile(rel,
            isImage ? "uploads/thietbi_anh" : "uploads/thietbi_pdf",
            isImage ? "anh-may" : "tai-lieu");

        if (fullPath == null) return (false, [], string.Empty, string.Empty);

        var bytes = await files.ReadFileBytesAsync(fullPath);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return (true, bytes, contentType, Path.GetFileName(fullPath));
    }

    public async Task<(bool Ok, byte[] Bytes, string ContentType, string FileName, string Error)> ExportPhieuLyLichAsync(string thietBiId, string format)
    {
        var exporter = exporters.FirstOrDefault(e => e.FormatName.Equals(format, StringComparison.OrdinalIgnoreCase));
        if (exporter == null)
            return (false, [], string.Empty, string.Empty, "Định dạng xuất không hợp lệ (chọn pdf hoặc excel).");

        var card = await GetPhieuLyLichAsync(thietBiId);
        if (!card.Success || card.Data == null)
            return (false, [], string.Empty, string.Empty, card.Message);

        var d = card.Data;
        var t = d.ThongTinChung;
        var now = LocalNow;
        var title = $"PHIẾU LÝ LỊCH THIẾT BỊ (BM-01) — {t.ThietBiId}";
        var subtitle = $"{t.TenThietBi} | Model: {t.Model} | Serial: {t.Serial} | Xuất lúc: {now:dd/MM/yyyy HH:mm}";
        var headers = new List<string> { "Nhóm mục", "Hạng mục / Thông số", "Giá trị / Chi tiết" };
        var rows = new List<List<string>>
        {
            new() { "Định danh", "Mã thiết bị / Máy cha", $"{t.ThietBiId} (Cha: {t.TenThietBiCha ?? "Không"})" },
            new() { "Định danh", "Nhóm / Nhà sản xuất", $"{t.TenNhomThietBi} | {t.NhaSanXuat}" },
            new() { "Định danh", "Vị trí / Bộ phận QL", $"{t.TenViTriLapDat} | {t.TenBoPhanQuanLy}" },
            new() { "Định danh", "Ngày đưa vào SD / Chu kỳ KN", $"{t.NgayDuaVaoSuDung:dd/MM/yyyy} (Tuổi: {t.TuoiThoNgay} ngày) | Chu kỳ: {t.ChuKyBaoTriKhuyenNghiNgay} ngày" },
            new() { "Vận hành", "Trạng thái hiện tại", t.TrangThaiVanHanh },
            new() { "Vận hành", "Công dụng & An toàn", $"{d.CongDungMoTa} | Lưu ý: {d.LuuYAnToan ?? "Không"}" }
        };

        foreach (var ts in d.ThongSoKyThuat)
            rows.Add(["Thông số KT", ts.TenThongSo, $"{ts.GiaTri} {ts.DonViDo}".Trim()]);

        foreach (var dm in d.DinhMucLinhKien)
            rows.Add(["Định mức PT", $"{dm.PhuTungId} - {dm.TenPhuTung}", $"SL: {dm.SoLuongDinhMuc} {dm.DonViTinh} (Định kỳ: {(dm.ThayTheDinhKy ? "Có" : "Không")}, Vị trí: {dm.ViTriLapDat ?? "-"})"]);

        foreach (var ls in d.LichSuBaoTri)
            rows.Add(["Lịch sử BT", $"{ls.LenhBaoTriId} ({ls.LoaiLenh})", $"{ls.ThoiDiemThucTeKetThuc:dd/MM/yyyy}: {ls.NoiDungCongViec} -> {ls.TinhTrangSauBaoTri}"]);

        var bytes = exporter.Export(title, subtitle, headers, rows);
        var fileName = $"LyLich_{t.ThietBiId}_{now:yyyyMMdd}{exporter.FileExtension}";
        return (true, bytes, exporter.ContentType, fileName, string.Empty);
    }

    private async Task<string?> ValidateStatusTransitionAsync(string thietBiId, string trangThaiMoi)
    {
        if (trangThaiMoi == CmmsConstants.TrangThaiThietBi.NgungSuDung)
        {
            bool hasRunningOrPaused = await db.LENH_BAO_TRI.AnyAsync(l =>
                l.ThietBiId == thietBiId
                && (l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien
                    || l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.TamDung));
            if (hasRunningOrPaused)
                return "Không thể chuyển thiết bị sang NgungSuDung khi đang có lệnh bảo trì thực hiện hoặc tạm dừng.";
        }
        else if (trangThaiMoi is CmmsConstants.TrangThaiThietBi.DangHoatDong or CmmsConstants.TrangThaiThietBi.DuPhong)
        {
            bool hasRunning = await db.LENH_BAO_TRI.AnyAsync(l =>
                l.ThietBiId == thietBiId && l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien);
            if (hasRunning)
                return $"Không thể chuyển thiết bị sang {trangThaiMoi} khi đang có lệnh bảo trì đang thực hiện.";
        }
        return null;
    }

    private async Task StopActivePlansForDeviceAsync(string thietBiId)
    {
        var activePlans = await db.KE_HOACH_BAO_TRI
            .Where(k => k.ThietBiId == thietBiId
                     && (k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.DangApDung
                         || k.TrangThaiKeHoach == CmmsConstants.TrangThaiKeHoach.TamDung))
            .ToListAsync();
        foreach (var plan in activePlans)
            plan.TrangThaiKeHoach = CmmsConstants.TrangThaiKeHoach.NgungApDung;
    }

    private async Task<string?> ValidateThietBiAsync(ThietBiUpsertReq req, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(req.ThietBiId) || string.IsNullOrWhiteSpace(req.TenThietBi)
            || string.IsNullOrWhiteSpace(req.Model) || string.IsNullOrWhiteSpace(req.Serial)
            || string.IsNullOrWhiteSpace(req.NhaSanXuat) || string.IsNullOrWhiteSpace(req.CongDungMoTa))
            return "Vui lòng nhập đầy đủ thông tin định danh và công dụng thiết bị.";

        var today = DateOnly.FromDateTime(LocalNow);
        if (req.NgayDuaVaoSuDung == default || req.NgayDuaVaoSuDung > today.AddDays(30))
            return "Ngày đưa vào sử dụng không hợp lệ hoặc vượt quá 30 ngày so với hiện tại.";

        if (req.ChuKyBaoTriKhuyenNghiNgay <= 0)
            return "Chu kỳ bảo trì khuyến nghị phải > 0 ngày.";

        if (!IsValidTrangThaiThietBi(req.TrangThaiVanHanh))
            return "Trạng thái vận hành thiết bị không hợp lệ.";

        var id = req.ThietBiId.Trim();
        if (isCreate && await db.THIET_BI.AnyAsync(t => t.ThietBiId == id))
            return $"Mã thiết bị '{id}' đã tồn tại.";

        if (await db.THIET_BI.AnyAsync(t => t.Serial == req.Serial.Trim() && t.ThietBiId != id))
            return $"Số Serial '{req.Serial}' đã tồn tại trên hệ thống.";

        var nhom = await db.NHOM_THIET_BI.AsNoTracking().FirstOrDefaultAsync(n => n.NhomThietBiId == req.NhomThietBiId.Trim());
        if (nhom == null)
            return "Nhóm thiết bị không tồn tại.";

        bool laCumCon = !string.IsNullOrWhiteSpace(req.ThietBiChaId);
        if (!laCumCon && nhom.LaCumChiTiet)
            return "Thiết bị chính (cấp 1) không thể gán vào nhóm cụm chi tiết con.";
        if (laCumCon && !nhom.LaCumChiTiet)
            return "Cụm thiết bị con (cấp 2) phải thuộc nhóm cụm chi tiết (LaCumChiTiet = true).";

        if (!await db.PHAN_XUONG_BO_PHAN.AnyAsync(d => d.DonViId == req.ViTriLapDatId.Trim()))
            return "Vị trí lắp đặt không tồn tại.";

        if (!await db.PHAN_XUONG_BO_PHAN.AnyAsync(d => d.DonViId == req.BoPhanQuanLyId.Trim()))
            return "Bộ phận quản lý không tồn tại.";

        // 2.2.1: Cây tối đa 2 cấp cha - con & chống vòng lặp
        if (!string.IsNullOrWhiteSpace(req.ThietBiChaId))
        {
            var chaId = req.ThietBiChaId.Trim();
            if (chaId == id)
                return "Thiết bị không thể tự làm cha của chính nó (vòng lặp).";

            var parent = await db.THIET_BI.AsNoTracking().FirstOrDefaultAsync(t => t.ThietBiId == chaId);
            if (parent == null)
                return "Không tìm thấy thiết bị cha.";
            if (parent.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
                return "Không thể gắn cụm con vào thiết bị cha đã ngừng sử dụng.";
            if (!string.IsNullOrEmpty(parent.ThietBiChaId))
                return "Hệ thống chỉ hỗ trợ cấu trúc cây tối đa 2 cấp (Máy chính -> Cụm con).";

            if (!isCreate && await db.THIET_BI.AnyAsync(t => t.ThietBiChaId == id))
                return "Thiết bị này đang có cụm con trực thuộc, không thể chuyển thành cụm con của máy khác.";
        }

        return null;
    }

    private async Task<ApiResponse<ThietBiListDto>> LoadSingleDtoAsync(string thietBiId, string msg)
    {
        var tb = await db.THIET_BI.AsNoTracking()
            .Include(t => t.ThietBiCha)
            .Include(t => t.NhomThietBi)
            .Include(t => t.ViTriLapDat)
            .Include(t => t.BoPhanQuanLy)
            .FirstAsync(t => t.ThietBiId == thietBiId);
        return ApiResponse<ThietBiListDto>.Ok(MapDto(tb), msg);
    }

    private static bool IsValidTrangThaiThietBi(string status) => status is
        CmmsConstants.TrangThaiThietBi.DangHoatDong
        or CmmsConstants.TrangThaiThietBi.DungDoSuCo
        or CmmsConstants.TrangThaiThietBi.DangBaoTri
        or CmmsConstants.TrangThaiThietBi.DuPhong
        or CmmsConstants.TrangThaiThietBi.NgungSuDung;

    internal static ThietBiListDto MapDto(THIET_BI t) => new()
    {
        ThietBiId = t.ThietBiId,
        TenThietBi = t.TenThietBi,
        ThietBiChaId = t.ThietBiChaId,
        TenThietBiCha = t.ThietBiCha?.TenThietBi,
        NhomThietBiId = t.NhomThietBiId,
        TenNhomThietBi = t.NhomThietBi.TenNhom,
        ViTriLapDatId = t.ViTriLapDatId,
        TenViTriLapDat = t.ViTriLapDat.TenDonVi,
        BoPhanQuanLyId = t.BoPhanQuanLyId,
        TenBoPhanQuanLy = t.BoPhanQuanLy.TenDonVi,
        Model = t.Model,
        Serial = t.Serial,
        NhaSanXuat = t.NhaSanXuat,
        NgayDuaVaoSuDung = t.NgayDuaVaoSuDung,
        ChuKyBaoTriKhuyenNghiNgay = t.ChuKyBaoTriKhuyenNghiNgay,
        TrangThaiVanHanh = t.TrangThaiVanHanh,
        DuongDanAnh = t.DuongDanAnh,
        DuongDanTaiLieu = t.DuongDanTaiLieu
    };
}
