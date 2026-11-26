namespace CMMS.Shared.Enums;

/// <summary>
/// Hằng số chuẩn khớp với toàn bộ ràng buộc CHECK trong create-database.sql.
/// </summary>
public static class CmmsConstants
{
    public static class VaiTroIds
    {
        public const int QuanTriHeThong = 1;
        public const int QuanLyNhaMay = 2;
        public const int TruongBoPhanBaoTri = 3;
        public const int KyThuatVien = 4;
        public const int NguoiDungThietBi = 5;
    }

    public static class TrangThaiNhanVien
    {
        public const string DangLamViec = "DangLamViec";
        public const string DaNghiViec = "DaNghiViec";
    }

    public static class CaLamViec
    {
        public const string Ca1 = "Ca 1";
        public const string Ca2 = "Ca 2";
        public const string Ca3 = "Ca 3";
        public const string HanhChinh = "HanhChinh";
    }

    public static class TrangThaiSanSang
    {
        public const string SanSang = "SanSang";
        public const string DangBan = "DangBan";
        public const string NghiPhep = "NghiPhep";
    }

    public static class TrangThaiTaiKhoan
    {
        public const string ChoDuyet = "ChoDuyet";
        public const string DangHoatDong = "DangHoatDong";
        public const string TuChoi = "TuChoi";
        public const string DangKhoa = "DangKhoa";
    }

    public static class LoaiDonVi
    {
        public const string PhanXuong = "PhanXuong";
        public const string DayChuyen = "DayChuyen";
        public const string BoPhanQuanLy = "BoPhanQuanLy";
    }

    public static class TrangThaiThietBi
    {
        public const string DangHoatDong = "DangHoatDong";
        public const string DungDoSuCo = "DungDoSuCo";
        public const string DangBaoTri = "DangBaoTri";
        public const string DuPhong = "DuPhong";
        public const string NgungSuDung = "NgungSuDung";
    }

    public static class TrangThaiYeuCau
    {
        public const string ChoTiepNhan = "ChoTiepNhan";
        public const string DaPheDuyet = "DaPheDuyet";
        public const string TuChoi = "TuChoi";
        public const string DangXuLy = "DangXuLy";
        public const string HoanThanh = "HoanThanh";
        public const string DaHuy = "DaHuy";
    }

    public static class HuongXuLy
    {
        public const string SuaChuaKhanCap = "SuaChuaKhanCap";
        public const string SuaChuaTheoKeHoach = "SuaChuaTheoKeHoach";
    }

    public static class TrangThaiHieuLuc
    {
        public const string DangApDung = "DangApDung";
        public const string NgungApDung = "NgungApDung";
    }

    public static class KieuKetQua
    {
        public const string DatKhongDat = "Dat_KhongDat";
        public const string NhapSoDo = "NhapSoDo";
    }

    public static class DonViChuKy
    {
        public const string Ngay = "Ngay";
        public const string Tuan = "Tuan";
        public const string Thang = "Thang";
        public const string Quy = "Quy";
        public const string Nam = "Nam";
    }

    public static class TrangThaiKeHoach
    {
        public const string DangApDung = "DangApDung";
        public const string TamDung = "TamDung";
        public const string NgungApDung = "NgungApDung";
    }

    public static class LoaiLenh
    {
        public const string BaoTriSuCo = "BaoTriSuCo";
        public const string BaoTriPhongNgua = "BaoTriPhongNgua";
    }

    public static class TrangThaiLenh
    {
        public const string MoiTao = "MoiTao";
        public const string DaPhanCong = "DaPhanCong";
        public const string DangThucHien = "DangThucHien";
        public const string TamDung = "TamDung";
        public const string DaHoanThanh = "DaHoanThanh";
        public const string DaNghiemThu = "DaNghiemThu";
        public const string DaDong = "DaDong";
        public const string DaHuy = "DaHuy";
    }

    public static class TinhTrangSauBaoTri
    {
        public const string DatChuan = "DatChuan";
        public const string CanTheoDoi = "CanTheoDoi";
    }

    public static class VaiTroTrongLenh
    {
        public const string ThoChinh = "ThoChinh";
        public const string ThoPhu = "ThoPhu";
    }

    public static class TrangThaiPhanCong
    {
        public const string DaGiao = "DaGiao";
        public const string DangLam = "DangLam";
        public const string HoanThanh = "HoanThanh";
        public const string HuyDieuChuyen = "HuyDieuChuyen";
    }

    public static class KetLuanChecklist
    {
        public const string Dat = "Dat";
        public const string KhongDat = "KhongDat";
    }

    public static class LoaiPhuTung
    {
        public const string CoKhi = "CoKhi";
        public const string Dien = "Dien";
        public const string TieuHao = "TieuHao";
        public const string DungCu = "DungCu";
    }

    public static class LoaiCapNhatKho
    {
        public const string BoSungTuKhoNgoai = "BoSungTuKhoNgoai";
        public const string DieuChinhKiemKe = "DieuChinhKiemKe";
    }

    public static class TrangThaiCapNhatKho
    {
        public const string ChoXacNhan = "ChoXacNhan";
        public const string DaXacNhan = "DaXacNhan";
        public const string TuChoi = "TuChoi";
    }

    public static class TrangThaiPhieuXuat
    {
        public const string DangDuTru = "DangDuTru";
        public const string DaKhaiBaoThucDung = "DaKhaiBaoThucDung";
        public const string ChoDuyetVuotDuTru = "ChoDuyetVuotDuTru";
        public const string DaXacNhanTruTon = "DaXacNhanTruTon";
        public const string DaHuy = "DaHuy";
    }

    public static class LoaiThaoTacLog
    {
        public const string DangNhap = "DangNhap";
        public const string DangNhapThatBai = "DangNhapThatBai";
        public const string DangXuat = "DangXuat";
        public const string DoiMatKhau = "DoiMatKhau";
        public const string DeXuatTaiKhoan = "DeXuatTaiKhoan";
        public const string PheDuyetTaiKhoan = "PheDuyetTaiKhoan";
        public const string TuChoiTaiKhoan = "TuChoiTaiKhoan";
        public const string KhoaTaiKhoan = "KhoaTaiKhoan";
        public const string MoKhoaTaiKhoan = "MoKhoaTaiKhoan";
        public const string ThayDoiPhanQuyen = "ThayDoiPhanQuyen";
        public const string GanVaiTroTaiKhoan = "GanVaiTroTaiKhoan";
        public const string XacNhanBoSungTonKho = "XacNhanBoSungTonKho";
        public const string DieuChinhKiemKe = "DieuChinhKiemKe";
        public const string TuChoiCapNhatTonKho = "TuChoiCapNhatTonKho";
        public const string XacNhanXuatDung = "XacNhanXuatDung";
        public const string PhatLenhBaoTri = "PhatLenhBaoTri";
        public const string HuyLenhBaoTri = "HuyLenhBaoTri";
        public const string DuyetDongLenh = "DuyetDongLenh";
        public const string SaoLuuDuLieu = "SaoLuuDuLieu";
    }
}
