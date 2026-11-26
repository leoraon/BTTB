namespace CMMS.Backend.Entities;

public class THONG_SO_THIET_BI
{
    public long ThongSoId { get; set; }
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThongSo { get; set; } = string.Empty;
    public string GiaTri { get; set; } = string.Empty;
    public string? DonViDo { get; set; }
    public string? MoTa { get; set; }

    public THIET_BI ThietBi { get; set; } = null!;
}
