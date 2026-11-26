namespace CMMS.Backend.Entities;

public class PHU_TUNG
{
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string LoaiPhuTung { get; set; } = string.Empty;
    public string QuyCach { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongTonHienCo { get; set; }
    public decimal SoLuongDangDuTru { get; set; }
    public decimal MucTonToiThieu { get; set; }
    public string? MoTa { get; set; }

    public ICollection<DINH_MUC_LINH_KIEN> DinhMucLinhKiens { get; set; } = [];
    public ICollection<CHI_TIET_CAP_NHAT_TON_KHO> ChiTietCapNhatTonKhos { get; set; } = [];
    public ICollection<CHI_TIET_PHIEU_XUAT> ChiTietPhieuXuats { get; set; } = [];
}
