using CMMS.Backend.Entities;
using CMMS.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Tests.Fixtures;

public sealed class TestDbContextHandle : IDisposable
{
    public SqliteConnection Connection { get; }
    public CmmsDbContext Db { get; }

    public TestDbContextHandle()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<CmmsDbContext>()
            .UseSqlite(Connection)
            .Options;

        Db = new CmmsDbContext(options);
        Db.Database.EnsureCreated();
        Seed(Db);
    }

    public static void Seed(CmmsDbContext db)
    {
        if (db.VAI_TRO.Any()) return;

        db.VAI_TRO.AddRange(
            new VAI_TRO { VaiTroId = 1, TenVaiTro = "Quản trị hệ thống" },
            new VAI_TRO { VaiTroId = 2, TenVaiTro = "Quản lý nhà máy" },
            new VAI_TRO { VaiTroId = 3, TenVaiTro = "Trưởng bộ phận bảo trì" },
            new VAI_TRO { VaiTroId = 4, TenVaiTro = "Kỹ thuật viên" },
            new VAI_TRO { VaiTroId = 5, TenVaiTro = "Người dùng thiết bị" }
        );

        db.VAI_TRO_QUYEN.AddRange(
            new VAI_TRO_QUYEN { VaiTroId = 1, MaQuyen = MaQuyen.USER_MANAGE },
            new VAI_TRO_QUYEN { VaiTroId = 1, MaQuyen = MaQuyen.ROLE_MANAGE },
            new VAI_TRO_QUYEN { VaiTroId = 1, MaQuyen = MaQuyen.SYSTEM_CONFIG },
            new VAI_TRO_QUYEN { VaiTroId = 1, MaQuyen = MaQuyen.AUDIT_VIEW },
            new VAI_TRO_QUYEN { VaiTroId = 1, MaQuyen = MaQuyen.BACKUP_EXECUTE },
            new VAI_TRO_QUYEN { VaiTroId = 2, MaQuyen = MaQuyen.ACCOUNT_APPROVE },
            new VAI_TRO_QUYEN { VaiTroId = 2, MaQuyen = MaQuyen.STOCK_ADJUST_APPROVE },
            new VAI_TRO_QUYEN { VaiTroId = 2, MaQuyen = MaQuyen.REPORT_VIEW_ALL },
            new VAI_TRO_QUYEN { VaiTroId = 2, MaQuyen = MaQuyen.KPI_MONITOR },
            new VAI_TRO_QUYEN { VaiTroId = 3, MaQuyen = MaQuyen.DEVICE_MANAGE },
            new VAI_TRO_QUYEN { VaiTroId = 3, MaQuyen = MaQuyen.REQUEST_APPROVE },
            new VAI_TRO_QUYEN { VaiTroId = 3, MaQuyen = MaQuyen.WO_CREATE_DISPATCH },
            new VAI_TRO_QUYEN { VaiTroId = 3, MaQuyen = MaQuyen.STOCK_PROPOSAL_CREATE },
            new VAI_TRO_QUYEN { VaiTroId = 3, MaQuyen = MaQuyen.CHECKLIST_MANAGE },
            new VAI_TRO_QUYEN { VaiTroId = 3, MaQuyen = MaQuyen.REPORT_MAINTENANCE_VIEW },
            new VAI_TRO_QUYEN { VaiTroId = 4, MaQuyen = MaQuyen.WO_EXECUTE },
            new VAI_TRO_QUYEN { VaiTroId = 4, MaQuyen = MaQuyen.CHECKLIST_RECORD },
            new VAI_TRO_QUYEN { VaiTroId = 4, MaQuyen = MaQuyen.MATERIAL_DECLARE },
            new VAI_TRO_QUYEN { VaiTroId = 4, MaQuyen = MaQuyen.INCIDENT_REPORT_CREATE },
            new VAI_TRO_QUYEN { VaiTroId = 5, MaQuyen = MaQuyen.REQUEST_CREATE },
            new VAI_TRO_QUYEN { VaiTroId = 5, MaQuyen = MaQuyen.WO_ACCEPTANCE_SIGN },
            new VAI_TRO_QUYEN { VaiTroId = 5, MaQuyen = MaQuyen.DEVICE_STATUS_VIEW }
        );

        db.THU_VIEN_BIEU_MAU.AddRange(
            new THU_VIEN_BIEU_MAU { BieuMauId = "BM-01", TenBieuMau = "Sổ Lý Lịch Thiết Bị", LoaiBieuMau = "LyLichThietBi", DinhDangTep = "DOCX", DuongDanTep = "/bieu-mau/BM-01_So_Ly_Lich_Thiet_Bi.docx" },
            new THU_VIEN_BIEU_MAU { BieuMauId = "BM-02", TenBieuMau = "Phiếu Báo Hỏng", LoaiBieuMau = "BaoHong", DinhDangTep = "DOCX", DuongDanTep = "/bieu-mau/BM-02_Phieu_Bao_Hong.docx" },
            new THU_VIEN_BIEU_MAU { BieuMauId = "BM-03", TenBieuMau = "Mẫu Checklist", LoaiBieuMau = "Checklist", DinhDangTep = "PDF", DuongDanTep = "/bieu-mau/BM-03_Checklist_Bao_Tri.pdf" },
            new THU_VIEN_BIEU_MAU { BieuMauId = "BM-04", TenBieuMau = "Lệnh Bảo Trì", LoaiBieuMau = "LenhSuaChua", DinhDangTep = "DOCX", DuongDanTep = "/bieu-mau/BM-04_Lenh_Bao_Tri.docx" },
            new THU_VIEN_BIEU_MAU { BieuMauId = "BM-05", TenBieuMau = "Dự Trù Xuất Dùng", LoaiBieuMau = "DuTruXuatDung", DinhDangTep = "DOCX", DuongDanTep = "/bieu-mau/BM-05_Du_Tru_Xuat_Dung.docx" },
            new THU_VIEN_BIEU_MAU { BieuMauId = "BM-06", TenBieuMau = "Biên Bản Nghiệm Thu", LoaiBieuMau = "NghiemThuBanGiao", DinhDangTep = "PDF", DuongDanTep = "/bieu-mau/BM-06_Bien_Ban_Nghiem_Thu.pdf" }
        );

        db.PHAN_XUONG_BO_PHAN.AddRange(
            new PHAN_XUONG_BO_PHAN { DonViId = "PX-COKHI", TenDonVi = "Phân xưởng Cơ khí", LoaiDonVi = "PhanXuong" },
            new PHAN_XUONG_BO_PHAN { DonViId = "DC-CNC-01", TenDonVi = "Dây chuyền CNC 01", DonViChaId = "PX-COKHI", LoaiDonVi = "DayChuyen" },
            new PHAN_XUONG_BO_PHAN { DonViId = "BP-BAOTRI", TenDonVi = "Bộ phận Bảo trì", LoaiDonVi = "BoPhanQuanLy" },
            new PHAN_XUONG_BO_PHAN { DonViId = "BP-QUANLY", TenDonVi = "Ban Giám đốc", LoaiDonVi = "BoPhanQuanLy" }
        );

        db.NHAN_VIEN.AddRange(
            new NHAN_VIEN { NhanVienId = "NV020", HoTen = "Đinh Xuân Hùng", SoDienThoai = "0900000020", Email = "hung@cmms.vn", DonViId = "BP-QUANLY", ChucVu = "Admin IT", CaLamViec = "HanhChinh", TrangThaiSanSang = "SanSang", TrangThai = "DangLamViec" },
            new NHAN_VIEN { NhanVienId = "NV001", HoTen = "Nguyễn Văn An", SoDienThoai = "0900000001", Email = "an@cmms.vn", DonViId = "BP-QUANLY", ChucVu = "Giám đốc", CaLamViec = "HanhChinh", TrangThaiSanSang = "SanSang", TrangThai = "DangLamViec" },
            new NHAN_VIEN { NhanVienId = "NV002", HoTen = "Trần Đình Trọng", SoDienThoai = "0900000002", Email = "trong@cmms.vn", DonViId = "BP-BAOTRI", ChucVu = "Trưởng BP Bảo trì", CaLamViec = "HanhChinh", TrangThaiSanSang = "SanSang", TrangThai = "DangLamViec" },
            new NHAN_VIEN { NhanVienId = "NV003", HoTen = "Lê Văn Ba", SoDienThoai = "0900000003", Email = "ba@cmms.vn", DonViId = "BP-BAOTRI", ChucVu = "KTV Cơ khí", ChuyenNganh = "Cơ khí", BacTho = "6/7", CaLamViec = "Ca 1", TrangThaiSanSang = "SanSang", TrangThai = "DangLamViec" },
            new NHAN_VIEN { NhanVienId = "NV013", HoTen = "Vũ Quốc Cường", SoDienThoai = "0900000013", Email = "cuong@cmms.vn", DonViId = "DC-CNC-01", ChucVu = "Công nhân CNC", CaLamViec = "Ca 1", TrangThaiSanSang = "SanSang", TrangThai = "DangLamViec" }
        );

        const string placeholderHash = "$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9";
        db.TAI_KHOAN.AddRange(
            new TAI_KHOAN { NhanVienId = "NV020", TenDangNhap = "admin.hung", MatKhauHash = placeholderHash, VaiTroId = 1, TrangThaiTaiKhoan = "DangHoatDong", NgayTao = DateTime.Now },
            new TAI_KHOAN { NhanVienId = "NV001", TenDangNhap = "manager.an", MatKhauHash = placeholderHash, VaiTroId = 2, TrangThaiTaiKhoan = "DangHoatDong", NgayTao = DateTime.Now },
            new TAI_KHOAN { NhanVienId = "NV002", TenDangNhap = "lead.trong", MatKhauHash = placeholderHash, VaiTroId = 3, TrangThaiTaiKhoan = "DangHoatDong", NgayTao = DateTime.Now },
            new TAI_KHOAN { NhanVienId = "NV003", TenDangNhap = "ktv.bale", MatKhauHash = placeholderHash, VaiTroId = 4, TrangThaiTaiKhoan = "DangHoatDong", NgayTao = DateTime.Now },
            new TAI_KHOAN { NhanVienId = "NV013", TenDangNhap = "op.cuong", MatKhauHash = placeholderHash, VaiTroId = 5, TrangThaiTaiKhoan = "DangHoatDong", NgayTao = DateTime.Now }
        );

        db.NHOM_THIET_BI.AddRange(
            new NHOM_THIET_BI { NhomThietBiId = "NHOM-TIEN-CNC", TenNhom = "Máy Tiện CNC", LaCumChiTiet = false },
            new NHOM_THIET_BI { NhomThietBiId = "NHOM-PHAY-CNC", TenNhom = "Máy Phay CNC", LaCumChiTiet = false },
            new NHOM_THIET_BI { NhomThietBiId = "NHOM-MAY-EP", TenNhom = "Máy Ép Thủy lực", LaCumChiTiet = false },
            new NHOM_THIET_BI { NhomThietBiId = "NHOM-NEN-KHI", TenNhom = "Máy Nén Khí", LaCumChiTiet = false },
            new NHOM_THIET_BI { NhomThietBiId = "CUM-SPINDLE", TenNhom = "Cụm Trục Chính", LaCumChiTiet = true }
        );

        db.DANH_MUC_MA_LOI.AddRange(
            new DANH_MUC_MA_LOI { MaLoiId = "LOI-QUA-NHIET", TenLoaiSuCo = "Quá nhiệt trục chính", MoTaDauHieu = "Nhiệt độ > 65 độ C" },
            new DANH_MUC_MA_LOI { MaLoiId = "LOI-RO-RI-DAU", TenLoaiSuCo = "Rò rỉ dầu", MoTaDauHieu = "Có dầu loang sàn máy" }
        );

        db.CAU_HINH_MUC_UU_TIEN.AddRange(
            new CAU_HINH_MUC_UU_TIEN { MucUuTienId = 1, TenMucUuTien = "Khẩn cấp", ThuTuUuTien = 1, MaMauHienThi = "#FF0000", ThoiHanPhanHoiPhut = 30 },
            new CAU_HINH_MUC_UU_TIEN { MucUuTienId = 2, TenMucUuTien = "Cao", ThuTuUuTien = 2, MaMauHienThi = "#FFA500", ThoiHanPhanHoiPhut = 120 },
            new CAU_HINH_MUC_UU_TIEN { MucUuTienId = 3, TenMucUuTien = "Trung bình", ThuTuUuTien = 3, MaMauHienThi = "#FFFF00", ThoiHanPhanHoiPhut = 480 },
            new CAU_HINH_MUC_UU_TIEN { MucUuTienId = 4, TenMucUuTien = "Thấp", ThuTuUuTien = 4, MaMauHienThi = "#008000", ThoiHanPhanHoiPhut = 1440 }
        );

        db.THIET_BI.AddRange(
            new THIET_BI
            {
                ThietBiId = "TB-CNC-01", TenThietBi = "Máy Tiện CNC Doosan", NhomThietBiId = "NHOM-TIEN-CNC",
                ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "Lynx 220", Serial = "SN-001",
                NhaSanXuat = "Doosan", NgayDuaVaoSuDung = new DateOnly(2023, 1, 15), ChuKyBaoTriKhuyenNghiNgay = 30,
                CongDungMoTa = "Tiện trục chính xác", TrangThaiVanHanh = "DangHoatDong",
                DuongDanAnh = "/anh-may/tb-cnc-01.jpg", DuongDanTaiLieu = "/tai-lieu/Lynx220_Manual.pdf"
            },
            new THIET_BI
            {
                ThietBiId = "CUM-SP-01", TenThietBi = "Cụm Spindle Doosan", ThietBiChaId = "TB-CNC-01", NhomThietBiId = "CUM-SPINDLE",
                ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "SP-6K", Serial = "SN-SP-001",
                NhaSanXuat = "Doosan", NgayDuaVaoSuDung = new DateOnly(2023, 1, 15), ChuKyBaoTriKhuyenNghiNgay = 30,
                CongDungMoTa = "Trục chính tốc độ cao", TrangThaiVanHanh = "DangHoatDong"
            }
        );

        db.CHUYEN_MON_KTV.Add(new CHUYEN_MON_KTV { NhanVienId = "NV003", NhomThietBiId = "NHOM-TIEN-CNC", GhiChu = "Chính" });

        db.MAU_CHECKLIST.Add(new MAU_CHECKLIST
        {
            MauChecklistId = "CK-CNC-01", TenMau = "Checklist CNC Tiêu chuẩn", NhomThietBiId = "NHOM-TIEN-CNC", TrangThaiHieuLuc = "DangApDung",
            HangMucs =
            [
                new HANG_MUC_CHECKLIST { NhomBoPhan = "Trục chính", NoiDungCongViec = "Đo nhiệt độ", KieuKetQua = "NhapSoDo", TieuChuanYeuCau = "40-80 độ C", GiaTriMin = 40, GiaTriMax = 80, DonViDo = "C", ThuTuThucHien = 1 },
                new HANG_MUC_CHECKLIST { NhomBoPhan = "Bôi trơn", NoiDungCongViec = "Kiểm tra mức dầu", KieuKetQua = "Dat_KhongDat", TieuChuanYeuCau = "Đủ vạch", ThuTuThucHien = 2 }
            ]
        });

        db.PHU_TUNG.AddRange(
            new PHU_TUNG { PhuTungId = "PT-VB-6205", TenPhuTung = "Vòng bi NSK 6205", LoaiPhuTung = "CoKhi", QuyCach = "25x52x15", DonViTinh = "Cái", SoLuongTonHienCo = 10, SoLuongDangDuTru = 0, MucTonToiThieu = 7 },
            new PHU_TUNG { PhuTungId = "PT-DAU-VG68", TenPhuTung = "Dầu rãnh trượt VG68", LoaiPhuTung = "TieuHao", QuyCach = "Can 20L", DonViTinh = "Lít", SoLuongTonHienCo = 50, SoLuongDangDuTru = 0, MucTonToiThieu = 10 }
        );

        db.DINH_MUC_LINH_KIEN.Add(new DINH_MUC_LINH_KIEN
        {
            ThietBiId = "TB-CNC-01", PhuTungId = "PT-VB-6205", SoLuongDinhMuc = 2, ThayTheDinhKy = true, ViTriLapDat = "Đầu trục"
        });

        db.SaveChanges();
    }

    public void Dispose()
    {
        Db.Dispose();
        Connection.Dispose();
    }
}
