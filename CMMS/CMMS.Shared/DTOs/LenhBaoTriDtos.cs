namespace CMMS.Shared.DTOs;

public class ChuyenMonKtvDto
{
    public string NhanVienId { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? ChuyenNganh { get; set; }
    public string? BacTho { get; set; }
    public string? CaLamViec { get; set; }
    public string? TrangThaiSanSang { get; set; }
    public List<NhomKtvItemDto> NhomThietBi { get; set; } = [];
}

public class NhomKtvItemDto
{
    public string NhomThietBiId { get; set; } = string.Empty;
    public string TenNhom { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public class CapNhatChuyenMonKtvReq
{
    public List<NhomKtvItemReq> DanhSachNhom { get; set; } = [];
}

public class NhomKtvItemReq
{
    public string NhomThietBiId { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public class PhanCongKtvDto
{
    public long PhanCongId { get; set; }
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string KyThuatVienId { get; set; } = string.Empty;
    public string TenKyThuatVien { get; set; } = string.Empty;
    public string? CaLamViec { get; set; }
    public string NguoiPhanCongId { get; set; } = string.Empty;
    public DateTime ThoiDiemPhanCong { get; set; }
    public DateTime KhungGioBatDau { get; set; }
    public DateTime KhungGioKetThuc { get; set; }
    public string VaiTroTrongLenh { get; set; } = string.Empty;
    public decimal SoGioCongThucTe { get; set; }
    public string TrangThaiPhanCong { get; set; } = string.Empty;
}

public class PhanCongKtvReq
{
    public string KyThuatVienId { get; set; } = string.Empty;
    public DateTime KhungGioBatDau { get; set; }
    public DateTime KhungGioKetThuc { get; set; }
    public string VaiTroTrongLenh { get; set; } = "ThoChinh";
    public bool ChoPhepChenNgang { get; set; }
    public string? LyDoTamDung { get; set; }
}

public class LenhBaoTriDto
{
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string LoaiLenh { get; set; } = string.Empty;
    public string? YeuCauId { get; set; }
    public string? KeHoachId { get; set; }
    public string NoiDungCongViec { get; set; } = string.Empty;
    public int MucUuTienId { get; set; }
    public string TenMucUuTien { get; set; } = string.Empty;
    public string MaMauHienThi { get; set; } = string.Empty;
    public DateTime ThoiGianDuKienBatDau { get; set; }
    public DateTime ThoiGianDuKienKetThuc { get; set; }
    public DateTime? ThoiDiemThucTeBatDau { get; set; }
    public DateTime? ThoiDiemThucTeKetThuc { get; set; }
    public string? LyDoTamDung { get; set; }
    public string TrangThaiLenh { get; set; } = string.Empty;
    public string? TinhTrangSauBaoTri { get; set; }
    public string? NguoiBanGiaoId { get; set; }
    public string? TenNguoiBanGiao { get; set; }
    public string? NguoiKyNhanId { get; set; }
    public string? TenNguoiKyNhan { get; set; }
    public DateTime? ThoiDiemNghiemThu { get; set; }
    public string? NhanXetNghiemThu { get; set; }
    public string? NguoiTaoId { get; set; }
    public string? NguoiDuyetDongId { get; set; }

    private bool? _isTreHanOverride;
    public bool IsTreHan
    {
        get => _isTreHanOverride ?? (TrangThaiLenh != "DaHuy" && (
            ThoiDiemThucTeKetThuc.HasValue
                ? ThoiDiemThucTeKetThuc.Value > ThoiGianDuKienKetThuc
                : TrangThaiLenh != "DaDong" && DateTime.Now > ThoiGianDuKienKetThuc));
        set => _isTreHanOverride = value;
    }
}

public class LenhBaoTriDetailDto : LenhBaoTriDto
{
    public List<PhanCongKtvDto> PhanCong { get; set; } = [];
    public List<KetQuaChecklistDto> KetQuaChecklist { get; set; } = [];
    public BaoCaoSuCoDto? BaoCaoSuCo { get; set; }
    public List<PhieuDuTruXuatDto> PhieuVatTu { get; set; } = [];
}

public class TaoLenhTuYeuCauReq
{
    public string YeuCauId { get; set; } = string.Empty;
    public string NoiDungCongViec { get; set; } = string.Empty;
    public DateTime ThoiGianDuKienBatDau { get; set; }
    public DateTime ThoiGianDuKienKetThuc { get; set; }
}

public class DoiLichLenhReq
{
    public DateTime ThoiGianDuKienBatDau { get; set; }
    public DateTime ThoiGianDuKienKetThuc { get; set; }
}

public class TamDungLenhReq
{
    public string LyDoTamDung { get; set; } = string.Empty;
}

public class KetThucLenhReq
{
    public DateTime? ThoiDiemThucTeBatDau { get; set; }
    public DateTime? ThoiDiemThucTeKetThuc { get; set; }
    public List<GioCongKtvReq> GioCongKtv { get; set; } = [];
}

public class GioCongKtvReq
{
    public long PhanCongId { get; set; }
    public decimal SoGioCongThucTe { get; set; }
}

public class KetQuaChecklistDto
{
    public long KetQuaId { get; set; }
    public string LenhBaoTriId { get; set; } = string.Empty;
    public int HangMucId { get; set; }
    public string NhomBoPhan { get; set; } = string.Empty;
    public string NoiDungCongViec { get; set; } = string.Empty;
    public string KieuKetQua { get; set; } = string.Empty;
    public decimal? GiaTriMin { get; set; }
    public decimal? GiaTriMax { get; set; }
    public string? DonViDo { get; set; }
    public decimal? GiaTriDoThucTe { get; set; }
    public string KetLuan { get; set; } = string.Empty;
    public string NguoiKiemTraId { get; set; } = string.Empty;
    public string TenNguoiKiemTra { get; set; } = string.Empty;
    public DateTime ThoiDiemKiemTra { get; set; }
    public string? GhiChu { get; set; }
}

public class KetQuaChecklistItemReq
{
    public int HangMucId { get; set; }
    public decimal? GiaTriDoThucTe { get; set; }
    public string? KetLuan { get; set; }
    public string? GhiChu { get; set; }
}

public class BaoCaoSuCoDto
{
    public long BaoCaoSuCoId { get; set; }
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string MaLoiChinhThucId { get; set; } = string.Empty;
    public string TenLoaiSuCo { get; set; } = string.Empty;
    public bool CoDungMay { get; set; }
    public DateTime? ThoiDiemXayRa { get; set; }
    public DateTime? ThoiDiemKhoiPhuc { get; set; }
    public double TongGioDungMay => CoDungMay && ThoiDiemXayRa.HasValue && ThoiDiemKhoiPhuc.HasValue
        ? Math.Round((ThoiDiemKhoiPhuc.Value - ThoiDiemXayRa.Value).TotalHours, 2)
        : 0;
    public string MoTaHuHong { get; set; } = string.Empty;
    public string NguyenNhanGocRe { get; set; } = string.Empty;
    public int MucDoAnhHuongPhanTram { get; set; }
    public string BienPhapKhacPhuc { get; set; } = string.Empty;
    public string NguoiLapBaoCaoId { get; set; } = string.Empty;
    public string TenNguoiLap { get; set; } = string.Empty;
}

public class BaoCaoSuCoUpsertReq
{
    public string MaLoiChinhThucId { get; set; } = string.Empty;
    public bool CoDungMay { get; set; }
    public DateTime? ThoiDiemXayRa { get; set; }
    public DateTime? ThoiDiemKhoiPhuc { get; set; }
    public string MoTaHuHong { get; set; } = string.Empty;
    public string NguyenNhanGocRe { get; set; } = string.Empty;
    public int MucDoAnhHuongPhanTram { get; set; } = 100;
    public string BienPhapKhacPhuc { get; set; } = string.Empty;
}

public class NghiemThuLenhReq
{
    public string NguoiBanGiaoId { get; set; } = string.Empty;
    public string NguoiKyNhanId { get; set; } = string.Empty;
    public string TinhTrangSauBaoTri { get; set; } = "DatChuan";
    public string? NhanXetNghiemThu { get; set; }
}

public class HuyLenhReq
{
    public string? LyDoHuy { get; set; }
}
