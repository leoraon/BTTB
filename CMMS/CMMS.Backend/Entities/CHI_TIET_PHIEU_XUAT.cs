namespace CMMS.Backend.Entities;

public class CHI_TIET_PHIEU_XUAT
{
    public long ChiTietId { get; set; }
    public string PhieuXuatId { get; set; } = string.Empty;
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongDuTru { get; set; }
    public decimal? SoLuongThucDung { get; set; }
    public string? GhiChuGiaiTrinh { get; set; }

    public PHIEU_DU_TRU_XUAT_DUNG PhieuXuat { get; set; } = null!;
    public PHU_TUNG PhuTung { get; set; } = null!;
}
