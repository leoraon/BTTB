namespace CMMS.Shared.DTOs;

// 1.1 Nhân viên & Tài khoản
public class NhanVienDto
{
    public string NhanVienId { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DonViId { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string? ChuyenNganh { get; set; }
    public string? BacTho { get; set; }
    public string? CaLamViec { get; set; }
    public string? TrangThaiSanSang { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public bool CoTaiKhoan { get; set; }
}

public class NhanVienUpsertReq
{
    public string NhanVienId { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DonViId { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string? ChuyenNganh { get; set; }
    public string? BacTho { get; set; }
    public string? CaLamViec { get; set; }
    public string? TrangThaiSanSang { get; set; }
    public string TrangThai { get; set; } = "DangLamViec";
}

public class TaiKhoanDto
{
    public int TaiKhoanId { get; set; }
    public string NhanVienId { get; set; } = string.Empty;
    public string HoTenNhanVien { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public int VaiTroId { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string TrangThaiTaiKhoan { get; set; } = string.Empty;
    public int SoLanDangNhapSai { get; set; }
    public DateTime? ThoiDiemMoKhoaTam { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiDuyetId { get; set; }
    public string? TenNguoiDuyet { get; set; }
}

public class DeXuatTaiKhoanReq
{
    public string NhanVienId { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauKhoiTao { get; set; } = string.Empty;
    public int VaiTroId { get; set; }
}

public class PheDuyetTaiKhoanReq
{
    public bool PheDuyet { get; set; }
    public string? GhiChu { get; set; }
}

public class KhoaMoTaiKhoanReq
{
    public bool Khoa { get; set; }
    public string? LyDo { get; set; }
}

// 1.2 Vai trò & Phân quyền
public class VaiTroDto
{
    public int VaiTroId { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public List<string> DanhSachQuyen { get; set; } = [];
}

public class VaiTroUpsertReq
{
    public int VaiTroId { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}

public class CapNhatQuyenVaiTroReq
{
    public List<string> DanhSachQuyen { get; set; } = [];
}

public class GanVaiTroReq
{
    public int VaiTroId { get; set; }
}

// 1.3 Nhật ký hệ thống
public class NhatKyDto
{
    public long NhatKyId { get; set; }
    public int? TaiKhoanId { get; set; }
    public string? TenDangNhap { get; set; }
    public DateTime ThoiDiem { get; set; }
    public string LoaiThaoTac { get; set; } = string.Empty;
    public string? DoiTuongTacDong { get; set; }
    public string MoTaChiTiet { get; set; } = string.Empty;
}

public class NhatKyFilterReq
{
    public int? TaiKhoanId { get; set; }
    public string? LoaiThaoTac { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string? Keyword { get; set; }
}

// 1.4 Thư viện biểu mẫu
public class BieuMauDto
{
    public string BieuMauId { get; set; } = string.Empty;
    public string TenBieuMau { get; set; } = string.Empty;
    public string LoaiBieuMau { get; set; } = string.Empty;
    public string DinhDangTep { get; set; } = string.Empty;
    public string DuongDanTep { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}
