namespace CMMS.Backend.Entities;

public class CHI_TIET_CAP_NHAT_TON_KHO
{
    public long ChiTietCapNhatId { get; set; }
    public string PhieuCapNhatId { get; set; } = string.Empty;
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongThayDoi { get; set; }
    public string? GhiChu { get; set; }

    public PHIEU_CAP_NHAT_TON_KHO PhieuCapNhat { get; set; } = null!;
    public PHU_TUNG PhuTung { get; set; } = null!;
}
