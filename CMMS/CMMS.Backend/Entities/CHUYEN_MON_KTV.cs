namespace CMMS.Backend.Entities;

public class CHUYEN_MON_KTV
{
    public string NhanVienId { get; set; } = string.Empty;
    public string NhomThietBiId { get; set; } = string.Empty;
    public string? GhiChu { get; set; }

    public NHAN_VIEN NhanVien { get; set; } = null!;
    public NHOM_THIET_BI NhomThietBi { get; set; } = null!;
}
