namespace CMMS.Shared.DTOs;

public class MucUuTienDto
{
    public int MucUuTienId { get; set; }
    public string TenMucUuTien { get; set; } = string.Empty;
    public int ThuTuUuTien { get; set; }
    public string MaMauHienThi { get; set; } = string.Empty;
    public int ThoiHanPhanHoiPhut { get; set; }
}

public class YeuCauBaoTriDto
{
    public string YeuCauId { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string NguoiBaoId { get; set; } = string.Empty;
    public string TenNguoiBao { get; set; } = string.Empty;
    public DateTime ThoiDiemGui { get; set; }
    public string MoTaTrieuChung { get; set; } = string.Empty;
    public string? DuongDanAnhSuCo { get; set; }
    public bool MayDangDung { get; set; }
    public int MucUuTienDeXuatId { get; set; }
    public string TenMucUuTienDeXuat { get; set; } = string.Empty;
    public string MaMauDeXuat { get; set; } = string.Empty;
    public int ThuTuUuTienHienThi { get; set; }
    public int? MucUuTienChinhThucId { get; set; }
    public string? TenMucUuTienChinhThuc { get; set; }
    public string? MaMauChinhThuc { get; set; }
    public DateTime HanPhanHoi { get; set; }
    public string TrangThaiYeuCau { get; set; } = string.Empty;
    public string? MaLoiId { get; set; }
    public string? TenLoaiSuCo { get; set; }
    public string? NhanDinhNguyenNhanSoBo { get; set; }
    public string? HuongXuLy { get; set; }
    public string? NguoiDuyetId { get; set; }
    public string? TenNguoiDuyet { get; set; }
    public DateTime? ThoiDiemDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }

    private bool? _isQuaHanPhanHoiOverride;
    public bool IsQuaHanPhanHoi
    {
        get => _isQuaHanPhanHoiOverride ?? (TrangThaiYeuCau == "ChoTiepNhan" && DateTime.Now > HanPhanHoi);
        set => _isQuaHanPhanHoiOverride = value;
    }

    public bool? PhanHoiDungHan => ThoiDiemDuyet.HasValue ? ThoiDiemDuyet.Value <= HanPhanHoi : null;
}

public class TaoSuaYeuCauReq
{
    public string ThietBiId { get; set; } = string.Empty;
    public string MoTaTrieuChung { get; set; } = string.Empty;
    public string? DuongDanAnhSuCo { get; set; }
    public bool MayDangDung { get; set; }
    public int MucUuTienDeXuatId { get; set; }
}

public class DuyetYeuCauReq
{
    public bool LaPheDuyet { get; set; }
    public string? MaLoiId { get; set; }
    public string? NhanDinhNguyenNhanSoBo { get; set; }
    public int? MucUuTienChinhThucId { get; set; }
    public string? HuongXuLy { get; set; }
    public string? LyDoTuChoi { get; set; }
}
