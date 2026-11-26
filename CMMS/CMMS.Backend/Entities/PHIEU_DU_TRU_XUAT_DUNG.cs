namespace CMMS.Backend.Entities;

public class PHIEU_DU_TRU_XUAT_DUNG
{
    public string PhieuXuatId { get; set; } = string.Empty;
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string NguoiLapDuTruId { get; set; } = string.Empty;
    public DateTime ThoiDiemLap { get; set; }
    public string? NguoiKhaiBaoId { get; set; }
    public DateTime? ThoiDiemKhaiBao { get; set; }
    public string? NguoiDuyetVuotId { get; set; }
    public DateTime? ThoiDiemDuyetVuot { get; set; }
    public string? NguoiXacNhanXuatId { get; set; }
    public DateTime? ThoiDiemXacNhan { get; set; }
    public string TrangThaiPhieu { get; set; } = "DangDuTru";
    public string? GhiChu { get; set; }

    public LENH_BAO_TRI LenhBaoTri { get; set; } = null!;
    public NHAN_VIEN NguoiLapDuTru { get; set; } = null!;
    public NHAN_VIEN? NguoiKhaiBao { get; set; }
    public NHAN_VIEN? NguoiDuyetVuot { get; set; }
    public NHAN_VIEN? NguoiXacNhanXuat { get; set; }
    public ICollection<CHI_TIET_PHIEU_XUAT> ChiTiets { get; set; } = [];
}
