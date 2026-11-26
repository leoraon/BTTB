using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Security;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Services.Implementations;

public class AuthService(
    CmmsDbContext db,
    JwtTokenHelper jwt,
    AuditLogHelper audit,
    IConfiguration config,
    TimeProvider? timeProvider = null) : IAuthService
{
    private const string SeedPlaceholderHash = "$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9";
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private string DefaultSeedPassword => config["SeedDefaultPassword"] ?? "123456";
    private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenDangNhap) || string.IsNullOrWhiteSpace(req.MatKhau))
            return ApiResponse<LoginResponse>.Fail("Vui lòng nhập tên đăng nhập và mật khẩu.");

        var tk = await db.TAI_KHOAN
            .Include(t => t.NhanVien)
            .Include(t => t.VaiTro).ThenInclude(v => v.VAI_TRO_QUYEN)
            .FirstOrDefaultAsync(t => t.TenDangNhap == req.TenDangNhap.Trim());

        if (tk == null)
        {
            await audit.WriteAsync(null, CmmsConstants.LoaiThaoTacLog.DangNhapThatBai, req.TenDangNhap,
                "Đăng nhập thất bại: tài khoản không tồn tại.");
            return ApiResponse<LoginResponse>.Fail("Sai tên đăng nhập hoặc mật khẩu.");
        }

        if (tk.NhanVien.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec)
            return ApiResponse<LoginResponse>.Fail("Nhân viên đã ngừng công tác, tài khoản không thể đăng nhập.");

        if (tk.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.DangHoatDong)
        {
            var msg = tk.TrangThaiTaiKhoan switch
            {
                CmmsConstants.TrangThaiTaiKhoan.ChoDuyet => "Tài khoản đang chờ Quản lý nhà máy phê duyệt kích hoạt.",
                CmmsConstants.TrangThaiTaiKhoan.TuChoi => "Đề xuất cấp tài khoản đã bị từ chối.",
                CmmsConstants.TrangThaiTaiKhoan.DangKhoa => "Tài khoản hiện đang bị khóa. Vui lòng liên hệ Quản trị hệ thống.",
                _ => $"Tài khoản không khả dụng ({tk.TrangThaiTaiKhoan})."
            };
            return ApiResponse<LoginResponse>.Fail(msg);
        }

        var now = LocalNow;
        if (tk.ThoiDiemMoKhoaTam.HasValue)
        {
            if (tk.ThoiDiemMoKhoaTam.Value > now)
            {
                return ApiResponse<LoginResponse>.Fail(
                    $"Tài khoản đang bị khóa tạm thời đến {tk.ThoiDiemMoKhoaTam.Value:HH:mm dd/MM/yyyy} do nhập sai mật khẩu nhiều lần.");
            }
            tk.ThoiDiemMoKhoaTam = null;
        }

        if (!VerifyAndUpgradeIfPlaceholder(tk, req.MatKhau))
        {
            tk.SoLanDangNhapSai++;
            string logMsg = $"Sai mật khẩu (lần {tk.SoLanDangNhapSai}/5).";
            if (tk.SoLanDangNhapSai >= 5)
            {
                tk.ThoiDiemMoKhoaTam = now.AddMinutes(15);
                tk.SoLanDangNhapSai = 0;
                logMsg = "Sai mật khẩu 5 lần liên tiếp -> Tự động khóa tạm 15 phút.";
                await db.SaveChangesAsync();
                await audit.WriteAsync(tk.TaiKhoanId, CmmsConstants.LoaiThaoTacLog.DangNhapThatBai, tk.TenDangNhap, logMsg);
                await audit.WriteAsync(tk.TaiKhoanId, CmmsConstants.LoaiThaoTacLog.KhoaTaiKhoan, tk.TenDangNhap,
                    $"Khóa tạm đến {tk.ThoiDiemMoKhoaTam:HH:mm dd/MM/yyyy}.");
                return ApiResponse<LoginResponse>.Fail("Bạn đã nhập sai mật khẩu 5 lần. Tài khoản bị khóa tạm 15 phút.");
            }

            await db.SaveChangesAsync();
            await audit.WriteAsync(tk.TaiKhoanId, CmmsConstants.LoaiThaoTacLog.DangNhapThatBai, tk.TenDangNhap, logMsg);
            return ApiResponse<LoginResponse>.Fail("Sai tên đăng nhập hoặc mật khẩu.");
        }

        tk.SoLanDangNhapSai = 0;
        tk.ThoiDiemMoKhoaTam = null;
        await db.SaveChangesAsync();

        var permissions = tk.VaiTro.VAI_TRO_QUYEN.Select(q => q.MaQuyen).ToList();
        var (token, expiresAt) = jwt.GenerateToken(tk, permissions);

        await audit.WriteAsync(tk.TaiKhoanId, CmmsConstants.LoaiThaoTacLog.DangNhap, tk.TenDangNhap,
            $"Đăng nhập thành công với vai trò {tk.VaiTro.TenVaiTro}.");

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            TaiKhoanId = tk.TaiKhoanId,
            NhanVienId = tk.NhanVienId,
            TenDangNhap = tk.TenDangNhap,
            HoTen = tk.NhanVien.HoTen,
            VaiTroId = tk.VaiTroId,
            TenVaiTro = tk.VaiTro.TenVaiTro,
            Permissions = permissions
        }, "Đăng nhập thành công.");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(int taiKhoanId, string tenDangNhap)
    {
        await audit.WriteAsync(taiKhoanId > 0 ? taiKhoanId : null,
            CmmsConstants.LoaiThaoTacLog.DangXuat, tenDangNhap, "Đăng xuất khỏi hệ thống.");
        return ApiResponse<bool>.Ok(true, "Đã đăng xuất.");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(int taiKhoanId, ChangePasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.MatKhauMoi) || req.MatKhauMoi.Length < 6)
            return ApiResponse<bool>.Fail("Mật khẩu mới phải có ít nhất 6 ký tự.");
        if (req.MatKhauMoi == req.MatKhauCu)
            return ApiResponse<bool>.Fail("Mật khẩu mới không được trùng với mật khẩu hiện tại.");

        var tk = await db.TAI_KHOAN.FindAsync(taiKhoanId);
        if (tk == null) return ApiResponse<bool>.Fail("Không tìm thấy tài khoản.");
        if (tk.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.DangHoatDong)
            return ApiResponse<bool>.Fail("Chỉ tài khoản đang hoạt động mới được đổi mật khẩu.");

        if (!VerifyAndUpgradeIfPlaceholder(tk, req.MatKhauCu))
            return ApiResponse<bool>.Fail("Mật khẩu hiện tại không chính xác.");

        tk.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(req.MatKhauMoi);
        await db.SaveChangesAsync();
        await audit.WriteAsync(tk.TaiKhoanId, CmmsConstants.LoaiThaoTacLog.DoiMatKhau, tk.TenDangNhap,
            "Người dùng tự đổi mật khẩu cá nhân.");
        return ApiResponse<bool>.Ok(true, "Đổi mật khẩu thành công.");
    }

    public async Task<ApiResponse<bool>> UpdateProfileAsync(string nhanVienId, UpdateProfileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.HoTen) || string.IsNullOrWhiteSpace(req.SoDienThoai) || string.IsNullOrWhiteSpace(req.Email))
            return ApiResponse<bool>.Fail("Vui lòng nhập đầy đủ họ tên, số điện thoại và email.");

        var nv = await db.NHAN_VIEN.FindAsync(nhanVienId);
        if (nv == null) return ApiResponse<bool>.Fail("Không tìm thấy hồ sơ nhân viên.");

        bool emailExists = await db.NHAN_VIEN.AnyAsync(n => n.Email == req.Email.Trim() && n.NhanVienId != nhanVienId);
        if (emailExists) return ApiResponse<bool>.Fail("Email đã được sử dụng bởi nhân viên khác.");

        nv.HoTen = req.HoTen.Trim();
        nv.SoDienThoai = req.SoDienThoai.Trim();
        nv.Email = req.Email.Trim();
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Cập nhật thông tin cá nhân thành công.");
    }

    public async Task<ApiResponse<CurrentUserDto>> GetCurrentUserAsync(int taiKhoanId)
    {
        var tk = await db.TAI_KHOAN.AsNoTracking()
            .Include(t => t.NhanVien).ThenInclude(n => n.DonVi)
            .Include(t => t.VaiTro).ThenInclude(v => v.VAI_TRO_QUYEN)
            .FirstOrDefaultAsync(t => t.TaiKhoanId == taiKhoanId);

        if (tk == null) return ApiResponse<CurrentUserDto>.Fail("Không tìm thấy tài khoản.");

        return ApiResponse<CurrentUserDto>.Ok(new CurrentUserDto
        {
            TaiKhoanId = tk.TaiKhoanId,
            NhanVienId = tk.NhanVienId,
            TenDangNhap = tk.TenDangNhap,
            HoTen = tk.NhanVien.HoTen,
            SoDienThoai = tk.NhanVien.SoDienThoai,
            Email = tk.NhanVien.Email,
            ChucVu = tk.NhanVien.ChucVu,
            DonViId = tk.NhanVien.DonViId,
            TenDonVi = tk.NhanVien.DonVi.TenDonVi,
            VaiTroId = tk.VaiTroId,
            TenVaiTro = tk.VaiTro.TenVaiTro,
            Permissions = tk.VaiTro.VAI_TRO_QUYEN.Select(q => q.MaQuyen).ToList()
        });
    }

    private bool VerifyAndUpgradeIfPlaceholder(TAI_KHOAN tk, string rawPassword)
    {
        if (tk.MatKhauHash == SeedPlaceholderHash && rawPassword == DefaultSeedPassword)
        {
            tk.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);
            return true;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(rawPassword, tk.MatKhauHash);
        }
        catch
        {
            return false;
        }
    }
}
