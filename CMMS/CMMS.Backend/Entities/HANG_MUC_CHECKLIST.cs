namespace CMMS.Backend.Entities;

public class HANG_MUC_CHECKLIST
{
    public int HangMucId { get; set; }
    public string MauChecklistId { get; set; } = string.Empty;
    public string NhomBoPhan { get; set; } = string.Empty;
    public string NoiDungCongViec { get; set; } = string.Empty;
    public string KieuKetQua { get; set; } = string.Empty;
    public string TieuChuanYeuCau { get; set; } = string.Empty;
    public decimal? GiaTriMin { get; set; }
    public decimal? GiaTriMax { get; set; }
    public string? DonViDo { get; set; }
    public int ThuTuThucHien { get; set; } = 1;

    public MAU_CHECKLIST MauChecklist { get; set; } = null!;
    public ICollection<KET_QUA_CHECKLIST> KetQuas { get; set; } = [];
}
