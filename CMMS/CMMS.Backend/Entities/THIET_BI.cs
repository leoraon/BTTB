namespace CMMS.Backend.Entities;

public class THIET_BI
{
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string? ThietBiChaId { get; set; }
    public string NhomThietBiId { get; set; } = string.Empty;
    public string ViTriLapDatId { get; set; } = string.Empty;
    public string BoPhanQuanLyId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string NhaSanXuat { get; set; } = string.Empty;
    public DateOnly NgayDuaVaoSuDung { get; set; }
    public int ChuKyBaoTriKhuyenNghiNgay { get; set; } = 30;
    public string CongDungMoTa { get; set; } = string.Empty;
    public string? LuuYAnToan { get; set; }
    public string TrangThaiVanHanh { get; set; } = "DangHoatDong";
    public string? DuongDanAnh { get; set; }
    public string? DuongDanTaiLieu { get; set; }

    public THIET_BI? ThietBiCha { get; set; }
    public ICollection<THIET_BI> CumCon { get; set; } = [];
    public NHOM_THIET_BI NhomThietBi { get; set; } = null!;
    public PHAN_XUONG_BO_PHAN ViTriLapDat { get; set; } = null!;
    public PHAN_XUONG_BO_PHAN BoPhanQuanLy { get; set; } = null!;
    public ICollection<THONG_SO_THIET_BI> ThongSos { get; set; } = [];
    public ICollection<DINH_MUC_LINH_KIEN> DinhMucLinhKiens { get; set; } = [];
    public ICollection<KE_HOACH_BAO_TRI> KeHoachBaoTris { get; set; } = [];
    public ICollection<YEU_CAU_BAO_TRI> YeuCauBaoTris { get; set; } = [];
    public ICollection<LENH_BAO_TRI> LenhBaoTris { get; set; } = [];
}
