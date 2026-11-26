namespace CMMS.Shared.DTOs;

public class LoginRequest
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int TaiKhoanId { get; set; }
    public string NhanVienId { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int VaiTroId { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}

public class ChangePasswordRequest
{
    public string MatKhauCu { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string HoTen { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public int TaiKhoanId { get; set; }
    public string NhanVienId { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonViId { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public int VaiTroId { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}
