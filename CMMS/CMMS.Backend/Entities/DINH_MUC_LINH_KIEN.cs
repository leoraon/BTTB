namespace CMMS.Backend.Entities;

public class DINH_MUC_LINH_KIEN
{
    public long DinhMucId { get; set; }
    public string ThietBiId { get; set; } = string.Empty;
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongDinhMuc { get; set; } = 1.00m;
    public bool ThayTheDinhKy { get; set; }
    public string? ViTriLapDat { get; set; }

    public THIET_BI ThietBi { get; set; } = null!;
    public PHU_TUNG PhuTung { get; set; } = null!;
}
