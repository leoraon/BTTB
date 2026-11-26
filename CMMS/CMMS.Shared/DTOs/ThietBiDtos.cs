namespace CMMS.Shared.DTOs;

// 2.1 Danh mục dùng chung
public class DonViDto
{
    public string DonViId { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public string? DonViChaId { get; set; }
    public string? TenDonViCha { get; set; }
    public string LoaiDonVi { get; set; } = string.Empty;
    public string? NguoiPhuTrachId { get; set; }
    public string? TenNguoiPhuTrach { get; set; }
    public string? MoTa { get; set; }
    public List<DonViDto> DonViCon { get; set; } = [];
}

public class DonViUpsertReq
{
    public string DonViId { get; set; } = string.Empty;
    public string TenDonVi { get; set; } = string.Empty;
    public string? DonViChaId { get; set; }
    public string LoaiDonVi { get; set; } = string.Empty;
    public string? NguoiPhuTrachId { get; set; }
    public string? MoTa { get; set; }
}

public class NhomThietBiDto
{
    public string NhomThietBiId { get; set; } = string.Empty;
    public string TenNhom { get; set; } = string.Empty;
    public bool LaCumChiTiet { get; set; }
    public string? MoTa { get; set; }
}

public class MaLoiDto
{
    public string MaLoiId { get; set; } = string.Empty;
    public string TenLoaiSuCo { get; set; } = string.Empty;
    public string MoTaDauHieu { get; set; } = string.Empty;
}

// 2.2 - 2.4 Hồ sơ Thiết bị & Thông số kỹ thuật
public class ThongSoThietBiDto
{
    public long ThongSoId { get; set; }
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThongSo { get; set; } = string.Empty;
    public string GiaTri { get; set; } = string.Empty;
    public string? DonViDo { get; set; }
    public string? MoTa { get; set; }
}

public class ThongSoUpsertReq
{
    public string TenThongSo { get; set; } = string.Empty;
    public string GiaTri { get; set; } = string.Empty;
    public string? DonViDo { get; set; }
    public string? MoTa { get; set; }
}

public class ThietBiListDto
{
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string? ThietBiChaId { get; set; }
    public string? TenThietBiCha { get; set; }
    public string NhomThietBiId { get; set; } = string.Empty;
    public string TenNhomThietBi { get; set; } = string.Empty;
    public string ViTriLapDatId { get; set; } = string.Empty;
    public string TenViTriLapDat { get; set; } = string.Empty;
    public string BoPhanQuanLyId { get; set; } = string.Empty;
    public string TenBoPhanQuanLy { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string NhaSanXuat { get; set; } = string.Empty;
    public DateOnly NgayDuaVaoSuDung { get; set; }
    public int TuoiThoNgay => Math.Max(0, DateOnly.FromDateTime(DateTime.Today).DayNumber - NgayDuaVaoSuDung.DayNumber);
    public int ChuKyBaoTriKhuyenNghiNgay { get; set; }
    public string TrangThaiVanHanh { get; set; } = string.Empty;
    public string? DuongDanAnh { get; set; }
    public string? DuongDanTaiLieu { get; set; }
    public List<ThietBiListDto> CumCon { get; set; } = [];
}

public class ThietBiUpsertReq
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
}

public class UpdateTrangThaiThietBiReq
{
    public string TrangThaiVanHanh { get; set; } = string.Empty;
}

public class LichSuBaoTriThietBiDto
{
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string LoaiLenh { get; set; } = string.Empty;
    public string NoiDungCongViec { get; set; } = string.Empty;
    public DateTime? ThoiDiemThucTeBatDau { get; set; }
    public DateTime? ThoiDiemThucTeKetThuc { get; set; }
    public string TrangThaiLenh { get; set; } = string.Empty;
    public string? TinhTrangSauBaoTri { get; set; }
    public string? NhanXetNghiemThu { get; set; }
    public string? MaLoiChinhThucId { get; set; }
    public string? BienPhapKhacPhuc { get; set; }
}

public class PhieuLyLichThietBiDto
{
    public ThietBiListDto ThongTinChung { get; set; } = new();
    public string CongDungMoTa { get; set; } = string.Empty;
    public string? LuuYAnToan { get; set; }
    public List<ThongSoThietBiDto> ThongSoKyThuat { get; set; } = [];
    public List<DinhMucLinhKienDto> DinhMucLinhKien { get; set; } = [];
    public List<LichSuBaoTriThietBiDto> LichSuBaoTri { get; set; } = [];
}
