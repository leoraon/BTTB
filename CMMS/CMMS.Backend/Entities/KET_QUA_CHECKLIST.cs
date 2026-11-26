namespace CMMS.Backend.Entities;

public class KET_QUA_CHECKLIST
{
    public long KetQuaId { get; set; }
    public string LenhBaoTriId { get; set; } = string.Empty;
    public int HangMucId { get; set; }
    public decimal? GiaTriDoThucTe { get; set; }
    public string KetLuan { get; set; } = string.Empty;
    public string NguoiKiemTraId { get; set; } = string.Empty;
    public DateTime ThoiDiemKiemTra { get; set; }
    public string? GhiChu { get; set; }

    public LENH_BAO_TRI LenhBaoTri { get; set; } = null!;
    public HANG_MUC_CHECKLIST HangMuc { get; set; } = null!;
    public NHAN_VIEN NguoiKiemTra { get; set; } = null!;
}
