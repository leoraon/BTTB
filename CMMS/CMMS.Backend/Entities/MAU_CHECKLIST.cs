namespace CMMS.Backend.Entities;

public class MAU_CHECKLIST
{
    public string MauChecklistId { get; set; } = string.Empty;
    public string TenMau { get; set; } = string.Empty;
    public string NhomThietBiId { get; set; } = string.Empty;
    public string? MoTaTieuChuan { get; set; }
    public string TrangThaiHieuLuc { get; set; } = "DangApDung";

    public NHOM_THIET_BI NhomThietBi { get; set; } = null!;
    public ICollection<HANG_MUC_CHECKLIST> HangMucs { get; set; } = [];
    public ICollection<KE_HOACH_BAO_TRI> KeHoachs { get; set; } = [];
}
