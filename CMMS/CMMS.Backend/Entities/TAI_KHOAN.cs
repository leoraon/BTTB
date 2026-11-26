namespace CMMS.Backend.Entities;

public class TAI_KHOAN
{
    public int TaiKhoanId { get; set; }
    public string NhanVienId { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public int VaiTroId { get; set; }
    public string TrangThaiTaiKhoan { get; set; } = "ChoDuyet";
    public int SoLanDangNhapSai { get; set; }
    public DateTime? ThoiDiemMoKhoaTam { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiDuyetId { get; set; }

    public NHAN_VIEN NhanVien { get; set; } = null!;
    public VAI_TRO VaiTro { get; set; } = null!;
    public NHAN_VIEN? NguoiDuyet { get; set; }
    public ICollection<NHAT_KY_HE_THONG> NhatKys { get; set; } = [];
}
