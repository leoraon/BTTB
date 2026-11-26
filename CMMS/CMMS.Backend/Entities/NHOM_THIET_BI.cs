namespace CMMS.Backend.Entities;

public class NHOM_THIET_BI
{
    public string NhomThietBiId { get; set; } = string.Empty;
    public string TenNhom { get; set; } = string.Empty;
    public bool LaCumChiTiet { get; set; }
    public string? MoTa { get; set; }

    public ICollection<THIET_BI> ThietBis { get; set; } = [];
    public ICollection<MAU_CHECKLIST> MauChecklists { get; set; } = [];
    public ICollection<CHUYEN_MON_KTV> ChuyenMonKtvs { get; set; } = [];
}
