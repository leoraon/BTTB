using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class NhanSuService(
    CmmsDbContext db,
    AuditLogHelper audit,
    TimeProvider? timeProvider = null) : INhanSuService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;
    public async Task<ApiResponse<List<NhanVienDto>>> GetNhanViensAsync(string? keyword, string? donViId, string? trangThai)
    {
        var q = db.NHAN_VIEN.AsNoTracking()
            .Include(n => n.DonVi)
            .Include(n => n.TaiKhoan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(n => n.NhanVienId.Contains(kw) || n.HoTen.Contains(kw) || n.Email.Contains(kw));
        }
        if (!string.IsNullOrWhiteSpace(donViId)) q = q.Where(n => n.DonViId == donViId);
        if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(n => n.TrangThai == trangThai);

        var list = await q.OrderBy(n => n.NhanVienId)
            .Select(n => new NhanVienDto
            {
                NhanVienId = n.NhanVienId,
                HoTen = n.HoTen,
                SoDienThoai = n.SoDienThoai,
                Email = n.Email,
                DonViId = n.DonViId,
                TenDonVi = n.DonVi.TenDonVi,
                ChucVu = n.ChucVu,
                ChuyenNganh = n.ChuyenNganh,
                BacTho = n.BacTho,
                CaLamViec = n.CaLamViec,
                TrangThaiSanSang = n.TrangThaiSanSang,
                TrangThai = n.TrangThai,
                CoTaiKhoan = n.TaiKhoan != null
            })
            .ToListAsync();

        return ApiResponse<List<NhanVienDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<NhanVienDto>>> GetKyThuatViensAsync()
    {
        var list = await db.NHAN_VIEN.AsNoTracking()
            .Include(n => n.DonVi)
            .Include(n => n.TaiKhoan)
            .Where(n => n.TrangThai == CmmsConstants.TrangThaiNhanVien.DangLamViec
                     && (n.TaiKhoan != null && n.TaiKhoan.VaiTroId == CmmsConstants.VaiTroIds.KyThuatVien
                         || n.ChuyenNganh != null && n.TrangThaiSanSang != null))
            .OrderBy(n => n.NhanVienId)
            .Select(n => new NhanVienDto
            {
                NhanVienId = n.NhanVienId,
                HoTen = n.HoTen,
                SoDienThoai = n.SoDienThoai,
                Email = n.Email,
                DonViId = n.DonViId,
                TenDonVi = n.DonVi.TenDonVi,
                ChucVu = n.ChucVu,
                ChuyenNganh = n.ChuyenNganh,
                BacTho = n.BacTho,
                CaLamViec = n.CaLamViec,
                TrangThaiSanSang = n.TrangThaiSanSang,
                TrangThai = n.TrangThai,
                CoTaiKhoan = n.TaiKhoan != null
            })
            .ToListAsync();

        return ApiResponse<List<NhanVienDto>>.Ok(list);
    }

    public async Task<ApiResponse<NhanVienDto>> CreateNhanVienAsync(NhanVienUpsertReq req)
    {
        var validation = await ValidateNhanVienAsync(req, isCreate: true);
        if (validation != null) return ApiResponse<NhanVienDto>.Fail(validation);

        var nv = new NHAN_VIEN
        {
            NhanVienId = req.NhanVienId.Trim(),
            HoTen = req.HoTen.Trim(),
            SoDienThoai = req.SoDienThoai.Trim(),
            Email = req.Email.Trim(),
            DonViId = req.DonViId.Trim(),
            ChucVu = req.ChucVu.Trim(),
            ChuyenNganh = req.ChuyenNganh?.Trim(),
            BacTho = req.BacTho?.Trim(),
            CaLamViec = req.CaLamViec,
            TrangThaiSanSang = req.TrangThaiSanSang,
            TrangThai = string.IsNullOrWhiteSpace(req.TrangThai) ? CmmsConstants.TrangThaiNhanVien.DangLamViec : req.TrangThai
        };
        db.NHAN_VIEN.Add(nv);
        await db.SaveChangesAsync();

        var dv = await db.PHAN_XUONG_BO_PHAN.FindAsync(nv.DonViId);
        return ApiResponse<NhanVienDto>.Ok(MapNhanVien(nv, dv!.TenDonVi, false), "Thêm hồ sơ nhân viên thành công.");
    }

    public async Task<ApiResponse<NhanVienDto>> UpdateNhanVienAsync(string nhanVienId, NhanVienUpsertReq req, int actorTaiKhoanId)
    {
        var nv = await db.NHAN_VIEN.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.NhanVienId == nhanVienId);
        if (nv == null) return ApiResponse<NhanVienDto>.Fail("Không tìm thấy nhân viên.");

        req.NhanVienId = nhanVienId;
        var validation = await ValidateNhanVienAsync(req, isCreate: false);
        if (validation != null) return ApiResponse<NhanVienDto>.Fail(validation);

        bool dangChuyenNghiViec = nv.TrangThai != CmmsConstants.TrangThaiNhanVien.DaNghiViec
            && req.TrangThai == CmmsConstants.TrangThaiNhanVien.DaNghiViec;

        if (dangChuyenNghiViec && nv.TaiKhoan != null && nv.TaiKhoan.TaiKhoanId == actorTaiKhoanId)
            return ApiResponse<NhanVienDto>.Fail("Không thể chuyển chính tài khoản đang đăng nhập sang DaNghiViec.");

        if (dangChuyenNghiViec && await HasActiveWorkOrderExecutionAsync(nhanVienId))
            return ApiResponse<NhanVienDto>.Fail("Không thể chuyển sang DaNghiViec khi nhân viên đang trực tiếp thực hiện lệnh bảo trì.");

        if (req.TrangThaiSanSang == CmmsConstants.TrangThaiSanSang.NghiPhep
            && nv.TrangThaiSanSang != CmmsConstants.TrangThaiSanSang.NghiPhep
            && await HasActiveWorkOrderExecutionAsync(nhanVienId))
            return ApiResponse<NhanVienDto>.Fail("Không thể chuyển sang NghiPhep khi kỹ thuật viên đang trực tiếp thực hiện lệnh bảo trì.");

        nv.HoTen = req.HoTen.Trim();
        nv.SoDienThoai = req.SoDienThoai.Trim();
        nv.Email = req.Email.Trim();
        nv.DonViId = req.DonViId.Trim();
        nv.ChucVu = req.ChucVu.Trim();
        nv.ChuyenNganh = req.ChuyenNganh?.Trim();
        nv.BacTho = req.BacTho?.Trim();
        nv.CaLamViec = req.CaLamViec;
        nv.TrangThaiSanSang = req.TrangThaiSanSang;
        nv.TrangThai = string.IsNullOrWhiteSpace(req.TrangThai) ? nv.TrangThai : req.TrangThai;

        if (nv.TrangThai == CmmsConstants.TrangThaiNhanVien.DaNghiViec && nv.TaiKhoan != null)
            nv.TaiKhoan.TrangThaiTaiKhoan = CmmsConstants.TrangThaiTaiKhoan.DangKhoa;

        await db.SaveChangesAsync();

        if (dangChuyenNghiViec)
        {
            await audit.WriteAsync(actorTaiKhoanId, CmmsConstants.LoaiThaoTacLog.KhoaTaiKhoan, nhanVienId,
                $"Cập nhật nhân viên {nv.HoTen} sang trạng thái DaNghiViec và khóa tài khoản.");
        }

        var dv = await db.PHAN_XUONG_BO_PHAN.FindAsync(nv.DonViId);
        return ApiResponse<NhanVienDto>.Ok(MapNhanVien(nv, dv!.TenDonVi, nv.TaiKhoan != null), "Cập nhật nhân viên thành công.");
    }

    public async Task<ApiResponse<bool>> NgungCongTacNhanVienAsync(string nhanVienId, int actorTaiKhoanId)
    {
        var nv = await db.NHAN_VIEN.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.NhanVienId == nhanVienId);
        if (nv == null) return ApiResponse<bool>.Fail("Không tìm thấy nhân viên.");

        // 1.1.1: Không xóa hồ sơ nhân viên, chuyển trạng thái công tác & khóa tài khoản nếu có
        if (nv.TaiKhoan != null && nv.TaiKhoan.TaiKhoanId == actorTaiKhoanId)
            return ApiResponse<bool>.Fail("Không thể ngừng công tác chính tài khoản đang đăng nhập.");
        if (await HasActiveWorkOrderExecutionAsync(nhanVienId))
            return ApiResponse<bool>.Fail("Không thể ngừng công tác khi nhân viên đang trực tiếp thực hiện lệnh bảo trì.");

        nv.TrangThai = CmmsConstants.TrangThaiNhanVien.DaNghiViec;
        if (nv.TaiKhoan != null)
            nv.TaiKhoan.TrangThaiTaiKhoan = CmmsConstants.TrangThaiTaiKhoan.DangKhoa;
        await db.SaveChangesAsync();
        await audit.WriteAsync(actorTaiKhoanId, CmmsConstants.LoaiThaoTacLog.KhoaTaiKhoan, nhanVienId,
            $"Chuyển nhân viên {nv.HoTen} sang trạng thái DaNghiViec (bảo toàn lịch sử).");
        return ApiResponse<bool>.Ok(true, "Đã chuyển trạng thái nhân viên sang ngừng công tác.");
    }

    public async Task<ApiResponse<List<TaiKhoanDto>>> GetTaiKhoansAsync(string? trangThai)
    {
        var q = db.TAI_KHOAN.AsNoTracking()
            .Include(t => t.NhanVien)
            .Include(t => t.VaiTro)
            .Include(t => t.NguoiDuyet)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
            q = q.Where(t => t.TrangThaiTaiKhoan == trangThai);

        var list = await q.OrderBy(t => t.TaiKhoanId)
            .Select(t => new TaiKhoanDto
            {
                TaiKhoanId = t.TaiKhoanId,
                NhanVienId = t.NhanVienId,
                HoTenNhanVien = t.NhanVien.HoTen,
                TenDangNhap = t.TenDangNhap,
                VaiTroId = t.VaiTroId,
                TenVaiTro = t.VaiTro.TenVaiTro,
                TrangThaiTaiKhoan = t.TrangThaiTaiKhoan,
                SoLanDangNhapSai = t.SoLanDangNhapSai,
                ThoiDiemMoKhoaTam = t.ThoiDiemMoKhoaTam,
                NgayTao = t.NgayTao,
                NguoiDuyetId = t.NguoiDuyetId,
                TenNguoiDuyet = t.NguoiDuyet != null ? t.NguoiDuyet.HoTen : null
            })
            .ToListAsync();

        return ApiResponse<List<TaiKhoanDto>>.Ok(list);
    }

    public async Task<ApiResponse<TaiKhoanDto>> DeXuatTaiKhoanAsync(DeXuatTaiKhoanReq req, int actorTaiKhoanId)
    {
        if (string.IsNullOrWhiteSpace(req.TenDangNhap) || string.IsNullOrWhiteSpace(req.MatKhauKhoiTao) || req.MatKhauKhoiTao.Length < 6)
            return ApiResponse<TaiKhoanDto>.Fail("Tên đăng nhập không được trống và mật khẩu tối thiểu 6 ký tự.");

        var nv = await db.NHAN_VIEN.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.NhanVienId == req.NhanVienId);
        if (nv == null) return ApiResponse<TaiKhoanDto>.Fail("Không tìm thấy nhân viên.");
        if (nv.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec)
            return ApiResponse<TaiKhoanDto>.Fail("Không thể cấp tài khoản cho nhân viên đã nghỉ việc.");

        var uname = req.TenDangNhap.Trim();
        var vaiTro = await db.VAI_TRO.FindAsync(req.VaiTroId);
        if (vaiTro == null) return ApiResponse<TaiKhoanDto>.Fail("Vai trò không hợp lệ.");

        TAI_KHOAN tk;
        if (nv.TaiKhoan != null)
        {
            if (nv.TaiKhoan.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.TuChoi)
                return ApiResponse<TaiKhoanDto>.Fail("Nhân viên này đã có tài khoản trên hệ thống.");

            if (await db.TAI_KHOAN.AnyAsync(t => t.TenDangNhap == uname && t.TaiKhoanId != nv.TaiKhoan.TaiKhoanId))
                return ApiResponse<TaiKhoanDto>.Fail("Tên đăng nhập đã tồn tại.");

            tk = nv.TaiKhoan;
            tk.TenDangNhap = uname;
            tk.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(req.MatKhauKhoiTao);
            tk.VaiTroId = vaiTro.VaiTroId;
            tk.TrangThaiTaiKhoan = CmmsConstants.TrangThaiTaiKhoan.ChoDuyet;
            tk.NguoiDuyetId = null;
            tk.SoLanDangNhapSai = 0;
            tk.ThoiDiemMoKhoaTam = null;
            tk.NgayTao = LocalNow;
        }
        else
        {
            if (await db.TAI_KHOAN.AnyAsync(t => t.TenDangNhap == uname))
                return ApiResponse<TaiKhoanDto>.Fail("Tên đăng nhập đã tồn tại.");

            tk = new TAI_KHOAN
            {
                NhanVienId = nv.NhanVienId,
                TenDangNhap = uname,
                MatKhauHash = BCrypt.Net.BCrypt.HashPassword(req.MatKhauKhoiTao),
                VaiTroId = vaiTro.VaiTroId,
                TrangThaiTaiKhoan = CmmsConstants.TrangThaiTaiKhoan.ChoDuyet,
                NgayTao = LocalNow
            };
            db.TAI_KHOAN.Add(tk);
        }

        await db.SaveChangesAsync();

        await audit.WriteAsync(actorTaiKhoanId, CmmsConstants.LoaiThaoTacLog.DeXuatTaiKhoan, uname,
            $"Đề xuất cấp tài khoản mới cho nhân viên {nv.HoTen} (vai trò {vaiTro.TenVaiTro}).");

        return ApiResponse<TaiKhoanDto>.Ok(new TaiKhoanDto
        {
            TaiKhoanId = tk.TaiKhoanId,
            NhanVienId = tk.NhanVienId,
            HoTenNhanVien = nv.HoTen,
            TenDangNhap = tk.TenDangNhap,
            VaiTroId = tk.VaiTroId,
            TenVaiTro = vaiTro.TenVaiTro,
            TrangThaiTaiKhoan = tk.TrangThaiTaiKhoan,
            NgayTao = tk.NgayTao
        }, "Đã đề xuất cấp tài khoản, đang chờ Quản lý nhà máy phê duyệt.");
    }

    public async Task<ApiResponse<bool>> PheDuyetTaiKhoanAsync(
        int taiKhoanId, PheDuyetTaiKhoanReq req, string nguoiDuyetId, int actorTaiKhoanId)
    {
        var tk = await db.TAI_KHOAN.Include(t => t.NhanVien).FirstOrDefaultAsync(t => t.TaiKhoanId == taiKhoanId);
        if (tk == null) return ApiResponse<bool>.Fail("Không tìm thấy tài khoản.");
        if (tk.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.ChoDuyet)
            return ApiResponse<bool>.Fail("Chỉ được phê duyệt các tài khoản đang ở trạng thái ChoDuyet.");
        if (tk.NhanVienId == nguoiDuyetId)
            return ApiResponse<bool>.Fail("Không thể tự phê duyệt tài khoản của chính mình.");
        if (req.PheDuyet && tk.NhanVien.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec)
            return ApiResponse<bool>.Fail("Không thể kích hoạt tài khoản cho nhân viên đã ngừng công tác.");
        if (!req.PheDuyet && string.IsNullOrWhiteSpace(req.GhiChu))
            return ApiResponse<bool>.Fail("Bắt buộc nhập lý do khi từ chối đề xuất cấp tài khoản.");

        tk.NguoiDuyetId = nguoiDuyetId;
        tk.TrangThaiTaiKhoan = req.PheDuyet
            ? CmmsConstants.TrangThaiTaiKhoan.DangHoatDong
            : CmmsConstants.TrangThaiTaiKhoan.TuChoi;

        await db.SaveChangesAsync();
        await audit.WriteAsync(actorTaiKhoanId,
            req.PheDuyet ? CmmsConstants.LoaiThaoTacLog.PheDuyetTaiKhoan : CmmsConstants.LoaiThaoTacLog.TuChoiTaiKhoan,
            tk.TenDangNhap,
            req.PheDuyet ? $"Phê duyệt kích hoạt tài khoản {tk.TenDangNhap}." : $"Từ chối cấp tài khoản {tk.TenDangNhap}: {req.GhiChu}");

        return ApiResponse<bool>.Ok(true, req.PheDuyet ? "Đã phê duyệt kích hoạt tài khoản." : "Đã từ chối đề xuất tài khoản.");
    }

    public async Task<ApiResponse<bool>> KhoaMoTaiKhoanAsync(int taiKhoanId, KhoaMoTaiKhoanReq req, int actorTaiKhoanId)
    {
        var tk = await db.TAI_KHOAN.Include(t => t.NhanVien).FirstOrDefaultAsync(t => t.TaiKhoanId == taiKhoanId);
        if (tk == null) return ApiResponse<bool>.Fail("Không tìm thấy tài khoản.");

        if (req.Khoa)
        {
            if (tk.TaiKhoanId == actorTaiKhoanId)
                return ApiResponse<bool>.Fail("Không thể tự khóa tài khoản đang đăng nhập.");
            if (tk.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.DangHoatDong)
                return ApiResponse<bool>.Fail("Chỉ có thể khóa tài khoản đang ở trạng thái DangHoatDong.");
            tk.TrangThaiTaiKhoan = CmmsConstants.TrangThaiTaiKhoan.DangKhoa;
        }
        else
        {
            if (tk.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.DangKhoa)
                return ApiResponse<bool>.Fail("Chỉ có thể mở khóa tài khoản đang bị khóa.");
            if (tk.NhanVien.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec)
                return ApiResponse<bool>.Fail("Không thể mở khóa tài khoản của nhân viên đã ngừng công tác.");
            if (string.IsNullOrWhiteSpace(tk.NguoiDuyetId))
                return ApiResponse<bool>.Fail("Tài khoản chưa được Quản lý nhà máy phê duyệt kích hoạt, không thể mở khóa.");

            tk.TrangThaiTaiKhoan = CmmsConstants.TrangThaiTaiKhoan.DangHoatDong;
            tk.SoLanDangNhapSai = 0;
            tk.ThoiDiemMoKhoaTam = null;
        }

        await db.SaveChangesAsync();
        await audit.WriteAsync(actorTaiKhoanId,
            req.Khoa ? CmmsConstants.LoaiThaoTacLog.KhoaTaiKhoan : CmmsConstants.LoaiThaoTacLog.MoKhoaTaiKhoan,
            tk.TenDangNhap,
            req.Khoa ? $"Khóa tài khoản {tk.TenDangNhap}. Lý do: {req.LyDo}" : $"Mở khóa tài khoản {tk.TenDangNhap}.");

        return ApiResponse<bool>.Ok(true, req.Khoa ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.");
    }

    private async Task<string?> ValidateNhanVienAsync(NhanVienUpsertReq req, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(req.NhanVienId) || string.IsNullOrWhiteSpace(req.HoTen)
            || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.SoDienThoai)
            || string.IsNullOrWhiteSpace(req.DonViId) || string.IsNullOrWhiteSpace(req.ChucVu))
            return "Vui lòng nhập đầy đủ mã nhân viên, họ tên, SĐT, email, đơn vị và chức vụ.";

        if (isCreate && await db.NHAN_VIEN.AnyAsync(n => n.NhanVienId == req.NhanVienId.Trim()))
            return $"Mã nhân viên '{req.NhanVienId}' đã tồn tại.";

        if (await db.NHAN_VIEN.AnyAsync(n => n.Email == req.Email.Trim() && n.NhanVienId != req.NhanVienId.Trim()))
            return $"Email '{req.Email}' đã được sử dụng.";

        if (!await db.PHAN_XUONG_BO_PHAN.AnyAsync(d => d.DonViId == req.DonViId.Trim()))
            return "Đơn vị trực thuộc không tồn tại.";

        if (!string.IsNullOrEmpty(req.CaLamViec) && req.CaLamViec is not (
            CmmsConstants.CaLamViec.Ca1 or CmmsConstants.CaLamViec.Ca2 or CmmsConstants.CaLamViec.Ca3 or CmmsConstants.CaLamViec.HanhChinh))
            return "Ca làm việc không hợp lệ.";

        if (!string.IsNullOrEmpty(req.TrangThaiSanSang) && req.TrangThaiSanSang is not (
            CmmsConstants.TrangThaiSanSang.SanSang or CmmsConstants.TrangThaiSanSang.DangBan or CmmsConstants.TrangThaiSanSang.NghiPhep))
            return "Trạng thái sẵn sàng không hợp lệ.";

        if (!string.IsNullOrWhiteSpace(req.TrangThai) && req.TrangThai is not (
            CmmsConstants.TrangThaiNhanVien.DangLamViec or CmmsConstants.TrangThaiNhanVien.DaNghiViec))
            return "Trạng thái nhân viên không hợp lệ.";

        return null;
    }

    private Task<bool> HasActiveWorkOrderExecutionAsync(string nhanVienId)
        => db.PHAN_CONG_KTV.AnyAsync(p =>
            p.KyThuatVienId == nhanVienId
            && p.TrangThaiPhanCong == CmmsConstants.TrangThaiPhanCong.DangLam
            && p.LenhBaoTri.TrangThaiLenh == CmmsConstants.TrangThaiLenh.DangThucHien);

    private static NhanVienDto MapNhanVien(NHAN_VIEN n, string tenDonVi, bool coTaiKhoan) => new()
    {
        NhanVienId = n.NhanVienId,
        HoTen = n.HoTen,
        SoDienThoai = n.SoDienThoai,
        Email = n.Email,
        DonViId = n.DonViId,
        TenDonVi = tenDonVi,
        ChucVu = n.ChucVu,
        ChuyenNganh = n.ChuyenNganh,
        BacTho = n.BacTho,
        CaLamViec = n.CaLamViec,
        TrangThaiSanSang = n.TrangThaiSanSang,
        TrangThai = n.TrangThai,
        CoTaiKhoan = coTaiKhoan
    };
}
