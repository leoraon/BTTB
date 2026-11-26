namespace CMMS.Shared.DTOs;

public class HangMucChecklistDto
{
    public int HangMucId { get; set; }
    public string MauChecklistId { get; set; } = string.Empty;
    public string NhomBoPhan { get; set; } = string.Empty;
    public string NoiDungCongViec { get; set; } = string.Empty;
    public string KieuKetQua { get; set; } = string.Empty;
    public string TieuChuanYeuCau { get; set; } = string.Empty;
    public decimal? GiaTriMin { get; set; }
    public decimal? GiaTriMax { get; set; }
    public string? DonViDo { get; set; }
    public int ThuTuThucHien { get; set; }
}

public class HangMucChecklistReq
{
    public string NhomBoPhan { get; set; } = string.Empty;
    public string NoiDungCongViec { get; set; } = string.Empty;
    public string KieuKetQua { get; set; } = string.Empty;
    public string TieuChuanYeuCau { get; set; } = string.Empty;
    public decimal? GiaTriMin { get; set; }
    public decimal? GiaTriMax { get; set; }
    public string? DonViDo { get; set; }
    public int ThuTuThucHien { get; set; } = 1;
}

public class MauChecklistDto
{
    public string MauChecklistId { get; set; } = string.Empty;
    public string TenMau { get; set; } = string.Empty;
    public string NhomThietBiId { get; set; } = string.Empty;
    public string TenNhomThietBi { get; set; } = string.Empty;
    public string? MoTaTieuChuan { get; set; }
    public string TrangThaiHieuLuc { get; set; } = string.Empty;
    public bool DaKhoaChinhSua { get; set; }
    public List<HangMucChecklistDto> HangMuc { get; set; } = [];
}

public class MauChecklistUpsertReq
{
    public string MauChecklistId { get; set; } = string.Empty;
    public string TenMau { get; set; } = string.Empty;
    public string NhomThietBiId { get; set; } = string.Empty;
    public string? MoTaTieuChuan { get; set; }
    public List<HangMucChecklistReq> HangMuc { get; set; } = [];
}

public class KeHoachBaoTriDto
{
    public string KeHoachId { get; set; } = string.Empty;
    public string TenKeHoach { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string MauChecklistId { get; set; } = string.Empty;
    public string TenMauChecklist { get; set; } = string.Empty;
    public string DonViChuKy { get; set; } = string.Empty;
    public int GiaTriChuKy { get; set; }
    public int SoNgaySinhLenhTruoc { get; set; }
    public int ThoiGianDuKienPhut { get; set; }
    public DateOnly NgayBatDauApDung { get; set; }
    public DateOnly? NgayHoanThanhGanNhat { get; set; }
    public DateOnly NgayDenHanTiepTheo { get; set; }
    public string TrangThaiKeHoach { get; set; } = string.Empty;
    public string? MoTa { get; set; }

    // Trạng thái theo dõi đến hạn / quá hạn (4.3.2)
    public string? LenhDangMoId { get; set; }
    public DateOnly? MocHanMoiNhat { get; set; }
    public string TrangThaiHan { get; set; } = "BinhThuong"; // BinhThuong | SapDenHan | QuaHan_ChuaCoLenh | QuaHan_DangCoLenh
}

public class KeHoachUpsertReq
{
    public string KeHoachId { get; set; } = string.Empty;
    public string TenKeHoach { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string MauChecklistId { get; set; } = string.Empty;
    public string DonViChuKy { get; set; } = "Thang";
    public int GiaTriChuKy { get; set; } = 1;
    public int SoNgaySinhLenhTruoc { get; set; } = 3;
    public int ThoiGianDuKienPhut { get; set; } = 60;
    public DateOnly NgayBatDauApDung { get; set; }
    public DateOnly? NgayDenHanTiepTheo { get; set; }
    public string TrangThaiKeHoach { get; set; } = "DangApDung";
    public string? MoTa { get; set; }
}

public class LichBaoTriDuKienItemDto
{
    public string KeHoachId { get; set; } = string.Empty;
    public string TenKeHoach { get; set; } = string.Empty;
    public string ThietBiId { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string NhomThietBiId { get; set; } = string.Empty;
    public string TenNhomThietBi { get; set; } = string.Empty;
    public string DonViChuKy { get; set; } = string.Empty;
    public int GiaTriChuKy { get; set; }
    public DateOnly NgayDuKien { get; set; }
    public DateOnly NgaySinhLenhDuKien { get; set; }
    public TimeOnly GioBatDauDuKien { get; set; }
    public int ThoiGianDuKienPhut { get; set; }
    public bool LaKyHienTai { get; set; }
    public string? LenhDangMoId { get; set; }
    public string TrangThaiDuKien { get; set; } = "DuKienTuongLai"; // QuaHan | SapDenHan | DaCoLenh | DuKienTuongLai
}

public class SyncWorkOrdersResultDto
{
    public int SoLenhMoiSinh { get; set; }
    public List<string> DanhSachMaLenhMoi { get; set; } = [];
}
