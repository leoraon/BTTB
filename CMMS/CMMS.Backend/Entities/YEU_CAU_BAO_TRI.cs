namespace CMMS.Backend.Entities;

public class YEU_CAU_BAO_TRI
{
    public string YeuCauId { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string NguoiBaoId { get; set; } = string.Empty;
    public DateTime ThoiDiemGui { get; set; }
    public string MoTaTrieuChung { get; set; } = string.Empty;
    public string? DuongDanAnhSuCo { get; set; }
    public bool MayDangDung { get; set; }
    public int MucUuTienDeXuatId { get; set; }
    public int? MucUuTienChinhThucId { get; set; }
    public DateTime HanPhanHoi { get; set; }
    public string TrangThaiYeuCau { get; set; } = "ChoTiepNhan";
    public string? MaLoiId { get; set; }
    public string? NhanDinhNguyenNhanSoBo { get; set; }
    public string? HuongXuLy { get; set; }
    public string? NguoiDuyetId { get; set; }
    public DateTime? ThoiDiemDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }

    public THIET_BI ThietBi { get; set; } = null!;
    public NHAN_VIEN NguoiBao { get; set; } = null!;
    public CAU_HINH_MUC_UU_TIEN MucUuTienDeXuat { get; set; } = null!;
    public CAU_HINH_MUC_UU_TIEN? MucUuTienChinhThuc { get; set; }
    public DANH_MUC_MA_LOI? MaLoi { get; set; }
    public NHAN_VIEN? NguoiDuyet { get; set; }
}
