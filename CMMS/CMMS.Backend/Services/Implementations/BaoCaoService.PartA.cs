using CMMS.Backend.Entities;
using CMMS.Backend.Helpers.Exporters;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

/// <summary>
/// Phần A (TV A): Báo cáo BC1, BC4, BC5 và điều phối xuất Excel/PDF theo Strategy Pattern (IReportExporter).
/// </summary>
public partial class BaoCaoService(
    CmmsDbContext db,
    IEnumerable<IReportExporter> exporters,
    TimeProvider? timeProvider = null) : IBaoCaoService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

    private static bool TryNormalizeRange(ReportFilterReq filter, out DateTime start, out DateTime end, out string? error)
    {
        start = filter.TuNgay.Date;
        end = filter.DenNgay.TimeOfDay == TimeSpan.Zero
            ? filter.DenNgay.Date.AddDays(1).AddTicks(-1)
            : filter.DenNgay;

        if (filter.TuNgay.Date > filter.DenNgay.Date || filter.TuNgay > end)
        {
            error = "Từ ngày không được lớn hơn đến ngày.";
            return false;
        }

        error = null;
        return true;
    }

    public async Task<ApiResponse<Bc1TongHopDto>> GetBc1TongHopAsync(ReportFilterReq filter)
    {
        if (!TryNormalizeRange(filter, out var start, out var end, out var rangeErr))
            return ApiResponse<Bc1TongHopDto>.Fail(rangeErr!);

        var tbByStatus = await db.THIET_BI.AsNoTracking()
            .GroupBy(t => t.TrangThaiVanHanh)
            .Select(g => new TrangThaiCountDto { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync();

        var ycByStatus = await db.YEU_CAU_BAO_TRI.AsNoTracking()
            .Where(y => y.ThoiDiemGui >= start && y.ThoiDiemGui <= end)
            .GroupBy(y => y.TrangThaiYeuCau)
            .Select(g => new TrangThaiCountDto { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync();

        var lenhs = await db.LENH_BAO_TRI.AsNoTracking()
            .Where(l => l.ThoiGianDuKienBatDau >= start && l.ThoiGianDuKienBatDau <= end)
            .Select(l => new
            {
                l.LoaiLenh,
                l.TrangThaiLenh
            })
            .ToListAsync();

        // Thống kê tỷ lệ hoàn thành PM đúng hạn theo cùng mốc thời gian kết thúc với BC3
        var pmClosed = await db.LENH_BAO_TRI.AsNoTracking()
            .Where(l => l.LoaiLenh == CmmsConstants.LoaiLenh.BaoTriPhongNgua
                     && l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaDong
                     && (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) >= start
                     && (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) <= end)
            .Select(l => new
            {
                l.ThoiGianDuKienKetThuc,
                l.ThoiDiemThucTeKetThuc
            })
            .ToListAsync();

        int pmDungHan = pmClosed.Count(l => (l.ThoiDiemThucTeKetThuc ?? l.ThoiGianDuKienKetThuc) <= l.ThoiGianDuKienKetThuc);
        int pmTreHan = pmClosed.Count - pmDungHan;
        double tyLeDungHan = pmClosed.Count == 0 ? 0 : Math.Round(pmDungHan * 100.0 / pmClosed.Count, 2);

        var gioCongList = await db.PHAN_CONG_KTV.AsNoTracking()
            .Where(p => p.TrangThaiPhanCong != CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen
                     && p.LenhBaoTri.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy
                     && p.KhungGioBatDau >= start && p.KhungGioBatDau <= end)
            .Select(p => p.SoGioCongThucTe)
            .ToListAsync();

        var stopReports = await db.BAO_CAO_SU_CO.AsNoTracking()
            .Where(b => b.CoDungMay && b.ThoiDiemXayRa.HasValue && b.ThoiDiemKhoiPhuc.HasValue
                     && b.LenhBaoTri.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy
                     && b.ThoiDiemXayRa.Value >= start && b.ThoiDiemXayRa.Value <= end)
            .Select(b => new { Start = b.ThoiDiemXayRa!.Value, End = b.ThoiDiemKhoiPhuc!.Value })
            .ToListAsync();

        var issuedItems = await db.CHI_TIET_PHIEU_XUAT.AsNoTracking()
            .Where(c => c.PhieuXuat.TrangThaiPhieu == CmmsConstants.TrangThaiPhieuXuat.DaXacNhanTruTon
                     && c.PhieuXuat.ThoiDiemXacNhan.HasValue
                     && c.PhieuXuat.ThoiDiemXacNhan.Value >= start && c.PhieuXuat.ThoiDiemXacNhan.Value <= end)
            .Select(c => c.SoLuongThucDung ?? 0)
            .ToListAsync();

        return ApiResponse<Bc1TongHopDto>.Ok(new Bc1TongHopDto
        {
            TuNgay = start,
            DenNgay = end,
            ThietBiTheoTrangThai = tbByStatus,
            YeuCauTheoTrangThai = ycByStatus,
            LenhTheoLoai = lenhs.GroupBy(l => l.LoaiLenh).Select(g => new TrangThaiCountDto { TrangThai = g.Key, SoLuong = g.Count() }).ToList(),
            LenhTheoTrangThai = lenhs.GroupBy(l => l.TrangThaiLenh).Select(g => new TrangThaiCountDto { TrangThai = g.Key, SoLuong = g.Count() }).ToList(),
            SoKeHoachPmDungHan = pmDungHan,
            SoKeHoachPmTreHan = pmTreHan,
            TyLePmDungHanPhanTram = tyLeDungHan,
            TongGioCongKtv = gioCongList.Sum(),
            TongGioDungMaySuCo = Math.Round(stopReports.Sum(s => Math.Max(0, (s.End - s.Start).TotalHours)), 2),
            TongSoLuongPhuTungXuat = issuedItems.Sum()
        });
    }

    public async Task<ApiResponse<List<Bc4SuCoMaLoiDto>>> GetBc4SuCoTheoMaLoiAsync(ReportFilterReq filter)
    {
        if (!TryNormalizeRange(filter, out var start, out var end, out var rangeErr))
            return ApiResponse<List<Bc4SuCoMaLoiDto>>.Fail(rangeErr!);

        var items = await db.BAO_CAO_SU_CO.AsNoTracking()
            .Include(b => b.MaLoiChinhThuc)
            .Include(b => b.LenhBaoTri)
            .Where(b => b.LenhBaoTri.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy
                     && (b.ThoiDiemXayRa ?? b.LenhBaoTri.ThoiGianDuKienBatDau) >= start
                     && (b.ThoiDiemXayRa ?? b.LenhBaoTri.ThoiGianDuKienBatDau) <= end)
            .ToListAsync();

        var result = items
            .GroupBy(b => new { b.MaLoiChinhThucId, b.MaLoiChinhThuc.TenLoaiSuCo })
            .Select(g => new Bc4SuCoMaLoiDto
            {
                MaLoiId = g.Key.MaLoiChinhThucId,
                TenLoaiSuCo = g.Key.TenLoaiSuCo,
                SoLanXayRa = g.Count(),
                TongGioDungMay = Math.Round(g.Sum(b =>
                    b.CoDungMay && b.ThoiDiemXayRa.HasValue && b.ThoiDiemKhoiPhuc.HasValue
                        ? Math.Max(0, (b.ThoiDiemKhoiPhuc.Value - b.ThoiDiemXayRa.Value).TotalHours)
                        : 0), 2),
                DanhSachNguyenNhanGoc = g.Select(b => b.NguyenNhanGocRe).Distinct().ToList()
            })
            .OrderByDescending(x => x.SoLanXayRa)
            .ThenByDescending(x => x.TongGioDungMay)
            .ToList();

        return ApiResponse<List<Bc4SuCoMaLoiDto>>.Ok(result);
    }

    public async Task<ApiResponse<Bc5KhoiLuongKtvDto>> GetBc5KhoiLuongKtvAsync(ReportFilterReq filter)
    {
        if (!TryNormalizeRange(filter, out var start, out var end, out var rangeErr))
            return ApiResponse<Bc5KhoiLuongKtvDto>.Fail(rangeErr!);

        var items = await db.PHAN_CONG_KTV.AsNoTracking()
            .Include(p => p.KyThuatVien).ThenInclude(n => n.DonVi)
            .Where(p => p.TrangThaiPhanCong != CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen
                     && p.LenhBaoTri.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy
                     && p.KhungGioBatDau >= start && p.KhungGioBatDau <= end)
            .ToListAsync();

        var theoKtv = items
            .GroupBy(p => new { p.KyThuatVienId, p.KyThuatVien.HoTen, p.KyThuatVien.DonVi.TenDonVi })
            .Select(g => new Bc5KtvItemDto
            {
                NhanVienId = g.Key.KyThuatVienId,
                HoTen = g.Key.HoTen,
                TenDonVi = g.Key.TenDonVi,
                SoLenhThamGia = g.Select(x => x.LenhBaoTriId).Distinct().Count(),
                TongGioCong = g.Sum(x => x.SoGioCongThucTe)
            })
            .OrderByDescending(x => x.TongGioCong)
            .ToList();

        var theoBoPhan = items
            .GroupBy(p => new { p.KyThuatVien.DonViId, p.KyThuatVien.DonVi.TenDonVi })
            .Select(g => new Bc5BoPhanItemDto
            {
                DonViId = g.Key.DonViId,
                TenDonVi = g.Key.TenDonVi,
                SoLuotThamGia = g.Count(),
                TongGioCong = g.Sum(x => x.SoGioCongThucTe)
            })
            .OrderByDescending(x => x.TongGioCong)
            .ToList();

        return ApiResponse<Bc5KhoiLuongKtvDto>.Ok(new Bc5KhoiLuongKtvDto
        {
            TheoKyThuatVien = theoKtv,
            TheoBoPhan = theoBoPhan
        });
    }

    public async Task<(bool Ok, byte[] Bytes, string ContentType, string FileName, string Error)> ExportAsync(
        string reportCode, string format, ReportFilterReq filter)
    {
        var exporter = exporters.FirstOrDefault(e => e.FormatName.Equals(format, StringComparison.OrdinalIgnoreCase));
        if (exporter == null)
            return (false, [], string.Empty, string.Empty, "Định dạng xuất không hợp lệ (chọn excel hoặc pdf).");

        var now = LocalNow;
        var code = reportCode.Trim().ToUpperInvariant();
        var subtitle = $"Kỳ báo cáo: {filter.TuNgay:dd/MM/yyyy} - {filter.DenNgay:dd/MM/yyyy} | Xuất lúc: {now:dd/MM/yyyy HH:mm}";
        string title;
        List<string> headers;
        List<List<string>> rows = [];

        switch (code)
        {
            case "BC1":
                var r1 = await GetBc1TongHopAsync(filter);
                if (!r1.Success || r1.Data == null) return (false, [], string.Empty, string.Empty, r1.Message);
                var bc1 = r1.Data;
                title = "BÁO CÁO TỔNG HỢP HOẠT ĐỘNG BẢO TRÌ (BC1)";
                headers = ["Chỉ tiêu thống kê", "Giá trị"];
                rows.Add(["Tỷ lệ PM hoàn thành đúng hạn (%)", $"{bc1.TyLePmDungHanPhanTram}% ({bc1.SoKeHoachPmDungHan}/{bc1.SoKeHoachPmDungHan + bc1.SoKeHoachPmTreHan})"]);
                rows.Add(["Tổng giờ công kỹ thuật viên", $"{bc1.TongGioCongKtv:N2} giờ"]);
                rows.Add(["Tổng thời gian dừng máy do sự cố", $"{bc1.TongGioDungMaySuCo:N2} giờ"]);
                rows.Add(["Tổng số lượng phụ tùng đã xuất dùng", $"{bc1.TongSoLuongPhuTungXuat:N2}"]);
                foreach (var x in bc1.ThietBiTheoTrangThai) rows.Add([$"Thiết bị [{x.TrangThai}]", x.SoLuong.ToString()]);
                foreach (var x in bc1.LenhTheoTrangThai) rows.Add([$"Lệnh [{x.TrangThai}]", x.SoLuong.ToString()]);
                break;

            case "BC2":
                var r2 = await GetBc2DanhSachThietBiAsync(filter);
                if (!r2.Success || r2.Data == null) return (false, [], string.Empty, string.Empty, r2.Message);
                var bc2 = r2.Data;
                title = "BÁO CÁO DANH SÁCH VÀ TÌNH TRẠNG THIẾT BỊ (BC2)";
                headers = ["Mã TB", "Tên thiết bị", "Nhóm", "Vị trí", "Bộ phận QL", "Tuổi (ngày)", "Trạng thái"];
                rows = bc2.Select(x => new List<string> { x.ThietBiId, x.TenThietBi, x.TenNhomThietBi, x.TenViTriLapDat, x.TenBoPhanQuanLy, x.TuoiThoNgay.ToString(), x.TrangThaiVanHanh }).ToList();
                break;

            case "BC3":
                var r3 = await GetBc3TienDoPmAsync(filter);
                if (!r3.Success || r3.Data == null) return (false, [], string.Empty, string.Empty, r3.Message);
                var bc3 = r3.Data;
                title = "BÁO CÁO TIẾN ĐỘ BẢO TRÌ PHÒNG NGỪA (BC3)";
                headers = ["Mã KH", "Tên kế hoạch", "Thiết bị", "Đến hạn", "Mã lệnh", "Kết quả"];
                rows = bc3.Select(x => new List<string> { x.KeHoachId, x.TenKeHoach, x.TenThietBi, x.NgayDenHanTiepTheo.ToString("dd/MM/yyyy"), x.LenhBaoTriId ?? "-", x.NhomKetQua }).ToList();
                break;

            case "BC4":
                var r4 = await GetBc4SuCoTheoMaLoiAsync(filter);
                if (!r4.Success || r4.Data == null) return (false, [], string.Empty, string.Empty, r4.Message);
                var bc4 = r4.Data;
                title = "BÁO CÁO TẦN SUẤT SỰ CỐ THEO MÃ LỖI (BC4)";
                headers = ["Mã lỗi", "Loại sự cố", "Số lần", "Tổng giờ dừng máy", "Nguyên nhân gốc tiêu biểu"];
                rows = bc4.Select(x => new List<string> { x.MaLoiId, x.TenLoaiSuCo, x.SoLanXayRa.ToString(), $"{x.TongGioDungMay:N2}", string.Join(" | ", x.DanhSachNguyenNhanGoc.Take(2)) }).ToList();
                break;

            case "BC5":
                var r5 = await GetBc5KhoiLuongKtvAsync(filter);
                if (!r5.Success || r5.Data == null) return (false, [], string.Empty, string.Empty, r5.Message);
                var bc5 = r5.Data;
                title = "BÁO CÁO KHỐI LƯỢNG CÔNG VIỆC VÀ GIỜ CÔNG KTV (BC5)";
                headers = ["Mã NV", "Họ tên KTV", "Đơn vị", "Số lệnh / lượt tham gia", "Tổng giờ công"];
                rows = bc5.TheoKyThuatVien.Select(x => new List<string> { x.NhanVienId, x.HoTen, x.TenDonVi, x.SoLenhThamGia.ToString(), $"{x.TongGioCong:N2}" }).ToList();
                foreach (var bp in bc5.TheoBoPhan)
                    rows.Add(["[TỔNG BỘ PHẬN]", bp.DonViId, bp.TenDonVi, bp.SoLuotThamGia.ToString(), $"{bp.TongGioCong:N2}"]);
                break;

            case "BC6":
                var r6 = await GetBc6TonKhoToiThieuAsync(filter);
                if (!r6.Success || r6.Data == null) return (false, [], string.Empty, string.Empty, r6.Message);
                var bc6 = r6.Data;
                title = "BÁO CÁO TỒN KHO VÀ PHỤ TÙNG DƯỚI MỨC TỐI THIỂU (BC6)";
                headers = ["Mã PT", "Tên phụ tùng", "ĐVT", "Tồn hiện có", "Đang dự trù", "Khả dụng", "Tối thiểu", "Cảnh báo"];
                rows = bc6.Select(x => new List<string> { x.PhuTungId, x.TenPhuTung, x.DonViTinh, $"{x.SoLuongTonHienCo:N2}", $"{x.SoLuongDangDuTru:N2}", $"{x.TonKhoKhaDung:N2}", $"{x.MucTonToiThieu:N2}", x.IsDuoiMucToiThieu ? "DƯỚI TỐI THIỂU" : "Đủ" }).ToList();
                break;

            case "BC7":
                var r7 = await GetBc7XuatTonDoiChieuAsync(filter);
                if (!r7.Success || r7.Data == null) return (false, [], string.Empty, string.Empty, r7.Message);
                var bc7 = r7.Data;
                title = "BÁO CÁO XUẤT - TỒN VÀ ĐỐI CHIẾU DỰ TRÙ - THỰC DÙNG (BC7)";
                if (bc7.DoiChieuDuTruThucDung.Count > 0)
                {
                    headers = ["Phiếu xuất", "Lệnh BT", "Mã PT", "Tên phụ tùng", "KTV khai báo", "Dự trù", "Thực dùng", "Chênh lệch", "Giải trình"];
                    rows = bc7.DoiChieuDuTruThucDung.Select(x => new List<string> { x.PhieuXuatId, x.LenhBaoTriId, x.PhuTungId, x.TenPhuTung, x.TenNguoiKhaiBao ?? "-", $"{x.SoLuongDuTru:N2}", $"{x.SoLuongThucDung:N2}", $"{x.ChenhLech:+0.##;-0.##;0}", x.GhiChuGiaiTrinh ?? "" }).ToList();
                }
                else
                {
                    headers = ["Mã PT", "Tên phụ tùng", "ĐVT", "Tổng bổ sung", "Tổng xuất dùng", "Tồn cuối kỳ"];
                    rows = bc7.BienDongTheoPhuTung.Select(x => new List<string> { x.PhuTungId, x.TenPhuTung, x.DonViTinh, $"{x.TongBoSung:N2}", $"{x.TongXuatDung:N2}", $"{x.TonCuoiKyHienCo:N2}" }).ToList();
                }
                break;

            default:
                return (false, [], string.Empty, string.Empty, $"Mã báo cáo '{reportCode}' không hợp lệ (chọn BC1..BC7).");
        }

        var bytes = exporter.Export(title, subtitle, headers, rows);
        var fileName = $"{code}_{now:yyyyMMdd_HHmmss}{exporter.FileExtension}";
        return (true, bytes, exporter.ContentType, fileName, string.Empty);
    }
}
