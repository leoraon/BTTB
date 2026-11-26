namespace CMMS.Backend.Entities;

public class NHAN_VIEN
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

    public PHAN_XUONG_BO_PHAN DonVi { get; set; } = null!;
    public TAI_KHOAN? TaiKhoan { get; set; }
    public ICollection<CHUYEN_MON_KTV> ChuyenMonKtvs { get; set; } = [];
}
