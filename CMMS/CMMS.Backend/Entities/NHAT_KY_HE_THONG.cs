namespace CMMS.Backend.Entities;

public class NHAT_KY_HE_THONG
{
    public long NhatKyId { get; set; }
    public int? TaiKhoanId { get; set; }
    public DateTime ThoiDiem { get; set; }
    public string LoaiThaoTac { get; set; } = string.Empty;
    public string? DoiTuongTacDong { get; set; }
    public string MoTaChiTiet { get; set; } = string.Empty;

    public TAI_KHOAN? TaiKhoan { get; set; }
}
