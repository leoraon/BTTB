using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class LenhBaoTriService(
    CmmsDbContext db,
    AuditLogHelper audit,
    TimeProvider? timeProvider = null) : ILenhBaoTriService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

    public async Task<ApiResponse<List<LenhBaoTriDto>>> GetDanhSachAsync(
        string? loaiLenh, string? trangThai, string? thietBiId, string? ktvId, DateTime? tuNgay, DateTime? denNgay)
    {
        var q = db.LENH_BAO_TRI.AsNoTracking()
            .Include(l => l.ThietBi)
            .Include(l => l.MucUuTien)
            .Include(l => l.NguoiBanGiao)
            .Include(l => l.NguoiKyNhan)
            .Include(l => l.PhanCongKtvs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(loaiLenh)) q = q.Where(l => l.LoaiLenh == loaiLenh);
        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(l => l.TrangThaiLenh == trangThai);
        if (!string.IsNullOrWhiteSpace(thietBiId)) q = q.Where(l => l.ThietBiId == thietBiId);
        if (!string.IsNullOrWhiteSpace(ktvId))
            q = q.Where(l => l.PhanCongKtvs.Any(p => p.KyThuatVienId == ktvId && p.TrangThaiPhanCong != CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen));
        if (tuNgay.HasValue) q = q.Where(l => l.ThoiGianDuKienBatDau >= tuNgay.Value);
        if (denNgay.HasValue)
        {
            var den = denNgay.Value.TimeOfDay == TimeSpan.Zero ? denNgay.Value.Date.AddDays(1).AddTicks(-1) : denNgay.Value;
            q = q.Where(l => l.ThoiGianDuKienBatDau <= den);
        }

        var items = await q.OrderBy(l => l.MucUuTien.ThuTuUuTien).ThenByDescending(l => l.ThoiGianDuKienBatDau).ToListAsync();
        var now = LocalNow;
        return ApiResponse<List<LenhBaoTriDto>>.Ok(items.Select(l => MapDto(l, now)).ToList());
    }

    public async Task<ApiResponse<LenhBaoTriDetailDto>> GetDetailAsync(string lenhBaoTriId)
    {
        var l = await db.LENH_BAO_TRI.AsNoTracking()
            .Include(x => x.ThietBi)
            .Include(x => x.MucUuTien)
            .Include(x => x.NguoiBanGiao)
            .Include(x => x.NguoiKyNhan)
            .Include(x => x.PhanCongKtvs).ThenInclude(p => p.KyThuatVien)
            .Include(x => x.KetQuaChecklists).ThenInclude(k => k.HangMuc)
            .Include(x => x.KetQuaChecklists).ThenInclude(k => k.NguoiKiemTra)
            .Include(x => x.BaoCaoSuCo).ThenInclude(b => b!.MaLoiChinhThuc)
            .Include(x => x.BaoCaoSuCo).ThenInclude(b => b!.NguoiLapBaoCao)
            .Include(x => x.PhieuDuTruXuatDungs).ThenInclude(p => p.NguoiLapDuTru)
            .Include(x => x.PhieuDuTruXuatDungs).ThenInclude(p => p.ChiTiets).ThenInclude(c => c.PhuTung)
            .FirstOrDefaultAsync(x => x.LenhBaoTriId == lenhBaoTriId);

        if (l == null) return ApiResponse<LenhBaoTriDetailDto>.Fail("Không tìm thấy lệnh bảo trì.");

        var now = LocalNow;
        var dto = new LenhBaoTriDetailDto
        {
            LenhBaoTriId = l.LenhBaoTriId,
            ThietBiId = l.ThietBiId,
            TenThietBi = l.ThietBi.TenThietBi,
            LoaiLenh = l.LoaiLenh,
            YeuCauId = l.YeuCauId,
            KeHoachId = l.KeHoachId,
            NoiDungCongViec = l.NoiDungCongViec,
            MucUuTienId = l.MucUuTienId,
            TenMucUuTien = l.MucUuTien.TenMucUuTien,
            MaMauHienThi = l.MucUuTien.MaMauHienThi,
            ThoiGianDuKienBatDau = l.ThoiGianDuKienBatDau,
            ThoiGianDuKienKetThuc = l.ThoiGianDuKienKetThuc,
            ThoiDiemThucTeBatDau = l.ThoiDiemThucTeBatDau,
            ThoiDiemThucTeKetThuc = l.ThoiDiemThucTeKetThuc,
            LyDoTamDung = l.LyDoTamDung,
            TrangThaiLenh = l.TrangThaiLenh,
            TinhTrangSauBaoTri = l.TinhTrangSauBaoTri,
            NguoiBanGiaoId = l.NguoiBanGiaoId,
            TenNguoiBanGiao = l.NguoiBanGiao?.HoTen,
            NguoiKyNhanId = l.NguoiKyNhanId,
            TenNguoiKyNhan = l.NguoiKyNhan?.HoTen,
            ThoiDiemNghiemThu = l.ThoiDiemNghiemThu,
            NhanXetNghiemThu = l.NhanXetNghiemThu,
            NguoiTaoId = l.NguoiTaoId,
            NguoiDuyetDongId = l.NguoiDuyetDongId,
            IsTreHan = ComputeIsTreHan(l, now),
            PhanCong = l.PhanCongKtvs.OrderBy(p => p.KhungGioBatDau).Select(p => new PhanCongKtvDto
            {
                PhanCongId = p.PhanCongId,
                LenhBaoTriId = p.LenhBaoTriId,
                KyThuatVienId = p.KyThuatVienId,
                TenKyThuatVien = p.KyThuatVien.HoTen,
                CaLamViec = p.KyThuatVien.CaLamViec,
                NguoiPhanCongId = p.NguoiPhanCongId,
                ThoiDiemPhanCong = p.ThoiDiemPhanCong,
                KhungGioBatDau = p.KhungGioBatDau,
                KhungGioKetThuc = p.KhungGioKetThuc,
                VaiTroTrongLenh = p.VaiTroTrongLenh,
                SoGioCongThucTe = p.SoGioCongThucTe,
                TrangThaiPhanCong = p.TrangThaiPhanCong
            }).ToList(),
            KetQuaChecklist = l.KetQuaChecklists.OrderBy(k => k.HangMuc.ThuTuThucHien).Select(k => new KetQuaChecklistDto
            {
                KetQuaId = k.KetQuaId,
                LenhBaoTriId = k.LenhBaoTriId,
                HangMucId = k.HangMucId,
                NhomBoPhan = k.HangMuc.NhomBoPhan,
                NoiDungCongViec = k.HangMuc.NoiDungCongViec,
                KieuKetQua = k.HangMuc.KieuKetQua,
                GiaTriMin = k.HangMuc.GiaTriMin,
                GiaTriMax = k.HangMuc.GiaTriMax,
                DonViDo = k.HangMuc.DonViDo,
                GiaTriDoThucTe = k.GiaTriDoThucTe,
                KetLuan = k.KetLuan,
                NguoiKiemTraId = k.NguoiKiemTraId,
                TenNguoiKiemTra = k.NguoiKiemTra.HoTen,
                ThoiDiemKiemTra = k.ThoiDiemKiemTra,
                GhiChu = k.GhiChu
            }).ToList(),
            BaoCaoSuCo = l.BaoCaoSuCo == null ? null : new BaoCaoSuCoDto
            {
                BaoCaoSuCoId = l.BaoCaoSuCo.BaoCaoSuCoId,
                LenhBaoTriId = l.BaoCaoSuCo.LenhBaoTriId,
                MaLoiChinhThucId = l.BaoCaoSuCo.MaLoiChinhThucId,
                TenLoaiSuCo = l.BaoCaoSuCo.MaLoiChinhThuc.TenLoaiSuCo,
                CoDungMay = l.BaoCaoSuCo.CoDungMay,
                ThoiDiemXayRa = l.BaoCaoSuCo.ThoiDiemXayRa,
                ThoiDiemKhoiPhuc = l.BaoCaoSuCo.ThoiDiemKhoiPhuc,
                MoTaHuHong = l.BaoCaoSuCo.MoTaHuHong,
                NguyenNhanGocRe = l.BaoCaoSuCo.NguyenNhanGocRe,
                MucDoAnhHuongPhanTram = l.BaoCaoSuCo.MucDoAnhHuongPhanTram,
                BienPhapKhacPhuc = l.BaoCaoSuCo.BienPhapKhacPhuc,
                NguoiLapBaoCaoId = l.BaoCaoSuCo.NguoiLapBaoCaoId,
                TenNguoiLap = l.BaoCaoSuCo.NguoiLapBaoCao.HoTen
            },
            PhieuVatTu = l.PhieuDuTruXuatDungs.Select(KhoService.MapPhieuXuatDto).ToList()
        };

        return ApiResponse<LenhBaoTriDetailDto>.Ok(dto);
    }

    public async Task<ApiResponse<LenhBaoTriDto>> TaoLenhTuYeuCauAsync(TaoLenhTuYeuCauReq req, string nguoiTaoId, int actorTaiKhoanId = 0)
    {
        var yc = await db.YEU_CAU_BAO_TRI.Include(y => y.ThietBi).FirstOrDefaultAsync(y => y.YeuCauId == req.YeuCauId);
        if (yc == null) return ApiResponse<LenhBaoTriDto>.Fail("Không tìm thấy yêu cầu báo hỏng.");
        if (yc.ThietBi.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            return ApiResponse<LenhBaoTriDto>.Fail("Thiết bị đã ngừng sử dụng, không thể tạo lệnh sửa chữa.");
        if (yc.TrangThaiYeuCau != CmmsConstants.TrangThaiYeuCau.DaPheDuyet)
            return ApiResponse<LenhBaoTriDto>.Fail("Chỉ được tạo lệnh sửa chữa từ yêu cầu đã được phê duyệt.");
        if (await db.LENH_BAO_TRI.AnyAsync(l => l.YeuCauId == req.YeuCauId && l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy))
            return ApiResponse<LenhBaoTriDto>.Fail("Yêu cầu báo hỏng này đã được tạo lệnh sửa chữa.");
        if (req.ThoiGianDuKienBatDau >= req.ThoiGianDuKienKetThuc)
            return ApiResponse<LenhBaoTriDto>.Fail("Thời gian dự kiến kết thúc phải sau thời gian bắt đầu.");
        if (string.IsNullOrWhiteSpace(req.NoiDungCongViec))
            return ApiResponse<LenhBaoTriDto>.Fail("Vui lòng nhập nội dung công việc sửa chữa.");

        var now = LocalNow;
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var woId = await CodeGeneratorHelper.NextMonthlyCodeAsync(db.LENH_BAO_TRI.Select(l => l.LenhBaoTriId), "WO", now);
            var lenh = new LENH_BAO_TRI
            {
                LenhBaoTriId = woId,
                ThietBiId = yc.ThietBiId,
                LoaiLenh = CmmsConstants.LoaiLenh.BaoTriSuCo,
                YeuCauId = yc.YeuCauId,
                NoiDungCongViec = req.NoiDungCongViec.Trim(),
                MucUuTienId = yc.MucUuTienChinhThucId ?? yc.MucUuTienDeXuatId,
                ThoiGianDuKienBatDau = req.ThoiGianDuKienBatDau,
                ThoiGianDuKienKetThuc = req.ThoiGianDuKienKetThuc,
                TrangThaiLenh = CmmsConstants.TrangThaiLenh.MoiTao,
                NguoiTaoId = nguoiTaoId
            };
            db.LENH_BAO_TRI.Add(lenh);
            yc.TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.DangXuLy;

            await db.SaveChangesAsync();
            await audit.WriteAsync(
                actorTaiKhoanId > 0 ? actorTaiKhoanId : null,
                CmmsConstants.LoaiThaoTacLog.PhatLenhBaoTri,
                woId,
                $"Khởi tạo lệnh sửa chữa {woId} từ yêu cầu báo hỏng {yc.YeuCauId} cho thiết bị {yc.ThietBiId}.");
            await tx.CommitAsync();

            var saved = await db.LENH_BAO_TRI.AsNoTracking()
                .Include(l => l.ThietBi).Include(l => l.MucUuTien)
                .Include(l => l.NguoiBanGiao).Include(l => l.NguoiKyNhan)
                .FirstAsync(l => l.LenhBaoTriId == woId);
            return ApiResponse<LenhBaoTriDto>.Ok(MapDto(saved, now), "Đã khởi tạo lệnh sửa chữa.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<LenhBaoTriDto>.Fail($"Lỗi khởi tạo lệnh: {ex.Message}");
        }
    }

    public async Task<ApiResponse<LenhBaoTriDto>> DoiLichAsync(string lenhBaoTriId, DoiLichLenhReq req)
    {
        var lenh = await db.LENH_BAO_TRI
            .Include(l => l.ThietBi).Include(l => l.MucUuTien)
            .Include(l => l.NguoiBanGiao).Include(l => l.NguoiKyNhan)
            .Include(l => l.PhanCongKtvs).ThenInclude(p => p.KyThuatVien)
            .FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);

        if (lenh == null) return ApiResponse<LenhBaoTriDto>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy or CmmsConstants.TrangThaiLenh.DaHoanThanh or CmmsConstants.TrangThaiLenh.DaNghiemThu)
            return ApiResponse<LenhBaoTriDto>.Fail("Không thể dời lịch cho lệnh đã hoàn thành/đóng/hủy.");
        if (req.ThoiGianDuKienBatDau >= req.ThoiGianDuKienKetThuc)
            return ApiResponse<LenhBaoTriDto>.Fail("Thời gian dự kiến kết thúc phải sau thời gian bắt đầu.");
        if (lenh.ThoiDiemThucTeBatDau.HasValue && req.ThoiGianDuKienKetThuc < lenh.ThoiDiemThucTeBatDau.Value)
            return ApiResponse<LenhBaoTriDto>.Fail("Thời gian dự kiến kết thúc mới không được trước thời điểm thực tế đã bắt đầu.");

        lenh.ThoiGianDuKienBatDau = req.ThoiGianDuKienBatDau;
        lenh.ThoiGianDuKienKetThuc = req.ThoiGianDuKienKetThuc;
        await db.SaveChangesAsync();

        var warnings = lenh.PhanCongKtvs
            .Where(p => p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao
                     && (p.KhungGioBatDau < req.ThoiGianDuKienBatDau || p.KhungGioKetThuc > req.ThoiGianDuKienKetThuc))
            .Select(p => $"Cảnh báo: Khung giờ phân công của KTV '{p.KyThuatVien.HoTen}' ({p.KhungGioBatDau:dd/MM HH:mm}–{p.KhungGioKetThuc:dd/MM HH:mm}) nằm ngoài lịch mới của lệnh.")
            .ToList();

        return ApiResponse<LenhBaoTriDto>.Ok(MapDto(lenh, LocalNow), "Đã dời lịch lệnh bảo trì.", warnings);
    }

    public async Task<ApiResponse<List<ChuyenMonKtvDto>>> GetHoSoKtvAsync()
    {
        var ktvs = await db.NHAN_VIEN.AsNoTracking()
            .Include(n => n.TaiKhoan)
            .Include(n => n.ChuyenMonKtvs).ThenInclude(c => c.NhomThietBi)
            .Where(n => n.TrangThai == CmmsConstants.TrangThaiNhanVien.DangLamViec
                     && (n.TaiKhoan != null && n.TaiKhoan.VaiTroId == CmmsConstants.VaiTroIds.KyThuatVien
                         || n.ChuyenMonKtvs.Any()))
            .OrderBy(n => n.NhanVienId)
            .ToListAsync();

        var result = ktvs.Select(n => new ChuyenMonKtvDto
        {
            NhanVienId = n.NhanVienId,
            HoTen = n.HoTen,
            ChuyenNganh = n.ChuyenNganh,
            BacTho = n.BacTho,
            CaLamViec = n.CaLamViec,
            TrangThaiSanSang = n.TrangThaiSanSang,
            NhomThietBi = n.ChuyenMonKtvs.Select(c => new NhomKtvItemDto
            {
                NhomThietBiId = c.NhomThietBiId,
                TenNhom = c.NhomThietBi.TenNhom,
                GhiChu = c.GhiChu
            }).ToList()
        }).ToList();

        return ApiResponse<List<ChuyenMonKtvDto>>.Ok(result);
    }

    public async Task<ApiResponse<bool>> CapNhatChuyenMonKtvAsync(string nhanVienId, CapNhatChuyenMonKtvReq req)
    {
        var nv = await db.NHAN_VIEN.FindAsync(nhanVienId);
        if (nv == null) return ApiResponse<bool>.Fail("Không tìm thấy kỹ thuật viên.");
        if (nv.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec)
            return ApiResponse<bool>.Fail("Nhân viên đã ngừng công tác, không thể cập nhật chuyên môn.");

        var distinctGroups = req.DanhSachNhom
            .Where(x => !string.IsNullOrWhiteSpace(x.NhomThietBiId))
            .GroupBy(x => x.NhomThietBiId.Trim())
            .Select(g => new NhomKtvItemReq { NhomThietBiId = g.Key, GhiChu = g.First().GhiChu })
            .ToList();

        // 5.2.1: Mỗi kỹ thuật viên phụ trách tối đa 3 nhóm máy
        if (distinctGroups.Count > 3)
            return ApiResponse<bool>.Fail("Ràng buộc nghiệp vụ: Mỗi kỹ thuật viên chỉ được phụ trách tối đa 3 nhóm thiết bị.");

        foreach (var item in distinctGroups)
        {
            if (!await db.NHOM_THIET_BI.AnyAsync(n => n.NhomThietBiId == item.NhomThietBiId))
                return ApiResponse<bool>.Fail($"Nhóm thiết bị '{item.NhomThietBiId}' không tồn tại.");
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var old = db.CHUYEN_MON_KTV.Where(c => c.NhanVienId == nhanVienId);
            db.CHUYEN_MON_KTV.RemoveRange(old);

            foreach (var item in distinctGroups)
            {
                db.CHUYEN_MON_KTV.Add(new CHUYEN_MON_KTV
                {
                    NhanVienId = nhanVienId,
                    NhomThietBiId = item.NhomThietBiId,
                    GhiChu = item.GhiChu?.Trim()
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã cập nhật hồ sơ chuyên môn kỹ thuật viên.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi cập nhật chuyên môn KTV: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PhanCongKtvDto>> PhanCongKtvAsync(
        string lenhBaoTriId, PhanCongKtvReq req, string nguoiPhanCongId)
    {
        var lenh = await db.LENH_BAO_TRI.Include(l => l.ThietBi).FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<PhanCongKtvDto>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong
            or CmmsConstants.TrangThaiLenh.DaHuy
            or CmmsConstants.TrangThaiLenh.DaNghiemThu
            or CmmsConstants.TrangThaiLenh.DaHoanThanh)
            return ApiResponse<PhanCongKtvDto>.Fail("Lệnh bảo trì đã ở trạng thái không cho phép phân công thêm.");
        if (req.KhungGioBatDau >= req.KhungGioKetThuc)
            return ApiResponse<PhanCongKtvDto>.Fail("Khung giờ kết thúc phải sau khung giờ bắt đầu.");

        var vaiTro = string.IsNullOrWhiteSpace(req.VaiTroTrongLenh)
            ? CmmsConstants.VaiTroTrongLenh.ThoChinh
            : req.VaiTroTrongLenh.Trim();
        if (vaiTro is not (CmmsConstants.VaiTroTrongLenh.ThoChinh or CmmsConstants.VaiTroTrongLenh.ThoPhu))
            return ApiResponse<PhanCongKtvDto>.Fail("Vai trò trong lệnh không hợp lệ (chọn ThoChinh hoặc ThoPhu).");

        var ktv = await db.NHAN_VIEN.FindAsync(req.KyThuatVienId);
        if (ktv == null || ktv.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec || ktv.TrangThaiSanSang == CmmsConstants.TrangThaiSanSang.NghiPhep)
            return ApiResponse<PhanCongKtvDto>.Fail("Kỹ thuật viên không tồn tại hoặc đang nghỉ phép / ngừng công tác.");

        bool dungChuyenMon = await db.CHUYEN_MON_KTV.AnyAsync(c =>
            c.NhanVienId == req.KyThuatVienId && c.NhomThietBiId == lenh.ThietBi.NhomThietBiId);
        if (!dungChuyenMon)
            return ApiResponse<PhanCongKtvDto>.Fail($"KTV '{ktv.HoTen}' chưa được gán chuyên môn cho nhóm thiết bị '{lenh.ThietBi.NhomThietBiId}'.");

        if (!IsWithinShift(ktv.CaLamViec, req.KhungGioBatDau, req.KhungGioKetThuc))
            return ApiResponse<PhanCongKtvDto>.Fail($"Khung giờ phân công ({req.KhungGioBatDau:HH:mm}–{req.KhungGioKetThuc:HH:mm}) không nằm trong ca làm việc ({ktv.CaLamViec}) của KTV.");

        bool dupSameWo = await db.PHAN_CONG_KTV.AnyAsync(p =>
            p.LenhBaoTriId == lenhBaoTriId
            && p.KyThuatVienId == req.KyThuatVienId
            && (p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao || p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam)
            && p.KhungGioBatDau < req.KhungGioKetThuc
            && req.KhungGioBatDau < p.KhungGioKetThuc);
        if (dupSameWo)
            return ApiResponse<PhanCongKtvDto>.Fail($"KTV '{ktv.HoTen}' đã được phân công trong khung giờ này ở chính lệnh {lenhBaoTriId}.");

        var overlapList = await db.PHAN_CONG_KTV
            .Include(p => p.LenhBaoTri)
            .Where(p => p.KyThuatVienId == req.KyThuatVienId
                     && p.LenhBaoTriId != lenhBaoTriId
                     && (p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao
                         || p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam)
                     && p.KhungGioBatDau < req.KhungGioKetThuc
                     && req.KhungGioBatDau < p.KhungGioKetThuc)
            .ToListAsync();

        var excludedIds = overlapList.Select(x => x.PhanCongId).ToHashSet();
        if (lenh.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien)
        {
            var otherRunning = await db.PHAN_CONG_KTV.AsNoTracking()
                .Where(p => p.KyThuatVienId == req.KyThuatVienId
                         && !excludedIds.Contains(p.PhanCongId)
                         && p.LenhBaoTriId != lenhBaoTriId
                         && p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam
                         && p.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien)
                .Select(p => p.LenhBaoTriId)
                .FirstOrDefaultAsync();
            if (otherRunning != null)
                return ApiResponse<PhanCongKtvDto>.Fail($"KTV '{ktv.HoTen}' đang thực hiện trực tiếp lệnh '{otherRunning}', không thể gán trạng thái DangLam đồng thời.");
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var preemptedWoIds = new List<string>();
            if (overlapList.Count > 0)
            {
                if (lenh.MucUuTienId != 1 || !req.ChoPhepChenNgang)
                {
                    var conflicts = string.Join(", ", overlapList.Select(x => x.LenhBaoTriId).Distinct());
                    return ApiResponse<PhanCongKtvDto>.Fail($"KTV bị trùng khung giờ với lệnh: {conflicts}.");
                }

                if (string.IsNullOrWhiteSpace(req.LyDoTamDung))
                    return ApiResponse<PhanCongKtvDto>.Fail("Khi chen ngang lệnh Khẩn cấp bắt buộc phải nhập lý do tạm dừng/điều chuyển lệnh mức thấp hơn.");

                var urgentConflict = overlapList.FirstOrDefault(pcCu => pcCu.LenhBaoTri.MucUuTienId == 1);
                if (urgentConflict != null)
                    return ApiResponse<PhanCongKtvDto>.Fail($"Không thể chen ngang lệnh cùng mức Khẩn cấp ({urgentConflict.LenhBaoTriId}).");

                foreach (var pcCu in overlapList)
                {
                    pcCu.TrangThaiPhanCong = CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen;
                    preemptedWoIds.Add(pcCu.LenhBaoTriId);
                    if (pcCu.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien)
                    {
                        pcCu.LenhBaoTri.TrangThaiLenh = CmmsConstants.TrangThaiLenh.TamDung;
                        pcCu.LenhBaoTri.LyDoTamDung = req.LyDoTamDung.Trim();
                    }
                }
            }

            var pc = new PHAN_CONG_KTV
            {
                LenhBaoTriId = lenhBaoTriId,
                KyThuatVienId = req.KyThuatVienId,
                NguoiPhanCongId = nguoiPhanCongId,
                ThoiDiemPhanCong = LocalNow,
                KhungGioBatDau = req.KhungGioBatDau,
                KhungGioKetThuc = req.KhungGioKetThuc,
                VaiTroTrongLenh = vaiTro,
                SoGioCongThucTe = 0,
                TrangThaiPhanCong = lenh.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien
                    ? CmmsConstants.TrangThaiPhanCong.DangLam
                    : CmmsConstants.TrangThaiPhanCong.DaGiao
            };
            db.PHAN_CONG_KTV.Add(pc);

            if (lenh.TrangThaiLenh == CmmsConstants.TrangThaiLenh.MoiTao)
                lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DaPhanCong;

            await db.SaveChangesAsync();

            foreach (var oldWoId in preemptedWoIds.Distinct())
            {
                var oldWo = await db.LENH_BAO_TRI.FindAsync(oldWoId);
                if (oldWo != null && oldWo.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaPhanCong)
                {
                    bool stillHasActive = await db.PHAN_CONG_KTV.AnyAsync(p =>
                        p.LenhBaoTriId == oldWoId
                        && (p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao
                            || p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam));
                    if (!stillHasActive)
                        oldWo.TrangThaiLenh = CmmsConstants.TrangThaiLenh.MoiTao;
                }
            }

            await RefreshKtvReadinessAsync([req.KyThuatVienId]);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return ApiResponse<PhanCongKtvDto>.Ok(new PhanCongKtvDto
            {
                PhanCongId = pc.PhanCongId,
                LenhBaoTriId = pc.LenhBaoTriId,
                KyThuatVienId = pc.KyThuatVienId,
                TenKyThuatVien = ktv.HoTen,
                CaLamViec = ktv.CaLamViec,
                NguoiPhanCongId = pc.NguoiPhanCongId,
                ThoiDiemPhanCong = pc.ThoiDiemPhanCong,
                KhungGioBatDau = pc.KhungGioBatDau,
                KhungGioKetThuc = pc.KhungGioKetThuc,
                VaiTroTrongLenh = pc.VaiTroTrongLenh,
                SoGioCongThucTe = pc.SoGioCongThucTe,
                TrangThaiPhanCong = pc.TrangThaiPhanCong
            }, "Phân công kỹ thuật viên thành công.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<PhanCongKtvDto>.Fail($"Lỗi phân công KTV: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> HuyPhanCongKtvAsync(long phanCongId)
    {
        var pc = await db.PHAN_CONG_KTV.Include(p => p.LenhBaoTri).FirstOrDefaultAsync(p => p.PhanCongId == phanCongId);
        if (pc == null) return ApiResponse<bool>.Fail("Không tìm thấy lượt phân công KTV.");
        if (pc.TrangThaiPhanCong is CmmsConstants.TrangThaiPhanCong.HoanThanh or CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen)
            return ApiResponse<bool>.Fail("Lượt phân công đã hoàn thành hoặc đã hủy trước đó.");
        if (pc.LenhBaoTri.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy)
            return ApiResponse<bool>.Fail("Không thể gỡ phân công khỏi lệnh đã đóng hoặc đã hủy.");

        pc.TrangThaiPhanCong = CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen;
        if (pc.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DaPhanCong)
        {
            bool conPhanCongMo = await db.PHAN_CONG_KTV.AnyAsync(p =>
                p.LenhBaoTriId == pc.LenhBaoTriId
                && p.PhanCongId != phanCongId
                && (p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao
                    || p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam));
            if (!conPhanCongMo)
                pc.LenhBaoTri.TrangThaiLenh = CmmsConstants.TrangThaiLenh.MoiTao;
        }

        await db.SaveChangesAsync();
        await RefreshKtvReadinessAsync([pc.KyThuatVienId]);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã gỡ phân công kỹ thuật viên.");
    }

    public async Task<ApiResponse<bool>> BatDauLenhAsync(string lenhBaoTriId, string actorNhanVienId)
    {
        var lenh = await db.LENH_BAO_TRI
            .Include(l => l.ThietBi)
            .Include(l => l.PhanCongKtvs).ThenInclude(p => p.KyThuatVien)
            .FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);

        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.ThietBi.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            return ApiResponse<bool>.Fail("Thiết bị đã ngừng sử dụng, không thể bắt đầu thực hiện lệnh bảo trì.");
        if (lenh.TrangThaiLenh is not (CmmsConstants.TrangThaiLenh.DaPhanCong or CmmsConstants.TrangThaiLenh.MoiTao))
            return ApiResponse<bool>.Fail($"Không thể bắt đầu lệnh đang ở trạng thái '{lenh.TrangThaiLenh}'.");

        var toStart = lenh.PhanCongKtvs
            .Where(p => p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao)
            .ToList();

        foreach (var pc in toStart)
        {
            if (pc.KyThuatVien.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec
                || pc.KyThuatVien.TrangThaiSanSang == CmmsConstants.TrangThaiSanSang.NghiPhep)
                return ApiResponse<bool>.Fail($"KTV '{pc.KyThuatVien.HoTen}' đang nghỉ phép hoặc đã ngừng công tác, vui lòng gỡ/đổi phân công trước khi bắt đầu.");

            var busyWo = await db.PHAN_CONG_KTV.AsNoTracking()
                .Where(p => p.KyThuatVienId == pc.KyThuatVienId
                         && p.LenhBaoTriId != lenhBaoTriId
                         && p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam
                         && p.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien)
                .Select(p => p.LenhBaoTriId)
                .FirstOrDefaultAsync();
            if (busyWo != null)
                return ApiResponse<bool>.Fail($"KTV '{pc.KyThuatVien.HoTen}' đang thực hiện lệnh '{busyWo}', không thể bắt đầu lệnh này cùng lúc.");
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DangThucHien;
            lenh.ThoiDiemThucTeBatDau ??= LocalNow;

            if (lenh.ThietBi.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung)
                lenh.ThietBi.TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DangBaoTri;

            var ktvIds = new List<string>();
            foreach (var pc in toStart)
            {
                pc.TrangThaiPhanCong = CmmsConstants.TrangThaiPhanCong.DangLam;
                ktvIds.Add(pc.KyThuatVienId);
            }

            await db.SaveChangesAsync();
            await RefreshKtvReadinessAsync(ktvIds);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã bắt đầu thực hiện lệnh bảo trì; thiết bị chuyển sang DangBaoTri.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi bắt đầu lệnh: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> TamDungLenhAsync(string lenhBaoTriId, TamDungLenhReq req)
    {
        var lenh = await db.LENH_BAO_TRI.Include(l => l.PhanCongKtvs).FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DangThucHien)
            return ApiResponse<bool>.Fail("Chỉ được tạm dừng lệnh đang thực hiện.");
        if (string.IsNullOrWhiteSpace(req.LyDoTamDung))
            return ApiResponse<bool>.Fail("Bắt buộc nhập lý do tạm dừng lệnh.");

        lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.TamDung;
        lenh.LyDoTamDung = req.LyDoTamDung.Trim();
        await db.SaveChangesAsync();
        await RefreshKtvReadinessAsync(lenh.PhanCongKtvs.Select(p => p.KyThuatVienId));
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã tạm dừng lệnh bảo trì.");
    }

    public async Task<ApiResponse<bool>> TiepTucLenhAsync(string lenhBaoTriId)
    {
        var lenh = await db.LENH_BAO_TRI
            .Include(l => l.ThietBi)
            .Include(l => l.PhanCongKtvs).ThenInclude(p => p.KyThuatVien)
            .FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.ThietBi.TrangThaiVanHanh == CmmsConstants.TrangThaiThietBi.NgungSuDung)
            return ApiResponse<bool>.Fail("Thiết bị đã ngừng sử dụng, không thể tiếp tục lệnh bảo trì.");
        if (lenh.TrangThaiLenh != CmmsConstants.TrangThaiLenh.TamDung)
            return ApiResponse<bool>.Fail("Chỉ có thể tiếp tục lệnh đang tạm dừng.");

        var activeAssignments = lenh.PhanCongKtvs
            .Where(p => p.TrangThaiPhanCong is CmmsConstants.TrangThaiPhanCong.DaGiao or CmmsConstants.TrangThaiPhanCong.DangLam)
            .ToList();

        if (lenh.PhanCongKtvs.Count > 0 && activeAssignments.Count == 0)
            return ApiResponse<bool>.Fail("Toàn bộ phân công kỹ thuật viên của lệnh đã bị điều chuyển hoặc hủy. Vui lòng phân công lại KTV trước khi tiếp tục.");

        foreach (var pc in activeAssignments)
        {
            if (pc.KyThuatVien.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec
                || pc.KyThuatVien.TrangThaiSanSang == CmmsConstants.TrangThaiSanSang.NghiPhep)
                return ApiResponse<bool>.Fail($"KTV '{pc.KyThuatVien.HoTen}' đang nghỉ phép hoặc ngừng công tác, vui lòng đổi phân công trước khi tiếp tục.");

            var busyWo = await db.PHAN_CONG_KTV.AsNoTracking()
                .Where(p => p.KyThuatVienId == pc.KyThuatVienId
                         && p.LenhBaoTriId != lenhBaoTriId
                         && p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam
                         && p.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien)
                .Select(p => p.LenhBaoTriId)
                .FirstOrDefaultAsync();
            if (busyWo != null)
                return ApiResponse<bool>.Fail($"KTV '{pc.KyThuatVien.HoTen}' đang thực hiện lệnh '{busyWo}', chưa thể tiếp tục lệnh này.");
        }

        foreach (var pc in activeAssignments.Where(p => p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DaGiao))
            pc.TrangThaiPhanCong = CmmsConstants.TrangThaiPhanCong.DangLam;

        lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DangThucHien;
        if (lenh.ThietBi.TrangThaiVanHanh != CmmsConstants.TrangThaiThietBi.NgungSuDung)
            lenh.ThietBi.TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DangBaoTri;

        await db.SaveChangesAsync();
        await RefreshKtvReadinessAsync(activeAssignments.Select(p => p.KyThuatVienId));
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã tiếp tục thực hiện lệnh bảo trì.");
    }

    public async Task<ApiResponse<bool>> KetThucLenhAsync(string lenhBaoTriId, KetThucLenhReq req)
    {
        var lenh = await db.LENH_BAO_TRI.Include(l => l.PhanCongKtvs).FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh is not (CmmsConstants.TrangThaiLenh.DangThucHien or CmmsConstants.TrangThaiLenh.TamDung))
            return ApiResponse<bool>.Fail("Chỉ có thể kết thúc lệnh đang thực hiện hoặc tạm dừng.");

        var now = LocalNow;
        var actualStart = req.ThoiDiemThucTeBatDau ?? lenh.ThoiDiemThucTeBatDau ?? now;
        var actualEnd = req.ThoiDiemThucTeKetThuc ?? now;

        if (actualStart > now.AddMinutes(5) || actualEnd > now.AddMinutes(5))
            return ApiResponse<bool>.Fail("Thời điểm thực tế bắt đầu và kết thúc không được ở tương lai.");
        if (actualEnd < actualStart)
            return ApiResponse<bool>.Fail("Thời điểm thực tế kết thúc phải sau hoặc bằng thời điểm thực tế bắt đầu.");

        var rawHours = Math.Max(0, (actualEnd - actualStart).TotalHours);
        var defaultHours = Math.Min(9999.99m, Math.Round((decimal)rawHours, 2));
        var ktvIds = new List<string>();
        foreach (var pc in lenh.PhanCongKtvs.Where(p => p.TrangThaiPhanCong != CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen))
        {
            var input = req.GioCongKtv.FirstOrDefault(x => x.PhanCongId == pc.PhanCongId);
            if (input != null)
            {
                if (input.SoGioCongThucTe is < 0 or > 9999.99m)
                    return ApiResponse<bool>.Fail("Số giờ công thực tế phải nằm trong khoảng 0 đến 9999.99 giờ.");
                pc.SoGioCongThucTe = input.SoGioCongThucTe;
            }
            else if (pc.SoGioCongThucTe <= 0)
            {
                pc.SoGioCongThucTe = defaultHours;
            }
            pc.TrangThaiPhanCong = CmmsConstants.TrangThaiPhanCong.HoanThanh;
            ktvIds.Add(pc.KyThuatVienId);
        }

        lenh.ThoiDiemThucTeBatDau = actualStart;
        lenh.ThoiDiemThucTeKetThuc = actualEnd;
        lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DaHoanThanh;

        await db.SaveChangesAsync();
        await RefreshKtvReadinessAsync(ktvIds);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã ghi nhận kết thúc công việc và giờ công thực tế.");
    }

    public async Task<ApiResponse<List<KetQuaChecklistDto>>> LuuKetQuaChecklistAsync(
        string lenhBaoTriId, List<KetQuaChecklistItemReq> items, string ktvId)
    {
        var lenh = await db.LENH_BAO_TRI.Include(l => l.KetQuaChecklists).FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<List<KetQuaChecklistDto>>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.LoaiLenh != CmmsConstants.LoaiLenh.BaoTriPhongNgua)
            return ApiResponse<List<KetQuaChecklistDto>>.Fail("Chỉ lệnh bảo trì phòng ngừa định kỳ mới ghi nhận kết quả Checklist.");
        if (lenh.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy or CmmsConstants.TrangThaiLenh.DaNghiemThu)
            return ApiResponse<List<KetQuaChecklistDto>>.Fail("Lệnh đã nghiệm thu/đóng/hủy, không thể cập nhật kết quả checklist.");
        if (items.Count == 0)
            return ApiResponse<List<KetQuaChecklistDto>>.Fail("Danh sách kết quả checklist trống.");
        if (items.Select(i => i.HangMucId).Distinct().Count() != items.Count)
            return ApiResponse<List<KetQuaChecklistDto>>.Fail("Danh sách kết quả checklist không được trùng lặp hạng mục.");

        string? expectedMauId = null;
        if (!string.IsNullOrEmpty(lenh.KeHoachId))
        {
            expectedMauId = await db.KE_HOACH_BAO_TRI
                .Where(k => k.KeHoachId == lenh.KeHoachId)
                .Select(k => k.MauChecklistId)
                .FirstOrDefaultAsync();
        }

        var hmIds = items.Select(i => i.HangMucId).Distinct().ToList();
        var hmMap = await db.HANG_MUC_CHECKLIST.Where(h => hmIds.Contains(h.HangMucId)).ToDictionaryAsync(h => h.HangMucId);
        var warnings = new List<string>();
        var now = LocalNow;

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            db.KET_QUA_CHECKLIST.RemoveRange(lenh.KetQuaChecklists);
            foreach (var item in items)
            {
                if (!hmMap.TryGetValue(item.HangMucId, out var hm))
                    return ApiResponse<List<KetQuaChecklistDto>>.Fail($"Hạng mục checklist ID={item.HangMucId} không tồn tại.");
                if (expectedMauId != null && hm.MauChecklistId != expectedMauId)
                    return ApiResponse<List<KetQuaChecklistDto>>.Fail($"Hạng mục ID={item.HangMucId} không thuộc mẫu checklist '{expectedMauId}' của kế hoạch.");

                string ketLuan;
                if (hm.KieuKetQua == CmmsConstants.KieuKetQua.NhapSoDo)
                {
                    if (!item.GiaTriDoThucTe.HasValue)
                        return ApiResponse<List<KetQuaChecklistDto>>.Fail($"Hạng mục '{hm.NoiDungCongViec}' yêu cầu nhập giá trị đo thực tế.");

                    bool dat = (!hm.GiaTriMin.HasValue || item.GiaTriDoThucTe.Value >= hm.GiaTriMin.Value)
                            && (!hm.GiaTriMax.HasValue || item.GiaTriDoThucTe.Value <= hm.GiaTriMax.Value);
                    ketLuan = dat ? CmmsConstants.KetLuanChecklist.Dat : CmmsConstants.KetLuanChecklist.KhongDat;
                }
                else
                {
                    ketLuan = item.KetLuan is CmmsConstants.KetLuanChecklist.Dat or CmmsConstants.KetLuanChecklist.KhongDat
                        ? item.KetLuan
                        : throw new ArgumentException($"Hạng mục '{hm.NoiDungCongViec}' phải chọn kết luận Dat hoặc KhongDat.");
                }

                if (ketLuan == CmmsConstants.KetLuanChecklist.KhongDat)
                    warnings.Add($"Không đạt / vượt ngưỡng: {hm.NhomBoPhan} - {hm.NoiDungCongViec}");

                db.KET_QUA_CHECKLIST.Add(new KET_QUA_CHECKLIST
                {
                    LenhBaoTriId = lenhBaoTriId,
                    HangMucId = hm.HangMucId,
                    GiaTriDoThucTe = item.GiaTriDoThucTe,
                    KetLuan = ketLuan,
                    NguoiKiemTraId = ktvId,
                    ThoiDiemKiemTra = now,
                    GhiChu = item.GhiChu?.Trim()
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            var detail = await GetDetailAsync(lenhBaoTriId);
            return ApiResponse<List<KetQuaChecklistDto>>.Ok(
                detail.Data!.KetQuaChecklist,
                warnings.Count > 0
                    ? $"Đã lưu kết quả checklist. Có {warnings.Count} hạng mục KHÔNG ĐẠT (cảnh báo để người dùng chủ động tạo phiếu báo hỏng nếu cần)."
                    : "Đã lưu kết quả checklist (tất cả đạt chuẩn).",
                warnings);
        }
        catch (ArgumentException ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<List<KetQuaChecklistDto>>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<List<KetQuaChecklistDto>>.Fail($"Lỗi lưu kết quả checklist: {ex.Message}");
        }
    }

    public async Task<ApiResponse<BaoCaoSuCoDto>> LuuBaoCaoSuCoAsync(
        string lenhBaoTriId, BaoCaoSuCoUpsertReq req, string ktvId)
    {
        var lenh = await db.LENH_BAO_TRI.Include(l => l.BaoCaoSuCo).FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<BaoCaoSuCoDto>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.LoaiLenh != CmmsConstants.LoaiLenh.BaoTriSuCo)
            return ApiResponse<BaoCaoSuCoDto>.Fail("Chỉ lệnh bảo trì sự cố mới được lập Báo cáo sự cố.");
        if (lenh.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy or CmmsConstants.TrangThaiLenh.DaNghiemThu)
            return ApiResponse<BaoCaoSuCoDto>.Fail("Lệnh đã nghiệm thu/đóng/hủy, không thể chỉnh sửa báo cáo sự cố.");

        if (!await db.DANH_MUC_MA_LOI.AnyAsync(m => m.MaLoiId == req.MaLoiChinhThucId))
            return ApiResponse<BaoCaoSuCoDto>.Fail("Mã lỗi chính thức không tồn tại.");
        if (req.MucDoAnhHuongPhanTram is < 0 or > 100)
            return ApiResponse<BaoCaoSuCoDto>.Fail("Mức độ ảnh hưởng phải từ 0% đến 100%.");
        if (string.IsNullOrWhiteSpace(req.MoTaHuHong) || string.IsNullOrWhiteSpace(req.NguyenNhanGocRe) || string.IsNullOrWhiteSpace(req.BienPhapKhacPhuc))
            return ApiResponse<BaoCaoSuCoDto>.Fail("Vui lòng nhập mô tả hư hỏng, nguyên nhân gốc rễ và biện pháp khắc phục.");

        if (req.CoDungMay)
        {
            if (!req.ThoiDiemXayRa.HasValue || !req.ThoiDiemKhoiPhuc.HasValue)
                return ApiResponse<BaoCaoSuCoDto>.Fail("Khi có dừng máy phải nhập Thời điểm xảy ra và Thời điểm khôi phục để tính tổng giờ dừng máy.");
            if (req.ThoiDiemXayRa.Value >= req.ThoiDiemKhoiPhuc.Value)
                return ApiResponse<BaoCaoSuCoDto>.Fail("Thời điểm khôi phục vận hành phải sau thời điểm xảy ra sự cố.");
            if (req.ThoiDiemKhoiPhuc.Value > LocalNow.AddMinutes(5))
                return ApiResponse<BaoCaoSuCoDto>.Fail("Thời điểm khôi phục vận hành không được ở tương lai.");
        }

        if (lenh.BaoCaoSuCo == null)
        {
            lenh.BaoCaoSuCo = new BAO_CAO_SU_CO { LenhBaoTriId = lenhBaoTriId };
            db.BAO_CAO_SU_CO.Add(lenh.BaoCaoSuCo);
        }

        lenh.BaoCaoSuCo.MaLoiChinhThucId = req.MaLoiChinhThucId;
        lenh.BaoCaoSuCo.CoDungMay = req.CoDungMay;
        lenh.BaoCaoSuCo.ThoiDiemXayRa = req.CoDungMay ? req.ThoiDiemXayRa : null;
        lenh.BaoCaoSuCo.ThoiDiemKhoiPhuc = req.CoDungMay ? req.ThoiDiemKhoiPhuc : null;
        lenh.BaoCaoSuCo.MoTaHuHong = req.MoTaHuHong.Trim();
        lenh.BaoCaoSuCo.NguyenNhanGocRe = req.NguyenNhanGocRe.Trim();
        lenh.BaoCaoSuCo.MucDoAnhHuongPhanTram = req.MucDoAnhHuongPhanTram;
        lenh.BaoCaoSuCo.BienPhapKhacPhuc = req.BienPhapKhacPhuc.Trim();
        lenh.BaoCaoSuCo.NguoiLapBaoCaoId = ktvId;

        await db.SaveChangesAsync();
        var detail = await GetDetailAsync(lenhBaoTriId);
        return ApiResponse<BaoCaoSuCoDto>.Ok(detail.Data!.BaoCaoSuCo!, "Đã lưu báo cáo xử lý sự cố.");
    }

    public async Task<ApiResponse<bool>> NghiemThuLenhAsync(string lenhBaoTriId, NghiemThuLenhReq req)
    {
        var lenh = await db.LENH_BAO_TRI.FindAsync(lenhBaoTriId);
        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHoanThanh)
            return ApiResponse<bool>.Fail("Chỉ được lập biên bản nghiệm thu khi lệnh đã ở trạng thái DaHoanThanh.");

        if (lenh.LoaiLenh == CmmsConstants.LoaiLenh.BaoTriSuCo)
        {
            bool hasReport = await db.BAO_CAO_SU_CO.AnyAsync(b => b.LenhBaoTriId == lenhBaoTriId);
            if (!hasReport)
                return ApiResponse<bool>.Fail("Lệnh sửa chữa sự cố bắt buộc phải lập Báo cáo xử lý sự cố trước khi nghiệm thu.");
        }
        else if (lenh.LoaiLenh == CmmsConstants.LoaiLenh.BaoTriPhongNgua && !string.IsNullOrEmpty(lenh.KeHoachId))
        {
            var mauId = await db.KE_HOACH_BAO_TRI
                .Where(k => k.KeHoachId == lenh.KeHoachId)
                .Select(k => k.MauChecklistId)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(mauId))
            {
                var requiredIds = await db.HANG_MUC_CHECKLIST
                    .Where(h => h.MauChecklistId == mauId)
                    .Select(h => h.HangMucId)
                    .ToListAsync();
                var recordedIds = await db.KET_QUA_CHECKLIST
                    .Where(q => q.LenhBaoTriId == lenhBaoTriId)
                    .Select(q => q.HangMucId)
                    .Distinct()
                    .ToListAsync();

                if (requiredIds.Any(id => !recordedIds.Contains(id)))
                    return ApiResponse<bool>.Fail("Lệnh bảo trì phòng ngừa bắt buộc phải hoàn thành đầy đủ tất cả hạng mục Checklist định kỳ trước khi nghiệm thu.");
            }
        }

        if (req.TinhTrangSauBaoTri is not (CmmsConstants.TinhTrangSauBaoTri.DatChuan or CmmsConstants.TinhTrangSauBaoTri.CanTheoDoi))
            return ApiResponse<bool>.Fail("Tình trạng sau bảo trì không hợp lệ (chọn DatChuan hoặc CanTheoDoi).");

        var banGiaoId = req.NguoiBanGiaoId?.Trim() ?? string.Empty;
        var kyNhanId = req.NguoiKyNhanId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(banGiaoId) || string.IsNullOrWhiteSpace(kyNhanId))
            return ApiResponse<bool>.Fail("Bắt buộc chọn người bàn giao và người ký nhận nghiệm thu.");
        if (banGiaoId == kyNhanId)
            return ApiResponse<bool>.Fail("Người bàn giao và người ký nhận nghiệm thu phải là hai nhân sự khác nhau.");

        var bg = await db.NHAN_VIEN.FindAsync(banGiaoId);
        var kn = await db.NHAN_VIEN.FindAsync(kyNhanId);
        if (bg == null || bg.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec
            || kn == null || kn.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec)
            return ApiResponse<bool>.Fail("Kỹ thuật viên bàn giao hoặc người dùng ký nhận không tồn tại hoặc đã ngừng công tác.");

        lenh.NguoiBanGiaoId = banGiaoId;
        lenh.NguoiKyNhanId = kyNhanId;
        lenh.ThoiDiemNghiemThu = LocalNow;
        lenh.TinhTrangSauBaoTri = req.TinhTrangSauBaoTri;
        lenh.NhanXetNghiemThu = req.NhanXetNghiemThu?.Trim();
        lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DaNghiemThu;

        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã lập biên bản nghiệm thu bàn giao thiết bị.");
    }

    public async Task<ApiResponse<bool>> DongLenhAsync(string lenhBaoTriId, string nguoiDuyetDongId, int actorTaiKhoanId = 0)
    {
        var lenh = await db.LENH_BAO_TRI.Include(l => l.ThietBi).FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);
        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaNghiemThu)
            return ApiResponse<bool>.Fail("Chỉ được phê duyệt đóng lệnh sau khi đã nghiệm thu bàn giao (DaNghiemThu).");

        // 5.5.2: CHẶN đóng lệnh nếu phiếu dự trù & xuất dùng phụ tùng chưa xác nhận trừ tồn kho hoặc hủy bỏ
        bool coPhieuVatTuChuaChot = await db.PHIEU_DU_TRU_XUAT_DUNG.AnyAsync(p =>
            p.LenhBaoTriId == lenhBaoTriId
            && p.TrangThaiPhieu != CmmsConstants.TrangThaiPhieuXuat.DaXacNhanTruTon
            && p.TrangThaiPhieu != CmmsConstants.TrangThaiPhieuXuat.DaHuy);
        if (coPhieuVatTuChuaChot)
            return ApiResponse<bool>.Fail("Không thể đóng lệnh: Phiếu dự trù/xuất dùng phụ tùng của lệnh vẫn chưa được xác nhận trừ tồn kho hoặc hủy bỏ.");

        var now = LocalNow;
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DaDong;
            lenh.NguoiDuyetDongId = nguoiDuyetDongId;

            if (lenh.LoaiLenh == CmmsConstants.LoaiLenh.BaoTriSuCo && !string.IsNullOrEmpty(lenh.YeuCauId))
            {
                var yc = await db.YEU_CAU_BAO_TRI.FindAsync(lenh.YeuCauId);
                if (yc != null) yc.TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.HoanThanh;
            }
            else if (lenh.LoaiLenh == CmmsConstants.LoaiLenh.BaoTriPhongNgua && !string.IsNullOrEmpty(lenh.KeHoachId))
            {
                var kh = await db.KE_HOACH_BAO_TRI.FindAsync(lenh.KeHoachId);
                if (kh != null)
                {
                    var ngayHoanThanh = DateOnly.FromDateTime(lenh.ThoiDiemThucTeKetThuc ?? now);
                    kh.NgayHoanThanhGanNhat = ngayHoanThanh;
                    kh.NgayDenHanTiepTheo = MaintenanceScheduleHelper.CalculateNextDueDateAfterCompletion(
                        kh.NgayDenHanTiepTheo,
                        ngayHoanThanh,
                        kh.DonViChuKy,
                        kh.GiaTriChuKy,
                        kh.NgayBatDauApDung);
                }
            }

            if (lenh.ThietBi.TrangThaiVanHanh is CmmsConstants.TrangThaiThietBi.DangBaoTri or CmmsConstants.TrangThaiThietBi.DungDoSuCo)
            {
                bool conLenhDangChay = await db.LENH_BAO_TRI.AnyAsync(l =>
                    l.ThietBiId == lenh.ThietBiId && l.LenhBaoTriId != lenhBaoTriId
                    && (l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien || l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.TamDung));

                if (conLenhDangChay)
                {
                    lenh.ThietBi.TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DangBaoTri;
                }
                else
                {
                    bool conYcDungMay = await db.YEU_CAU_BAO_TRI.AnyAsync(y =>
                        y.ThietBiId == lenh.ThietBiId
                        && y.YeuCauId != lenh.YeuCauId
                        && y.MayDangDung
                        && (y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.ChoTiepNhan
                            || y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DaPheDuyet
                            || y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DangXuLy));

                    lenh.ThietBi.TrangThaiVanHanh = conYcDungMay
                        ? CmmsConstants.TrangThaiThietBi.DungDoSuCo
                        : CmmsConstants.TrangThaiThietBi.DangHoatDong;
                }
            }

            await db.SaveChangesAsync();
            await audit.WriteAsync(
                actorTaiKhoanId > 0 ? actorTaiKhoanId : null,
                CmmsConstants.LoaiThaoTacLog.DuyetDongLenh,
                lenhBaoTriId,
                $"Phê duyệt đóng lệnh bảo trì {lenhBaoTriId} ({lenh.ThietBiId}).");
            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã phê duyệt đóng lệnh, khôi phục trạng thái máy và cập nhật chu kỳ bảo trì.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi đóng lệnh bảo trì: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> HuyLenhAsync(string lenhBaoTriId, HuyLenhReq req, int actorTaiKhoanId = 0)
    {
        var lenh = await db.LENH_BAO_TRI
            .Include(l => l.ThietBi)
            .Include(l => l.PhanCongKtvs)
            .Include(l => l.PhieuDuTruXuatDungs).ThenInclude(p => p.ChiTiets)
            .FirstOrDefaultAsync(l => l.LenhBaoTriId == lenhBaoTriId);

        if (lenh == null) return ApiResponse<bool>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy or CmmsConstants.TrangThaiLenh.DaNghiemThu)
            return ApiResponse<bool>.Fail("Không thể hủy lệnh đã nghiệm thu, đã đóng hoặc đã hủy.");

        if (lenh.PhieuDuTruXuatDungs.Any(p => p.TrangThaiPhieu == CmmsConstants.TrangThaiPhieuXuat.DaXacNhanTruTon))
            return ApiResponse<bool>.Fail("Không thể hủy lệnh vì đã có phiếu phụ tùng xác nhận trừ tồn kho.");

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var phieu in lenh.PhieuDuTruXuatDungs.Where(p => p.TrangThaiPhieu != CmmsConstants.TrangThaiPhieuXuat.DaHuy))
            {
                foreach (var ct in phieu.ChiTiets)
                {
                    var pt = await db.PHU_TUNG.FindAsync(ct.PhuTungId);
                    if (pt != null)
                        pt.SoLuongDangDuTru = Math.Max(0, pt.SoLuongDangDuTru - ct.SoLuongDuTru);
                }
                phieu.TrangThaiPhieu = CmmsConstants.TrangThaiPhieuXuat.DaHuy;
            }

            var ktvIds = new List<string>();
            foreach (var pc in lenh.PhanCongKtvs.Where(p => p.TrangThaiPhanCong != CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen))
            {
                pc.TrangThaiPhanCong = CmmsConstants.TrangThaiPhanCong.HuyDieuChuyen;
                ktvIds.Add(pc.KyThuatVienId);
            }

            lenh.TrangThaiLenh = CmmsConstants.TrangThaiLenh.DaHuy;
            if (!string.IsNullOrWhiteSpace(req.LyDoHuy))
                lenh.LyDoTamDung = $"Đã hủy: {req.LyDoHuy.Trim()}";

            if (lenh.LoaiLenh == CmmsConstants.LoaiLenh.BaoTriSuCo && !string.IsNullOrEmpty(lenh.YeuCauId))
            {
                var yc = await db.YEU_CAU_BAO_TRI.FindAsync(lenh.YeuCauId);
                if (yc != null && yc.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DangXuLy)
                    yc.TrangThaiYeuCau = CmmsConstants.TrangThaiYeuCau.DaPheDuyet;
            }

            if (lenh.ThietBi.TrangThaiVanHanh is CmmsConstants.TrangThaiThietBi.DangBaoTri or CmmsConstants.TrangThaiThietBi.DungDoSuCo)
            {
                bool otherActiveWo = await db.LENH_BAO_TRI.AnyAsync(l =>
                    l.ThietBiId == lenh.ThietBiId && l.LenhBaoTriId != lenhBaoTriId
                    && (l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien || l.TrangThaiLenh == CmmsConstants.TrangThaiLenh.TamDung));

                if (otherActiveWo)
                {
                    lenh.ThietBi.TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DangBaoTri;
                }
                else
                {
                    bool stoppingYc = await db.YEU_CAU_BAO_TRI.AnyAsync(y =>
                        y.ThietBiId == lenh.ThietBiId && y.MayDangDung
                        && (y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.ChoTiepNhan
                            || y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DaPheDuyet
                            || y.TrangThaiYeuCau == CmmsConstants.TrangThaiYeuCau.DangXuLy));

                    lenh.ThietBi.TrangThaiVanHanh = stoppingYc
                        ? CmmsConstants.TrangThaiThietBi.DungDoSuCo
                        : CmmsConstants.TrangThaiThietBi.DangHoatDong;
                }
            }

            await db.SaveChangesAsync();
            await RefreshKtvReadinessAsync(ktvIds);
            await db.SaveChangesAsync();
            await audit.WriteAsync(
                actorTaiKhoanId > 0 ? actorTaiKhoanId : null,
                CmmsConstants.LoaiThaoTacLog.HuyLenhBaoTri,
                lenhBaoTriId,
                $"Hủy lệnh bảo trì {lenhBaoTriId}. Lý do: {req.LyDoHuy ?? "Không nêu"}");
            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã hủy lệnh bảo trì, giải phóng giữ chỗ vật tư và cập nhật lại trạng thái liên quan.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi hủy lệnh bảo trì: {ex.Message}");
        }
    }

    private async Task RefreshKtvReadinessAsync(IEnumerable<string> ktvIds)
    {
        var ids = ktvIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (ids.Count == 0) return;

        var busyKtvs = await db.PHAN_CONG_KTV
            .Where(p => ids.Contains(p.KyThuatVienId)
                     && p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam
                     && p.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien)
            .Select(p => p.KyThuatVienId)
            .Distinct()
            .ToHashSetAsync();

        var ktvs = await db.NHAN_VIEN.Where(n => ids.Contains(n.NhanVienId)).ToListAsync();
        foreach (var ktv in ktvs)
        {
            if (ktv.TrangThaiSanSang == CmmsConstants.TrangThaiSanSang.NghiPhep) continue;
            ktv.TrangThaiSanSang = busyKtvs.Contains(ktv.NhanVienId)
                ? CmmsConstants.TrangThaiSanSang.DangBan
                : CmmsConstants.TrangThaiSanSang.SanSang;
        }
    }

    private static bool IsWithinShift(string? ca, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(ca)) return true;
        return ca switch
        {
            CmmsConstants.CaLamViec.Ca1 => start.TimeOfDay >= TimeSpan.FromHours(6) && end.TimeOfDay <= TimeSpan.FromHours(14) && start.Date == end.Date,
            CmmsConstants.CaLamViec.Ca2 => start.TimeOfDay >= TimeSpan.FromHours(14) && end.TimeOfDay <= TimeSpan.FromHours(22) && start.Date == end.Date,
            CmmsConstants.CaLamViec.HanhChinh => start.TimeOfDay >= TimeSpan.FromHours(8) && end.TimeOfDay <= TimeSpan.FromHours(17) && start.Date == end.Date,
            CmmsConstants.CaLamViec.Ca3 =>
                (start.TimeOfDay >= TimeSpan.FromHours(22)
                    && (end.Date == start.Date || (end.Date == start.Date.AddDays(1) && end.TimeOfDay <= TimeSpan.FromHours(6))))
                || (start.TimeOfDay < TimeSpan.FromHours(6)
                    && end.Date == start.Date && end.TimeOfDay <= TimeSpan.FromHours(6)),
            _ => true
        };
    }

    private static bool ComputeIsTreHan(LENH_BAO_TRI l, DateTime now)
        => l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaHuy && (
            l.ThoiDiemThucTeKetThuc.HasValue
                ? l.ThoiDiemThucTeKetThuc.Value > l.ThoiGianDuKienKetThuc
                : l.TrangThaiLenh != CmmsConstants.TrangThaiLenh.DaDong && now > l.ThoiGianDuKienKetThuc);

    internal static LenhBaoTriDto MapDto(LENH_BAO_TRI l, DateTime? now = null) => new()
    {
        LenhBaoTriId = l.LenhBaoTriId,
        ThietBiId = l.ThietBiId,
        TenThietBi = l.ThietBi.TenThietBi,
        LoaiLenh = l.LoaiLenh,
        YeuCauId = l.YeuCauId,
        KeHoachId = l.KeHoachId,
        NoiDungCongViec = l.NoiDungCongViec,
        MucUuTienId = l.MucUuTienId,
        TenMucUuTien = l.MucUuTien.TenMucUuTien,
        MaMauHienThi = l.MucUuTien.MaMauHienThi,
        ThoiGianDuKienBatDau = l.ThoiGianDuKienBatDau,
        ThoiGianDuKienKetThuc = l.ThoiGianDuKienKetThuc,
        ThoiDiemThucTeBatDau = l.ThoiDiemThucTeBatDau,
        ThoiDiemThucTeKetThuc = l.ThoiDiemThucTeKetThuc,
        LyDoTamDung = l.LyDoTamDung,
        TrangThaiLenh = l.TrangThaiLenh,
        TinhTrangSauBaoTri = l.TinhTrangSauBaoTri,
        NguoiBanGiaoId = l.NguoiBanGiaoId,
        TenNguoiBanGiao = l.NguoiBanGiao?.HoTen,
        NguoiKyNhanId = l.NguoiKyNhanId,
        TenNguoiKyNhan = l.NguoiKyNhan?.HoTen,
        ThoiDiemNghiemThu = l.ThoiDiemNghiemThu,
        NhanXetNghiemThu = l.NhanXetNghiemThu,
        NguoiTaoId = l.NguoiTaoId,
        NguoiDuyetDongId = l.NguoiDuyetDongId,
        IsTreHan = ComputeIsTreHan(l, now ?? DateTime.Now)
    };
}
