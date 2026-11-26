namespace CMMS.Shared.DTOs;

public class PhuTungDto
{
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string LoaiPhuTung { get; set; } = string.Empty;
    public string QuyCach { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongTonHienCo { get; set; }
    public decimal SoLuongDangDuTru { get; set; }
    public decimal TonKhoKhaDung => SoLuongTonHienCo - SoLuongDangDuTru;
    public decimal MucTonToiThieu { get; set; }
    public bool IsDuoiMucToiThieu => SoLuongTonHienCo <= MucTonToiThieu;
    public string? MoTa { get; set; }
}

public class PhuTungUpsertReq
{
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string LoaiPhuTung { get; set; } = string.Empty;
    public string QuyCach { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongTonHienCo { get; set; }
    public decimal MucTonToiThieu { get; set; }
    public string? MoTa { get; set; }
}

public class DinhMucLinhKienDto
{
    public long DinhMucId { get; set; }
    public string ThietBiId { get; set; } = string.Empty;
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongDinhMuc { get; set; }
    public bool ThayTheDinhKy { get; set; }
    public string? ViTriLapDat { get; set; }
    public decimal TonKhoKhaDung { get; set; }
}

public class DinhMucUpsertReq
{
    public string ThietBiId { get; set; } = string.Empty;
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongDinhMuc { get; set; } = 1;
    public bool ThayTheDinhKy { get; set; }
    public string? ViTriLapDat { get; set; }
}

public class PhieuCapNhatTonKhoDto
{
    public string PhieuCapNhatId { get; set; } = string.Empty;
    public string LoaiCapNhat { get; set; } = string.Empty;
    public string? MaPhieuKhoNgoai { get; set; }
    public DateTime ThoiDiemLap { get; set; }
    public string NguoiLapId { get; set; } = string.Empty;
    public string TenNguoiLap { get; set; } = string.Empty;
    public string? NguoiXacNhanId { get; set; }
    public string? TenNguoiXacNhan { get; set; }
    public DateTime? ThoiDiemXacNhan { get; set; }
    public string TrangThaiPhieu { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public List<ChiTietCapNhatTonKhoDto> ChiTiet { get; set; } = [];
}

public class ChiTietCapNhatTonKhoDto
{
    public long ChiTietCapNhatId { get; set; }
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongThayDoi { get; set; }
    public string? GhiChu { get; set; }
}

public class TaoPhieuCapNhatTonKhoReq
{
    public string LoaiCapNhat { get; set; } = string.Empty;
    public string? MaPhieuKhoNgoai { get; set; }
    public string? GhiChu { get; set; }
    public List<ChiTietCapNhatItemReq> ChiTiet { get; set; } = [];
}

public class ChiTietCapNhatItemReq
{
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongThayDoi { get; set; }
    public string? GhiChu { get; set; }
}

public class PhieuDuTruXuatDto
{
    public string PhieuXuatId { get; set; } = string.Empty;
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string NguoiLapDuTruId { get; set; } = string.Empty;
    public string TenNguoiLap { get; set; } = string.Empty;
    public DateTime ThoiDiemLap { get; set; }
    public string? NguoiKhaiBaoId { get; set; }
    public DateTime? ThoiDiemKhaiBao { get; set; }
    public string? NguoiDuyetVuotId { get; set; }
    public DateTime? ThoiDiemDuyetVuot { get; set; }
    public string? NguoiXacNhanXuatId { get; set; }
    public DateTime? ThoiDiemXacNhan { get; set; }
    public string TrangThaiPhieu { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public List<ChiTietPhieuXuatDto> ChiTiet { get; set; } = [];
}

public class ChiTietPhieuXuatDto
{
    public long ChiTietId { get; set; }
    public string PhuTungId { get; set; } = string.Empty;
    public string TenPhuTung { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongDuTru { get; set; }
    public decimal? SoLuongThucDung { get; set; }
    public decimal? ChenhLech => SoLuongThucDung.HasValue ? SoLuongThucDung.Value - SoLuongDuTru : null;
    public decimal TonKhoHienCo { get; set; }
    public decimal TonKhoKhaDung { get; set; }
    public string? GhiChuGiaiTrinh { get; set; }
}

public class TaoPhieuDuTruReq
{
    public string LenhBaoTriId { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public List<ChiTietDuTruItemReq> ChiTiet { get; set; } = [];
}

public class ChiTietDuTruItemReq
{
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongDuTru { get; set; }
    public string? GhiChuGiaiTrinh { get; set; }
}

public class KhaiBaoThucDungReq
{
    public List<ChiTietThucDungItemReq> ChiTiet { get; set; } = [];
}

public class ChiTietThucDungItemReq
{
    public string PhuTungId { get; set; } = string.Empty;
    public decimal SoLuongThucDung { get; set; }
    public string? GhiChuGiaiTrinh { get; set; }
}

public class DuyetVuotDuTruReq
{
    public bool LaPheDuyet { get; set; } = true;
    public string? GhiChu { get; set; }
}
