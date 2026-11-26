namespace CMMS.Backend.Entities;

public class CAU_HINH_MUC_UU_TIEN
{
    public int MucUuTienId { get; set; }
    public string TenMucUuTien { get; set; } = string.Empty;
    public int ThuTuUuTien { get; set; }
    public string MaMauHienThi { get; set; } = string.Empty;
    public int ThoiHanPhanHoiPhut { get; set; }
}
