namespace CMMS.Backend.Entities;

public class BAO_CAO_SU_CO
{
    public long BaoCaoSuCoId { get; set; }
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string MaLoiChinhThucId { get; set; } = string.Empty;
    public bool CoDungMay { get; set; }
    public DateTime? ThoiDiemXayRa { get; set; }
    public DateTime? ThoiDiemKhoiPhuc { get; set; }
    public string MoTaHuHong { get; set; } = string.Empty;
    public string NguyenNhanGocRe { get; set; } = string.Empty;
    public int MucDoAnhHuongPhanTram { get; set; } = 100;
    public string BienPhapKhacPhuc { get; set; } = string.Empty;
    public string NguoiLapBaoCaoId { get; set; } = string.Empty;

    public LENH_BAO_TRI LenhBaoTri { get; set; } = null!;
    public DANH_MUC_MA_LOI MaLoiChinhThuc { get; set; } = null!;
    public NHAN_VIEN NguoiLapBaoCao { get; set; } = null!;
}
