using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CMMS.Backend.Services.Implementations;

public class KhoService(
    CmmsDbContext db,
    AuditLogHelper audit,
    TimeProvider? timeProvider = null,
    IOptions<InventorySettings>? invOptions = null) : IKhoService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly InventorySettings _settings = invOptions?.Value ?? new InventorySettings();
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

    public async Task<ApiResponse<List<PhuTungDto>>> GetPhuTungAsync(string? keyword, string? loai, bool chiDuoiToiThieu)
    {
        var q = db.PHU_TUNG.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(p => p.PhuTungId.Contains(kw) || p.TenPhuTung.Contains(kw) || p.QuyCach.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(loai)) q = q.Where(p => p.LoaiPhuTung == loai);
        if (chiDuoiToiThieu) q = q.Where(p => p.SoLuongTonHienCo <= p.MucTonToiThieu);

        var list = await q.OrderBy(p => p.PhuTungId)
            .Select(p => MapPhuTungDto(p))
            .ToListAsync();
        return ApiResponse<List<PhuTungDto>>.Ok(list);
    }

    public async Task<ApiResponse<PhuTungDto>> CreatePhuTungAsync(PhuTungUpsertReq req)
    {
        if (string.IsNullOrWhiteSpace(req.PhuTungId) || string.IsNullOrWhiteSpace(req.TenPhuTung)
            || string.IsNullOrWhiteSpace(req.QuyCach) || string.IsNullOrWhiteSpace(req.DonViTinh))
            return ApiResponse<PhuTungDto>.Fail("Vui lòng nhập đầy đủ mã, tên, quy cách và đơn vị tính phụ tùng.");
        if (!IsValidLoaiPhuTung(req.LoaiPhuTung))
            return ApiResponse<PhuTungDto>.Fail("Loại phụ tùng không hợp lệ.");
        if (req.SoLuongTonHienCo < 0 || req.MucTonToiThieu < 0)
            return ApiResponse<PhuTungDto>.Fail("Số lượng tồn và mức tồn tối thiểu không được âm.");
        if (await db.PHU_TUNG.AnyAsync(p => p.PhuTungId == req.PhuTungId.Trim()))
            return ApiResponse<PhuTungDto>.Fail($"Mã phụ tùng '{req.PhuTungId}' đã tồn tại.");

        var entity = new PHU_TUNG
        {
            PhuTungId = req.PhuTungId.Trim(),
            TenPhuTung = req.TenPhuTung.Trim(),
            LoaiPhuTung = req.LoaiPhuTung,
            QuyCach = req.QuyCach.Trim(),
            DonViTinh = req.DonViTinh.Trim(),
            SoLuongTonHienCo = req.SoLuongTonHienCo,
            SoLuongDangDuTru = 0,
            MucTonToiThieu = req.MucTonToiThieu,
            MoTa = req.MoTa?.Trim()
        };
        db.PHU_TUNG.Add(entity);
        await db.SaveChangesAsync();
        return ApiResponse<PhuTungDto>.Ok(MapPhuTungDto(entity), "Thêm danh mục phụ tùng thành công.");
    }

    public async Task<ApiResponse<PhuTungDto>> UpdatePhuTungAsync(string phuTungId, PhuTungUpsertReq req)
    {
        var entity = await db.PHU_TUNG.FindAsync(phuTungId);
        if (entity == null) return ApiResponse<PhuTungDto>.Fail("Không tìm thấy phụ tùng.");
        if (string.IsNullOrWhiteSpace(req.TenPhuTung) || string.IsNullOrWhiteSpace(req.QuyCach) || string.IsNullOrWhiteSpace(req.DonViTinh))
            return ApiResponse<PhuTungDto>.Fail("Tên, quy cách và đơn vị tính phụ tùng không được trống.");
        if (!IsValidLoaiPhuTung(req.LoaiPhuTung))
            return ApiResponse<PhuTungDto>.Fail("Loại phụ tùng không hợp lệ.");
        if (req.MucTonToiThieu < 0)
            return ApiResponse<PhuTungDto>.Fail("Mức tồn tối thiểu không được âm.");

        var warnings = new List<string>();
        if (req.SoLuongTonHienCo != entity.SoLuongTonHienCo)
            warnings.Add("Lưu ý: Số lượng tồn kho hiện có không thể sửa trực tiếp trên danh mục; hãy lập Phiếu cập nhật tồn kho / kiểm kê.");

        // Không cho phép tự ý sửa trực tiếp SoLuongTonHienCo khi cập nhật danh mục nếu đã có phiếu kho
        entity.TenPhuTung = req.TenPhuTung.Trim();
        entity.LoaiPhuTung = req.LoaiPhuTung;
        entity.QuyCach = req.QuyCach.Trim();
        entity.DonViTinh = req.DonViTinh.Trim();
        entity.MucTonToiThieu = req.MucTonToiThieu;
        entity.MoTa = req.MoTa?.Trim();

        await db.SaveChangesAsync();
        return ApiResponse<PhuTungDto>.Ok(MapPhuTungDto(entity), "Cập nhật thông tin phụ tùng thành công.", warnings);
    }

    public async Task<ApiResponse<bool>> DeletePhuTungAsync(string phuTungId)
    {
        var entity = await db.PHU_TUNG.FindAsync(phuTungId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy phụ tùng.");

        if (await db.DINH_MUC_LINH_KIEN.AnyAsync(d => d.PhuTungId == phuTungId)
            || await db.CHI_TIET_CAP_NHAT_TON_KHO.AnyAsync(c => c.PhuTungId == phuTungId)
            || await db.CHI_TIET_PHIEU_XUAT.AnyAsync(c => c.PhuTungId == phuTungId))
            return ApiResponse<bool>.Fail("Không thể xóa phụ tùng đã phát sinh định mức hoặc phiếu xuất/nhập kho.");

        db.PHU_TUNG.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa phụ tùng.");
    }

    public async Task<ApiResponse<List<DinhMucLinhKienDto>>> GetDinhMucByThietBiAsync(string thietBiId, bool chiThayTheDinhKy)
    {
        var q = db.DINH_MUC_LINH_KIEN.AsNoTracking()
            .Include(d => d.PhuTung)
            .Where(d => d.ThietBiId == thietBiId);

        if (chiThayTheDinhKy) q = q.Where(d => d.ThayTheDinhKy);

        var list = await q.OrderByDescending(d => d.ThayTheDinhKy).ThenBy(d => d.PhuTungId)
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
        return ApiResponse<List<DinhMucLinhKienDto>>.Ok(list);
    }

    public async Task<ApiResponse<DinhMucLinhKienDto>> UpsertDinhMucAsync(DinhMucUpsertReq req)
    {
        if (!await db.THIET_BI.AnyAsync(t => t.ThietBiId == req.ThietBiId))
            return ApiResponse<DinhMucLinhKienDto>.Fail("Không tìm thấy thiết bị.");
        var pt = await db.PHU_TUNG.FindAsync(req.PhuTungId);
        if (pt == null) return ApiResponse<DinhMucLinhKienDto>.Fail("Không tìm thấy phụ tùng.");
        if (req.SoLuongDinhMuc <= 0)
            return ApiResponse<DinhMucLinhKienDto>.Fail("Số lượng định mức phải > 0.");

        var entity = await db.DINH_MUC_LINH_KIEN.FirstOrDefaultAsync(d =>
            d.ThietBiId == req.ThietBiId && d.PhuTungId == req.PhuTungId);

        if (entity == null)
        {
            entity = new DINH_MUC_LINH_KIEN
            {
                ThietBiId = req.ThietBiId,
                PhuTungId = req.PhuTungId
            };
            db.DINH_MUC_LINH_KIEN.Add(entity);
        }

        entity.SoLuongDinhMuc = req.SoLuongDinhMuc;
        entity.ThayTheDinhKy = req.ThayTheDinhKy;
        entity.ViTriLapDat = req.ViTriLapDat?.Trim();

        await db.SaveChangesAsync();
        return ApiResponse<DinhMucLinhKienDto>.Ok(new DinhMucLinhKienDto
        {
            DinhMucId = entity.DinhMucId,
            ThietBiId = entity.ThietBiId,
            PhuTungId = entity.PhuTungId,
            TenPhuTung = pt.TenPhuTung,
            DonViTinh = pt.DonViTinh,
            SoLuongDinhMuc = entity.SoLuongDinhMuc,
            ThayTheDinhKy = entity.ThayTheDinhKy,
            ViTriLapDat = entity.ViTriLapDat,
            TonKhoKhaDung = pt.SoLuongTonHienCo - pt.SoLuongDangDuTru
        }, "Đã lưu định mức linh kiện thiết bị.");
    }

    public async Task<ApiResponse<bool>> DeleteDinhMucAsync(long dinhMucId)
    {
        var entity = await db.DINH_MUC_LINH_KIEN.FindAsync(dinhMucId);
        if (entity == null) return ApiResponse<bool>.Fail("Không tìm thấy định mức linh kiện.");

        db.DINH_MUC_LINH_KIEN.Remove(entity);
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa định mức linh kiện.");
    }

    public async Task<ApiResponse<List<PhieuCapNhatTonKhoDto>>> GetPhieuCapNhatTonKhoAsync(string? trangThai)
    {
        var q = db.PHIEU_CAP_NHAT_TON_KHO.AsNoTracking()
            .Include(p => p.NguoiLap)
            .Include(p => p.NguoiXacNhan)
            .Include(p => p.ChiTiets).ThenInclude(c => c.PhuTung)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(p => p.TrangThaiPhieu == trangThai);

        var items = await q.OrderByDescending(p => p.ThoiDiemLap).ToListAsync();
        return ApiResponse<List<PhieuCapNhatTonKhoDto>>.Ok(items.Select(MapCapNhatKhoDto).ToList());
    }

    public async Task<ApiResponse<PhieuCapNhatTonKhoDto>> TaoPhieuCapNhatTonKhoAsync(TaoPhieuCapNhatTonKhoReq req, string nguoiLapId)
    {
        if (req.LoaiCapNhat is not (CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai or CmmsConstants.LoaiCapNhatKho.DieuChinhKiemKe))
            return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Loại cập nhật tồn kho không hợp lệ.");
        if (req.ChiTiet.Count == 0)
            return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Phiếu cập nhật phải có ít nhất 1 phụ tùng.");

        if (req.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai)
        {
            if (string.IsNullOrWhiteSpace(req.MaPhieuKhoNgoai))
                return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Bổ sung tồn kho từ kho ngoài bắt buộc nhập Mã phiếu kho ngoài tham chiếu.");

            var maKhoNgoai = req.MaPhieuKhoNgoai.Trim();
            bool dupRef = await db.PHIEU_CAP_NHAT_TON_KHO.AnyAsync(p =>
                p.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai
                && p.MaPhieuKhoNgoai == maKhoNgoai
                && (p.TrangThaiPhieu == CmmsConstants.TrangThaiCapNhatKho.ChoXacNhan
                    || p.TrangThaiPhieu == CmmsConstants.TrangThaiCapNhatKho.DaXacNhan));
            if (dupRef)
                return ApiResponse<PhieuCapNhatTonKhoDto>.Fail($"Mã phiếu kho ngoài '{maKhoNgoai}' đã được ghi nhận trước đó.");
        }

        var localNow = _timeProvider.GetLocalNow();
        if (req.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai && _settings.EnforceReplenishmentWindow)
        {
            var startTod = TimeSpan.FromHours(_settings.ReplenishmentStartHour);
            var endTod = TimeSpan.FromHours(_settings.ReplenishmentEndHour);
            if (localNow.TimeOfDay < startTod || localNow.TimeOfDay > endTod)
            {
                return ApiResponse<PhieuCapNhatTonKhoDto>.Fail(
                    $"Phiếu bổ sung tồn kho từ kho ngoài chỉ được lập vào khung giờ cố định đầu ngày từ {_settings.ReplenishmentStartHour:D2}:00 đến {_settings.ReplenishmentEndHour:D2}:00 sáng (hiện tại: {localNow:HH:mm}).");
            }
        }

        if (req.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.DieuChinhKiemKe && string.IsNullOrWhiteSpace(req.GhiChu))
            return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Điều chỉnh kiểm kê bắt buộc nhập ghi chú giải trình.");

        if (req.ChiTiet.Select(x => x.PhuTungId).Distinct().Count() != req.ChiTiet.Count)
            return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Không được chọn trùng mã phụ tùng trong cùng một phiếu.");

        foreach (var ct in req.ChiTiet)
        {
            if (!await db.PHU_TUNG.AnyAsync(p => p.PhuTungId == ct.PhuTungId))
                return ApiResponse<PhieuCapNhatTonKhoDto>.Fail($"Không tìm thấy phụ tùng '{ct.PhuTungId}'.");
            if (req.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai && ct.SoLuongThayDoi <= 0)
                return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Số lượng bổ sung từ kho ngoài phải > 0.");
            if (req.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.DieuChinhKiemKe && ct.SoLuongThayDoi == 0)
                return ApiResponse<PhieuCapNhatTonKhoDto>.Fail("Số lượng điều chỉnh kiểm kê phải khác 0.");
        }

        var maPhieu = await CodeGeneratorHelper.NextMonthlyCodeAsync(
            db.PHIEU_CAP_NHAT_TON_KHO.Select(p => p.PhieuCapNhatId), "CNK", localNow.DateTime);
        var entity = new PHIEU_CAP_NHAT_TON_KHO
        {
            PhieuCapNhatId = maPhieu,
            LoaiCapNhat = req.LoaiCapNhat,
            MaPhieuKhoNgoai = req.MaPhieuKhoNgoai?.Trim(),
            ThoiDiemLap = localNow.DateTime,
            NguoiLapId = nguoiLapId,
            TrangThaiPhieu = CmmsConstants.TrangThaiCapNhatKho.ChoXacNhan,
            GhiChu = req.GhiChu?.Trim(),
            ChiTiets = req.ChiTiet.Select(c => new CHI_TIET_CAP_NHAT_TON_KHO
            {
                PhuTungId = c.PhuTungId,
                SoLuongThayDoi = c.SoLuongThayDoi,
                GhiChu = c.GhiChu?.Trim()
            }).ToList()
        };

        // 6.3: Chưa cộng tồn kho ở bước lập; chỉ cập nhật tồn sau khi Quản lý nhà máy xác nhận
        db.PHIEU_CAP_NHAT_TON_KHO.Add(entity);
        await db.SaveChangesAsync();

        var saved = await db.PHIEU_CAP_NHAT_TON_KHO.AsNoTracking()
            .Include(p => p.NguoiLap).Include(p => p.NguoiXacNhan)
            .Include(p => p.ChiTiets).ThenInclude(c => c.PhuTung)
            .FirstAsync(p => p.PhieuCapNhatId == maPhieu);
        return ApiResponse<PhieuCapNhatTonKhoDto>.Ok(MapCapNhatKhoDto(saved), "Đã lập phiếu cập nhật tồn kho, đang chờ Quản lý nhà máy xác nhận.");
    }

    public async Task<ApiResponse<bool>> XacNhanCapNhatTonKhoAsync(
        string phieuId, bool laXacNhan, string nguoiXacNhanId, int actorTaiKhoanId)
    {
        var phieu = await db.PHIEU_CAP_NHAT_TON_KHO
            .Include(p => p.ChiTiets)
            .FirstOrDefaultAsync(p => p.PhieuCapNhatId == phieuId);

        if (phieu == null) return ApiResponse<bool>.Fail("Không tìm thấy phiếu cập nhật tồn kho.");
        if (phieu.TrangThaiPhieu != CmmsConstants.TrangThaiCapNhatKho.ChoXacNhan)
            return ApiResponse<bool>.Fail("Phiếu cập nhật tồn kho đã được xử lý trước đó.");
        if (phieu.NguoiLapId == nguoiXacNhanId)
            return ApiResponse<bool>.Fail("Vi phạm kiểm soát chéo: Người xác nhận cập nhật tồn kho không được trùng với người lập phiếu.");

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            phieu.NguoiXacNhanId = nguoiXacNhanId;
            phieu.ThoiDiemXacNhan = LocalNow;

            if (!laXacNhan)
            {
                phieu.TrangThaiPhieu = CmmsConstants.TrangThaiCapNhatKho.TuChoi;
                await db.SaveChangesAsync();
                await audit.WriteAsync(actorTaiKhoanId, CmmsConstants.LoaiThaoTacLog.TuChoiCapNhatTonKho, phieuId,
                    $"Từ chối phiếu cập nhật tồn kho ({phieu.LoaiCapNhat}).");
                await tx.CommitAsync();
                return ApiResponse<bool>.Ok(true, "Đã từ chối phiếu cập nhật tồn kho.");
            }

            var warnings = new List<string>();
            foreach (var ct in phieu.ChiTiets)
            {
                var pt = await db.PHU_TUNG.FindAsync(ct.PhuTungId);
                if (pt == null) return ApiResponse<bool>.Fail($"Không tìm thấy phụ tùng '{ct.PhuTungId}'.");
                if (pt.SoLuongTonHienCo + ct.SoLuongThayDoi < 0)
                    return ApiResponse<bool>.Fail($"Phụ tùng '{pt.TenPhuTung}' không thể điều chỉnh làm tồn kho bị âm.");

                pt.SoLuongTonHienCo += ct.SoLuongThayDoi;
                if (pt.SoLuongTonHienCo <= pt.MucTonToiThieu)
                    warnings.Add($"Tồn kho thấp: {pt.TenPhuTung} (Hiện có: {pt.SoLuongTonHienCo} / Tối thiểu: {pt.MucTonToiThieu})");
                if (pt.SoLuongTonHienCo < pt.SoLuongDangDuTru)
                    warnings.Add($"Cảnh báo tồn khả dụng âm: {pt.TenPhuTung} (Hiện có: {pt.SoLuongTonHienCo} < Đang giữ chỗ dự trù: {pt.SoLuongDangDuTru})");
            }

            phieu.TrangThaiPhieu = CmmsConstants.TrangThaiCapNhatKho.DaXacNhan;
            await db.SaveChangesAsync();

            var loaiLog = phieu.LoaiCapNhat == CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai
                ? CmmsConstants.LoaiThaoTacLog.XacNhanBoSungTonKho
                : CmmsConstants.LoaiThaoTacLog.DieuChinhKiemKe;

            await audit.WriteAsync(actorTaiKhoanId, loaiLog, phieuId,
                $"Quản lý nhà máy xác nhận phiếu {phieu.LoaiCapNhat} (phiếu ngoài: {phieu.MaPhieuKhoNgoai ?? "N/A"}).");

            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã xác nhận cập nhật tồn kho và ghi nhật ký hệ thống.", warnings);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi xác nhận cập nhật kho: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<PhieuDuTruXuatDto>>> GetPhieuXuatByLenhAsync(string? lenhBaoTriId, string? trangThai)
    {
        var q = db.PHIEU_DU_TRU_XUAT_DUNG.AsNoTracking()
            .Include(p => p.NguoiLapDuTru)
            .Include(p => p.ChiTiets).ThenInclude(c => c.PhuTung)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(lenhBaoTriId)) q = q.Where(p => p.LenhBaoTriId == lenhBaoTriId);
        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(p => p.TrangThaiPhieu == trangThai);

        var list = await q.OrderByDescending(p => p.ThoiDiemLap).ToListAsync();
        return ApiResponse<List<PhieuDuTruXuatDto>>.Ok(list.Select(MapPhieuXuatDto).ToList());
    }

    public async Task<ApiResponse<PhieuDuTruXuatDto>> TaoPhieuDuTruAsync(TaoPhieuDuTruReq req, string nguoiLapId)
    {
        var lenh = await db.LENH_BAO_TRI.FindAsync(req.LenhBaoTriId);
        if (lenh == null) return ApiResponse<PhieuDuTruXuatDto>.Fail("Không tìm thấy lệnh bảo trì.");
        if (lenh.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy or CmmsConstants.TrangThaiLenh.DaNghiemThu)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Không thể lập phiếu dự trù cho lệnh đã nghiệm thu, đã đóng hoặc đã hủy.");
        if (req.ChiTiet.Count == 0)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Phiếu dự trù phải có ít nhất 1 phụ tùng.");
        if (req.ChiTiet.Select(x => x.PhuTungId).Distinct().Count() != req.ChiTiet.Count)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Không được chọn trùng mã phụ tùng trong cùng phiếu dự trù.");

        var now = LocalNow;
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var warnings = new List<string>();
            var maPhieu = await CodeGeneratorHelper.NextMonthlyCodeAsync(db.PHIEU_DU_TRU_XUAT_DUNG.Select(p => p.PhieuXuatId), "PX", now);
            var chiTiets = new List<CHI_TIET_PHIEU_XUAT>();

            foreach (var item in req.ChiTiet)
            {
                if (item.SoLuongDuTru <= 0)
                    return ApiResponse<PhieuDuTruXuatDto>.Fail("Số lượng dự trù phải > 0.");

                var pt = await db.PHU_TUNG.FindAsync(item.PhuTungId);
                if (pt == null) return ApiResponse<PhieuDuTruXuatDto>.Fail($"Không tìm thấy phụ tùng '{item.PhuTungId}'.");

                var khaDung = pt.SoLuongTonHienCo - pt.SoLuongDangDuTru;
                if (item.SoLuongDuTru > khaDung)
                    warnings.Add($"Thiếu tồn khả dụng: {pt.TenPhuTung} (Cần: {item.SoLuongDuTru}, Khả dụng: {khaDung}).");

                pt.SoLuongDangDuTru += item.SoLuongDuTru;
                chiTiets.Add(new CHI_TIET_PHIEU_XUAT
                {
                    PhieuXuatId = maPhieu,
                    PhuTungId = pt.PhuTungId,
                    SoLuongDuTru = item.SoLuongDuTru,
                    GhiChuGiaiTrinh = item.GhiChuGiaiTrinh?.Trim()
                });
            }

            db.PHIEU_DU_TRU_XUAT_DUNG.Add(new PHIEU_DU_TRU_XUAT_DUNG
            {
                PhieuXuatId = maPhieu,
                LenhBaoTriId = req.LenhBaoTriId,
                NguoiLapDuTruId = nguoiLapId,
                ThoiDiemLap = now,
                TrangThaiPhieu = CmmsConstants.TrangThaiPhieuXuat.DangDuTru,
                GhiChu = req.GhiChu?.Trim(),
                ChiTiets = chiTiets
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            var dto = await LoadPhieuXuatDtoAsync(maPhieu);
            return ApiResponse<PhieuDuTruXuatDto>.Ok(dto!,
                warnings.Count > 0 ? "Đã lập phiếu dự trù và giữ chỗ (có phụ tùng đang thiếu tồn khả dụng)." : "Đã lập phiếu dự trù và giữ chỗ phụ tùng đủ tồn.",
                warnings);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<PhieuDuTruXuatDto>.Fail($"Lỗi lập phiếu dự trù: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> HuyPhieuDuTruAsync(string phieuXuatId)
    {
        var phieu = await db.PHIEU_DU_TRU_XUAT_DUNG.Include(p => p.ChiTiets).FirstOrDefaultAsync(p => p.PhieuXuatId == phieuXuatId);
        if (phieu == null) return ApiResponse<bool>.Fail("Không tìm thấy phiếu xuất phụ tùng.");
        if (phieu.TrangThaiPhieu is CmmsConstants.TrangThaiPhieuXuat.DaXacNhanTruTon or CmmsConstants.TrangThaiPhieuXuat.DaHuy)
            return ApiResponse<bool>.Fail("Không thể hủy phiếu đã xác nhận trừ tồn hoặc đã hủy.");

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var ct in phieu.ChiTiets)
            {
                var pt = await db.PHU_TUNG.FindAsync(ct.PhuTungId);
                if (pt != null)
                    pt.SoLuongDangDuTru = Math.Max(0, pt.SoLuongDangDuTru - ct.SoLuongDuTru);
            }

            phieu.TrangThaiPhieu = CmmsConstants.TrangThaiPhieuXuat.DaHuy;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Đã hủy phiếu dự trù và giải phóng giữ chỗ tồn kho.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi hủy phiếu dự trù: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PhieuDuTruXuatDto>> KhaiBaoThucDungAsync(string phieuXuatId, KhaiBaoThucDungReq req, string ktvId)
    {
        var phieu = await db.PHIEU_DU_TRU_XUAT_DUNG
            .Include(p => p.LenhBaoTri)
            .Include(p => p.ChiTiets)
            .FirstOrDefaultAsync(p => p.PhieuXuatId == phieuXuatId);
        if (phieu == null) return ApiResponse<PhieuDuTruXuatDto>.Fail("Không tìm thấy phiếu xuất dùng.");
        if (phieu.LenhBaoTri.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Lệnh bảo trì đã đóng hoặc hủy, không thể khai báo thực dùng.");
        if (phieu.TrangThaiPhieu is not (CmmsConstants.TrangThaiPhieuXuat.DangDuTru or CmmsConstants.TrangThaiPhieuXuat.DaKhaiBaoThucDung))
            return ApiResponse<PhieuDuTruXuatDto>.Fail($"Không thể khai báo thực dùng khi phiếu ở trạng thái '{phieu.TrangThaiPhieu}'.");
        if (req.ChiTiet.Count == 0)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Danh sách khai báo thực dùng không được trống.");
        if (req.ChiTiet.Select(x => x.PhuTungId).Distinct().Count() != req.ChiTiet.Count)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Không được khai báo trùng mã phụ tùng trong cùng một phiếu.");
        if (req.ChiTiet.Count != phieu.ChiTiets.Count)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Phải khai báo số lượng thực dùng cho tất cả phụ tùng trong phiếu (nhập 0 nếu không dùng).");

        bool vuotDuTru = false;
        foreach (var item in req.ChiTiet)
        {
            if (item.SoLuongThucDung < 0)
                return ApiResponse<PhieuDuTruXuatDto>.Fail("Số lượng thực dùng không được âm.");

            var ct = phieu.ChiTiets.FirstOrDefault(c => c.PhuTungId == item.PhuTungId);
            if (ct == null)
                return ApiResponse<PhieuDuTruXuatDto>.Fail($"Phụ tùng '{item.PhuTungId}' không thuộc phiếu dự trù này.");

            if (item.SoLuongThucDung > ct.SoLuongDuTru)
            {
                vuotDuTru = true;
                if (string.IsNullOrWhiteSpace(item.GhiChuGiaiTrinh))
                    return ApiResponse<PhieuDuTruXuatDto>.Fail($"Phụ tùng '{item.PhuTungId}' dùng vượt dự trù ({item.SoLuongThucDung} > {ct.SoLuongDuTru}) bắt buộc nhập lý do giải trình.");
            }

            ct.SoLuongThucDung = item.SoLuongThucDung;
            ct.GhiChuGiaiTrinh = item.GhiChuGiaiTrinh?.Trim();
        }

        if (phieu.ChiTiets.Any(c => !c.SoLuongThucDung.HasValue))
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Phải khai báo số lượng thực dùng cho tất cả phụ tùng trong phiếu (nhập 0 nếu không dùng).");

        // 5.4.4: Bước khai báo thực dùng KHÔNG trừ tồn kho vật lý
        phieu.NguoiKhaiBaoId = ktvId;
        phieu.ThoiDiemKhaiBao = LocalNow;
        phieu.TrangThaiPhieu = vuotDuTru
            ? CmmsConstants.TrangThaiPhieuXuat.ChoDuyetVuotDuTru
            : CmmsConstants.TrangThaiPhieuXuat.DaKhaiBaoThucDung;

        await db.SaveChangesAsync();
        var dto = await LoadPhieuXuatDtoAsync(phieuXuatId);
        return ApiResponse<PhieuDuTruXuatDto>.Ok(dto!,
            vuotDuTru
                ? "Đã khai báo thực dùng (có vượt dự trù -> chuyển sang ChoDuyetVuotDuTru)."
                : "Đã khai báo số lượng phụ tùng thực dùng, chờ Trưởng BP xác nhận xuất kho.");
    }

    public async Task<ApiResponse<PhieuDuTruXuatDto>> DuyetVuotDuTruAsync(
        string phieuXuatId, DuyetVuotDuTruReq req, string nguoiDuyetId)
    {
        var phieu = await db.PHIEU_DU_TRU_XUAT_DUNG
            .Include(p => p.ChiTiets)
            .FirstOrDefaultAsync(p => p.PhieuXuatId == phieuXuatId);
        if (phieu == null) return ApiResponse<PhieuDuTruXuatDto>.Fail("Không tìm thấy phiếu xuất dùng.");
        if (phieu.TrangThaiPhieu != CmmsConstants.TrangThaiPhieuXuat.ChoDuyetVuotDuTru)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Phiếu không ở trạng thái chờ duyệt vượt dự trù.");

        var now = LocalNow;
        if (!req.LaPheDuyet)
        {
            if (string.IsNullOrWhiteSpace(req.GhiChu))
                return ApiResponse<PhieuDuTruXuatDto>.Fail("Bắt buộc nhập lý do khi từ chối duyệt vượt dự trù để KTV khai báo lại.");

            foreach (var ct in phieu.ChiTiets)
                ct.SoLuongThucDung = null;

            phieu.NguoiDuyetVuotId = nguoiDuyetId;
            phieu.ThoiDiemDuyetVuot = now;
            phieu.TrangThaiPhieu = CmmsConstants.TrangThaiPhieuXuat.DangDuTru;
            phieu.GhiChu = string.IsNullOrWhiteSpace(phieu.GhiChu)
                ? $"[Từ chối vượt dự trù]: {req.GhiChu.Trim()}"
                : $"{phieu.GhiChu} | [Từ chối vượt dự trù]: {req.GhiChu.Trim()}";

            await db.SaveChangesAsync();
            var rejectedDto = await LoadPhieuXuatDtoAsync(phieuXuatId);
            return ApiResponse<PhieuDuTruXuatDto>.Ok(rejectedDto!, "Đã từ chối duyệt vượt dự trù, trả phiếu về DangDuTru để KTV khai báo lại.");
        }

        foreach (var ct in phieu.ChiTiets)
        {
            var pt = await db.PHU_TUNG.FindAsync(ct.PhuTungId);
            decimal thucDung = ct.SoLuongThucDung ?? 0;
            if (pt == null || pt.SoLuongTonHienCo < thucDung)
                return ApiResponse<PhieuDuTruXuatDto>.Fail($"Không thể duyệt vượt dự trù: '{pt?.TenPhuTung ?? ct.PhuTungId}' không đủ tồn kho thực tế (Hiện có: {pt?.SoLuongTonHienCo ?? 0}, Thực dùng: {thucDung}).");
        }

        phieu.NguoiDuyetVuotId = nguoiDuyetId;
        phieu.ThoiDiemDuyetVuot = now;
        phieu.TrangThaiPhieu = CmmsConstants.TrangThaiPhieuXuat.DaKhaiBaoThucDung;

        await db.SaveChangesAsync();
        var dto = await LoadPhieuXuatDtoAsync(phieuXuatId);
        return ApiResponse<PhieuDuTruXuatDto>.Ok(dto!, "Đã duyệt bổ sung vượt dự trù.");
    }

    public async Task<ApiResponse<PhieuDuTruXuatDto>> XacNhanXuatDungAsync(
        string phieuXuatId, string nguoiXacNhanId, int actorTaiKhoanId = 0)
    {
        var phieu = await db.PHIEU_DU_TRU_XUAT_DUNG
            .Include(p => p.LenhBaoTri)
            .Include(p => p.ChiTiets)
            .FirstOrDefaultAsync(p => p.PhieuXuatId == phieuXuatId);
        if (phieu == null) return ApiResponse<PhieuDuTruXuatDto>.Fail("Không tìm thấy phiếu xuất dùng.");
        if (phieu.LenhBaoTri.TrangThaiLenh is CmmsConstants.TrangThaiLenh.DaDong or CmmsConstants.TrangThaiLenh.DaHuy)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Lệnh bảo trì đã đóng hoặc hủy, không thể xác nhận xuất dùng.");
        if (phieu.TrangThaiPhieu != CmmsConstants.TrangThaiPhieuXuat.DaKhaiBaoThucDung)
            return ApiResponse<PhieuDuTruXuatDto>.Fail("Chỉ được xác nhận xuất trừ tồn khi phiếu ở trạng thái DaKhaiBaoThucDung (và đã duyệt vượt nếu có).");

        var now = LocalNow;
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var warnings = new List<string>();
            foreach (var ct in phieu.ChiTiets)
            {
                var pt = await db.PHU_TUNG.FindAsync(ct.PhuTungId);
                if (pt == null) return ApiResponse<PhieuDuTruXuatDto>.Fail($"Không tìm thấy phụ tùng '{ct.PhuTungId}'.");

                decimal thucDung = ct.SoLuongThucDung ?? 0;
                if (pt.SoLuongTonHienCo < thucDung)
                    return ApiResponse<PhieuDuTruXuatDto>.Fail($"Không đủ tồn kho thực tế cho '{pt.TenPhuTung}' (Hiện có: {pt.SoLuongTonHienCo}, Cần xuất: {thucDung}).");

                pt.SoLuongTonHienCo -= thucDung;
                pt.SoLuongDangDuTru = Math.Max(0, pt.SoLuongDangDuTru - ct.SoLuongDuTru);

                if (pt.SoLuongTonHienCo <= pt.MucTonToiThieu)
                    warnings.Add($"Cảnh báo tồn kho tối thiểu: {pt.TenPhuTung} (Còn: {pt.SoLuongTonHienCo} / Tối thiểu: {pt.MucTonToiThieu})");
            }

            phieu.NguoiXacNhanXuatId = nguoiXacNhanId;
            phieu.ThoiDiemXacNhan = now;
            phieu.TrangThaiPhieu = CmmsConstants.TrangThaiPhieuXuat.DaXacNhanTruTon;

            await db.SaveChangesAsync();
            await audit.WriteAsync(
                actorTaiKhoanId > 0 ? actorTaiKhoanId : null,
                CmmsConstants.LoaiThaoTacLog.XacNhanXuatDung,
                phieuXuatId,
                $"Xác nhận xuất dùng phụ tùng cho lệnh {phieu.LenhBaoTriId} ({phieu.ChiTiets.Count} hạng mục).");
            await tx.CommitAsync();

            var dto = await LoadPhieuXuatDtoAsync(phieuXuatId);
            return ApiResponse<PhieuDuTruXuatDto>.Ok(dto!, "Đã xác nhận xuất dùng, trừ tồn kho vật lý và khóa sổ phiếu xuất.", warnings);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ApiResponse<PhieuDuTruXuatDto>.Fail($"Lỗi xác nhận xuất dùng: {ex.Message}");
        }
    }

    private async Task<PhieuDuTruXuatDto?> LoadPhieuXuatDtoAsync(string phieuXuatId)
    {
        var entity = await db.PHIEU_DU_TRU_XUAT_DUNG.AsNoTracking()
            .Include(p => p.NguoiLapDuTru)
            .Include(p => p.ChiTiets).ThenInclude(c => c.PhuTung)
            .FirstOrDefaultAsync(p => p.PhieuXuatId == phieuXuatId);
        return entity == null ? null : MapPhieuXuatDto(entity);
    }

    private static bool IsValidLoaiPhuTung(string loai) => loai is
        CmmsConstants.LoaiPhuTung.CoKhi or CmmsConstants.LoaiPhuTung.Dien
        or CmmsConstants.LoaiPhuTung.TieuHao or CmmsConstants.LoaiPhuTung.DungCu;

    internal static PhuTungDto MapPhuTungDto(PHU_TUNG p) => new()
    {
        PhuTungId = p.PhuTungId,
        TenPhuTung = p.TenPhuTung,
        LoaiPhuTung = p.LoaiPhuTung,
        QuyCach = p.QuyCach,
        DonViTinh = p.DonViTinh,
        SoLuongTonHienCo = p.SoLuongTonHienCo,
        SoLuongDangDuTru = p.SoLuongDangDuTru,
        MucTonToiThieu = p.MucTonToiThieu,
        MoTa = p.MoTa
    };

    internal static PhieuCapNhatTonKhoDto MapCapNhatKhoDto(PHIEU_CAP_NHAT_TON_KHO p) => new()
    {
        PhieuCapNhatId = p.PhieuCapNhatId,
        LoaiCapNhat = p.LoaiCapNhat,
        MaPhieuKhoNgoai = p.MaPhieuKhoNgoai,
        ThoiDiemLap = p.ThoiDiemLap,
        NguoiLapId = p.NguoiLapId,
        TenNguoiLap = p.NguoiLap.HoTen,
        NguoiXacNhanId = p.NguoiXacNhanId,
        TenNguoiXacNhan = p.NguoiXacNhan?.HoTen,
        ThoiDiemXacNhan = p.ThoiDiemXacNhan,
        TrangThaiPhieu = p.TrangThaiPhieu,
        GhiChu = p.GhiChu,
        ChiTiet = p.ChiTiets.Select(c => new ChiTietCapNhatTonKhoDto
        {
            ChiTietCapNhatId = c.ChiTietCapNhatId,
            PhuTungId = c.PhuTungId,
            TenPhuTung = c.PhuTung.TenPhuTung,
            DonViTinh = c.PhuTung.DonViTinh,
            SoLuongThayDoi = c.SoLuongThayDoi,
            GhiChu = c.GhiChu
        }).ToList()
    };

    internal static PhieuDuTruXuatDto MapPhieuXuatDto(PHIEU_DU_TRU_XUAT_DUNG p) => new()
    {
        PhieuXuatId = p.PhieuXuatId,
        LenhBaoTriId = p.LenhBaoTriId,
        NguoiLapDuTruId = p.NguoiLapDuTruId,
        TenNguoiLap = p.NguoiLapDuTru.HoTen,
        ThoiDiemLap = p.ThoiDiemLap,
        NguoiKhaiBaoId = p.NguoiKhaiBaoId,
        ThoiDiemKhaiBao = p.ThoiDiemKhaiBao,
        NguoiDuyetVuotId = p.NguoiDuyetVuotId,
        ThoiDiemDuyetVuot = p.ThoiDiemDuyetVuot,
        NguoiXacNhanXuatId = p.NguoiXacNhanXuatId,
        ThoiDiemXacNhan = p.ThoiDiemXacNhan,
        TrangThaiPhieu = p.TrangThaiPhieu,
        GhiChu = p.GhiChu,
        ChiTiet = p.ChiTiets.Select(c => new ChiTietPhieuXuatDto
        {
            ChiTietId = c.ChiTietId,
            PhuTungId = c.PhuTungId,
            TenPhuTung = c.PhuTung.TenPhuTung,
            DonViTinh = c.PhuTung.DonViTinh,
            SoLuongDuTru = c.SoLuongDuTru,
            SoLuongThucDung = c.SoLuongThucDung,
            TonKhoHienCo = c.PhuTung.SoLuongTonHienCo,
            TonKhoKhaDung = c.PhuTung.SoLuongTonHienCo - c.PhuTung.SoLuongDangDuTru,
            GhiChuGiaiTrinh = c.GhiChuGiaiTrinh
        }).ToList()
    };
}
