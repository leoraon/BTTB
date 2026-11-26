namespace CMMS.Backend.Entities;

public class PHAN_XUONG_BO_PHAN
{
    public string DonViId { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public string? DonViChaId { get; set; }
    public string LoaiDonVi { get; set; } = string.Empty;
    public string? NguoiPhuTrachId { get; set; }
    public string? MoTa { get; set; }

    public PHAN_XUONG_BO_PHAN? DonViCha { get; set; }
    public ICollection<PHAN_XUONG_BO_PHAN> DonViCon { get; set; } = [];
    public NHAN_VIEN? NguoiPhuTrach { get; set; }
    public ICollection<NHAN_VIEN> NhanViens { get; set; } = [];
    public ICollection<THIET_BI> ThietBiTaiViTri { get; set; } = [];
    public ICollection<THIET_BI> ThietBiQuanLy { get; set; } = [];
}
