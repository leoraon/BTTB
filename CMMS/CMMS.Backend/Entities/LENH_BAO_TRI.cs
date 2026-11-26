namespace CMMS.Backend.Entities;

public class LENH_BAO_TRI
{
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string LoaiLenh { get; set; } = string.Empty;
    public string? YeuCauId { get; set; }
    public string? KeHoachId { get; set; }
    public string NoiDungCongViec { get; set; } = string.Empty;
    public int MucUuTienId { get; set; }
    public DateTime ThoiGianDuKienBatDau { get; set; }
    public DateTime ThoiGianDuKienKetThuc { get; set; }
    public DateTime? ThoiDiemThucTeBatDau { get; set; }
    public DateTime? ThoiDiemThucTeKetThuc { get; set; }
    public string? LyDoTamDung { get; set; }
    public string TrangThaiLenh { get; set; } = "MoiTao";
    public string? TinhTrangSauBaoTri { get; set; }
    public string? NguoiBanGiaoId { get; set; }
    public string? NguoiKyNhanId { get; set; }
    public DateTime? ThoiDiemNghiemThu { get; set; }
    public string? NhanXetNghiemThu { get; set; }
    public string? NguoiTaoId { get; set; }
    public string? NguoiDuyetDongId { get; set; }

    public THIET_BI ThietBi { get; set; } = null!;
    public YEU_CAU_BAO_TRI? YeuCau { get; set; }
    public KE_HOACH_BAO_TRI? KeHoach { get; set; }
    public CAU_HINH_MUC_UU_TIEN MucUuTien { get; set; } = null!;
    public NHAN_VIEN? NguoiBanGiao { get; set; }
    public NHAN_VIEN? NguoiKyNhan { get; set; }
    public NHAN_VIEN? NguoiTao { get; set; }
    public NHAN_VIEN? NguoiDuyetDong { get; set; }
    public ICollection<PHAN_CONG_KTV> PhanCongKtvs { get; set; } = [];
    public ICollection<KET_QUA_CHECKLIST> KetQuaChecklists { get; set; } = [];
    public BAO_CAO_SU_CO? BaoCaoSuCo { get; set; }
    public ICollection<PHIEU_DU_TRU_XUAT_DUNG> PhieuDuTruXuatDungs { get; set; } = [];
}
