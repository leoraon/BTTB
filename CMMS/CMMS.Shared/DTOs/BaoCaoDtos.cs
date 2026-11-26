namespace CMMS.Shared.DTOs;

public class DashboardSummaryDto
{
    public int SoMayDaBaoTriTrongThang { get; set; }
    public int SoMayChuaBaoTriTrongThang { get; set; }
    public int SoMayQuaHanPM { get; set; }
    public int TongSoThietBi { get; set; }
    public int TongPhuTung { get; set; }
    public int SoPhuTungDuoiToiThieu { get; set; }
    public double TongGioDungMayTrongThang { get; set; }
    public List<TrangThaiCountDto> CoCauLenhTheoTrangThai { get; set; } = [];

    // 4 danh sách cảnh báo nguy cấp (7.1.2)
    public List<ThietBiListDto> MayDangDungSuCo { get; set; } = [];
    public List<YeuCauBaoTriDto> YeuCauQuaHanPhanHoi { get; set; } = [];
    public List<KeHoachBaoTriDto> KeHoachQuaHan { get; set; } = [];
    public List<PhuTungDto> PhuTungDuoiToiThieu { get; set; } = [];
}

public class TrangThaiCountDto
{
    public string TrangThai { get; set; } = string.Empty;
    public int SoLuong { get; set; }
}

public class ReportFilterReq
{
    public DateTime TuNgay { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime DenNgay { get; set; } = DateTime.Now;
    public string? ViTriLapDatId { get; set; }
    public string? BoPhanQuanLyId { get; set; }
    public string? NhomThietBiId { get; set; }
    public string? TrangThaiVanHanh { get; set; }
    public bool ChiDuoiToiThieu { get; set; }
}

public class Bc1TongHopDto
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public List<TrangThaiCountDto> ThietBiTheoTrangThai { get; set; } = [];
    public List<TrangThaiCountDto> YeuCauTheoTrangThai { get; set; } = [];
    public List<TrangThaiCountDto> LenhTheoLoai { get; set; } = [];
    public List<TrangThaiCountDto> LenhTheoTrangThai { get; set; } = [];
    public int SoKeHoachPmDungHan { get; set; }
    public int SoKeHoachPmTreHan { get; set; }
    public double TyLePmDungHanPhanTram { get; set; }
    public decimal TongGioCongKtv { get; set; }
    public double TongGioDungMaySuCo { get; set; }
    public decimal TongSoLuongPhuTungXuat { get; set; }
}

public class Bc3TienDoPmItemDto
{
    public string KeHoachId { get; set; } = string.Empty;
    public string TenKeHoach { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public DateOnly NgayDenHanTiepTheo { get; set; }
    public string? LenhBaoTriId { get; set; }
    public string NhomKetQua { get; set; } = string.Empty; // HoanThanhDungHan | HoanThanhTreHan | ChuaThucHien | DangQuaHan
    public DateTime? ThoiDiemHoanThanh { get; set; }
}

public class Bc4SuCoMaLoiDto
{
    public string MaLoiId { get; set; } = string.Empty;
    public string TenLoaiSuCo { get; set; } = string.Empty;
    public int SoLanXayRa { get; set; }
    public double TongGioDungMay { get; set; }
    public List<string> DanhSachNguyenNhanGoc { get; set; } = [];
}

public class Bc5KhoiLuongKtvDto
{
    public List<Bc5KtvItemDto> TheoKyThuatVien { get; set; } = [];
    public List<Bc5BoPhanItemDto> TheoBoPhan { get; set; } = [];
}

public class Bc5KtvItemDto
{
    public string NhanVienId { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public int SoLenhThamGia { get; set; }
    public decimal TongGioCong { get; set; }
}

public class Bc5BoPhanItemDto
{
    public string DonViId { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public int SoLuotThamGia { get; set; }
    public decimal TongGioCong { get; set; }
}

public class Bc7XuatTonDoiChieuDto
{
    public List<Bc7BienDongPhuTungDto> BienDongTheoPhuTung { get; set; } = [];
    public List<Bc7DoiChieuLenhDto> DoiChieuDuTruThucDung { get; set; } = [];
}

public class Bc7BienDongPhuTungDto
{
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal TongBoSung { get; set; }
    public decimal TongXuatDung { get; set; }
    public decimal TonCuoiKyHienCo { get; set; }
}

public class Bc7DoiChieuLenhDto
{
    public string PhieuXuatId { get; set; } = string.Empty;
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string? NguoiKhaiBaoId { get; set; }
    public string? TenNguoiKhaiBao { get; set; }
    public decimal SoLuongDuTru { get; set; }
    public decimal SoLuongThucDung { get; set; }
    public decimal ChenhLech => SoLuongThucDung - SoLuongDuTru;
    public string? GhiChuGiaiTrinh { get; set; }
}
