namespace CMMS.Backend.Entities;

public class PHIEU_CAP_NHAT_TON_KHO
{
    public string PhieuCapNhatId { get; set; } = string.Empty;
    public string LoaiCapNhat { get; set; } = string.Empty;
    public string? MaPhieuKhoNgoai { get; set; }
    public DateTime ThoiDiemLap { get; set; }
    public string NguoiLapId { get; set; } = string.Empty;
    public string? NguoiXacNhanId { get; set; }
    public DateTime? ThoiDiemXacNhan { get; set; }
    public string TrangThaiPhieu { get; set; } = "ChoXacNhan";
    public string? GhiChu { get; set; }

    public NHAN_VIEN NguoiLap { get; set; } = null!;
    public NHAN_VIEN? NguoiXacNhan { get; set; }
    public ICollection<CHI_TIET_CAP_NHAT_TON_KHO> ChiTiets { get; set; } = [];
}
