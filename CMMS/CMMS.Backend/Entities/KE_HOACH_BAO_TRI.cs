namespace CMMS.Backend.Entities;

public class KE_HOACH_BAO_TRI
{
    public string KeHoachId { get; set; } = string.Empty;
    public string TenKeHoach { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string MauChecklistId { get; set; } = string.Empty;
    public string DonViChuKy { get; set; } = string.Empty;
    public int GiaTriChuKy { get; set; }
    public int SoNgaySinhLenhTruoc { get; set; } = 3;
    public int ThoiGianDuKienPhut { get; set; } = 60;
    public DateOnly NgayBatDauApDung { get; set; }
    public DateOnly? NgayHoanThanhGanNhat { get; set; }
    public DateOnly NgayDenHanTiepTheo { get; set; }
    public string TrangThaiKeHoach { get; set; } = "DangApDung";
    public string? MoTa { get; set; }

    public THIET_BI ThietBi { get; set; } = null!;
    public MAU_CHECKLIST MauChecklist { get; set; } = null!;
    public ICollection<LENH_BAO_TRI> LenhBaoTris { get; set; } = [];
}
