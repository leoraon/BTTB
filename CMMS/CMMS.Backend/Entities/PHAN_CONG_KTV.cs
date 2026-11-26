namespace CMMS.Backend.Entities;

public class PHAN_CONG_KTV
{
    public long PhanCongId { get; set; }
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string KyThuatVienId { get; set; } = string.Empty;
    public string NguoiPhanCongId { get; set; } = string.Empty;
    public DateTime ThoiDiemPhanCong { get; set; }
    public DateTime KhungGioBatDau { get; set; }
    public DateTime KhungGioKetThuc { get; set; }
    public string VaiTroTrongLenh { get; set; } = "ThoChinh";
    public decimal SoGioCongThucTe { get; set; }
    public string TrangThaiPhanCong { get; set; } = "DaGiao";

    public LENH_BAO_TRI LenhBaoTri { get; set; } = null!;
    public NHAN_VIEN KyThuatVien { get; set; } = null!;
    public NHAN_VIEN NguoiPhanCong { get; set; } = null!;
}
