-- ============================================================
-- DDL SCRIPT: TẠO CƠ SỞ DỮ LIỆU & BẢNG VẬT LÝ HỆ THỐNG CMMS
-- Hệ quản trị: MySQL Server 8.0+
-- Storage Engine: InnoDB | Charset: utf8mb4 | Collation: utf8mb4_unicode_ci
-- Quy chuẩn: 27 bảng đạt chuẩn 3NF, tách toàn bộ quan hệ M-N
-- ============================================================

-- Thiết lập môi trường thực thi an toàn
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 1. Khởi tạo Cơ sở Dữ liệu
CREATE DATABASE IF NOT EXISTS `cmms_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `cmms_db`;

-- ============================================================
-- PHÂN HỆ 1: HỆ THỐNG & TỔ CHỨC (7 BẢNG)
-- ============================================================

-- Bảng 1: VAI_TRO (Danh mục vai trò)
DROP TABLE IF EXISTS `VAI_TRO`;
CREATE TABLE `VAI_TRO` (
    `VaiTroId` INT NOT NULL,
    `TenVaiTro` VARCHAR(50) NOT NULL,
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`VaiTroId`),
    UNIQUE KEY `UQ_VaiTro_Ten` (`TenVaiTro`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục vai trò người dùng trong hệ thống';

-- Bảng 2: VAI_TRO_QUYEN (Bảng trung gian M-N giữa Vai trò và Mã quyền Enum)
DROP TABLE IF EXISTS `VAI_TRO_QUYEN`;
CREATE TABLE `VAI_TRO_QUYEN` (
    `VaiTroId` INT NOT NULL,
    `MaQuyen` VARCHAR(50) NOT NULL,
    PRIMARY KEY (`VaiTroId`, `MaQuyen`),
    CONSTRAINT `FK_VaiTroQuyen_VaiTro` FOREIGN KEY (`VaiTroId`) 
        REFERENCES `VAI_TRO` (`VaiTroId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phân quyền chức năng theo vai trò';

-- Bảng 3: THU_VIEN_BIEU_MAU (Quản lý 6 biểu mẫu quy trình BM-01 đến BM-06)
DROP TABLE IF EXISTS `THU_VIEN_BIEU_MAU`;
CREATE TABLE `THU_VIEN_BIEU_MAU` (
    `BieuMauId` VARCHAR(30) NOT NULL,
    `TenBieuMau` VARCHAR(150) NOT NULL,
    `LoaiBieuMau` VARCHAR(50) NOT NULL,
    `DinhDangTep` VARCHAR(10) NOT NULL,
    `DuongDanTep` VARCHAR(255) NOT NULL,
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`BieuMauId`),
    CONSTRAINT `CHK_BieuMau_Loai` CHECK (`LoaiBieuMau` IN ('LyLichThietBi', 'BaoHong', 'Checklist', 'LenhSuaChua', 'DuTruXuatDung', 'NghiemThuBanGiao')),
    CONSTRAINT `CHK_BieuMau_DinhDang` CHECK (`DinhDangTep` IN ('DOCX', 'PDF'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Thư viện biểu mẫu quy trình bảo trì';

-- Bảng 4: PHAN_XUONG_BO_PHAN (Phân xưởng/bộ phận/dây chuyền đệ quy)
DROP TABLE IF EXISTS `PHAN_XUONG_BO_PHAN`;
CREATE TABLE `PHAN_XUONG_BO_PHAN` (
    `DonViId` VARCHAR(30) NOT NULL,
    `TenDonVi` VARCHAR(100) NOT NULL,
    `DonViChaId` VARCHAR(30) NULL,
    `LoaiDonVi` VARCHAR(30) NOT NULL,
    `NguoiPhuTrachId` VARCHAR(20) NULL,
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`DonViId`),
    CONSTRAINT `CHK_PhanXuong_Loai` CHECK (`LoaiDonVi` IN ('PhanXuong', 'DayChuyen', 'BoPhanQuanLy')),
    CONSTRAINT `FK_PhanXuong_Cha` FOREIGN KEY (`DonViChaId`) 
        REFERENCES `PHAN_XUONG_BO_PHAN` (`DonViId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Cơ cấu tổ chức và vị trí phân xưởng';

-- Bảng 5: NHAN_VIEN (Nhân sự nhà máy & KTV bảo trì)
DROP TABLE IF EXISTS `NHAN_VIEN`;
CREATE TABLE `NHAN_VIEN` (
    `NhanVienId` VARCHAR(20) NOT NULL,
    `HoTen` VARCHAR(100) NOT NULL,
    `SoDienThoai` VARCHAR(15) NOT NULL,
    `Email` VARCHAR(100) NOT NULL,
    `DonViId` VARCHAR(30) NOT NULL,
    `ChucVu` VARCHAR(50) NOT NULL,
    `ChuyenNganh` VARCHAR(50) NULL,
    `BacTho` VARCHAR(20) NULL,
    `CaLamViec` VARCHAR(20) NULL,
    `TrangThaiSanSang` VARCHAR(20) NULL,
    `TrangThai` VARCHAR(20) NOT NULL DEFAULT 'DangLamViec',
    PRIMARY KEY (`NhanVienId`),
    UNIQUE KEY `UQ_NhanVien_Email` (`Email`),
    CONSTRAINT `CHK_NhanVien_CaLamViec` CHECK (`CaLamViec` IS NULL OR `CaLamViec` IN ('Ca 1', 'Ca 2', 'Ca 3', 'HanhChinh')),
    CONSTRAINT `CHK_NhanVien_TrangThaiSanSang` CHECK (`TrangThaiSanSang` IS NULL OR `TrangThaiSanSang` IN ('SanSang', 'DangBan', 'NghiPhep')),
    CONSTRAINT `CHK_NhanVien_TrangThai` CHECK (`TrangThai` IN ('DangLamViec', 'DaNghiViec')),
    CONSTRAINT `FK_NhanVien_DonVi` FOREIGN KEY (`DonViId`) 
        REFERENCES `PHAN_XUONG_BO_PHAN` (`DonViId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ nhân viên và kỹ thuật viên';

-- Thêm khóa ngoại người phụ trách cho PHAN_XUONG_BO_PHAN sau khi NHAN_VIEN đã tạo
ALTER TABLE `PHAN_XUONG_BO_PHAN` 
    ADD CONSTRAINT `FK_PhanXuong_NguoiPhuTrach` FOREIGN KEY (`NguoiPhuTrachId`) 
    REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE;

-- Bảng 6: TAI_KHOAN (Tài khoản người dùng hệ thống)
DROP TABLE IF EXISTS `TAI_KHOAN`;
CREATE TABLE `TAI_KHOAN` (
    `TaiKhoanId` INT NOT NULL AUTO_INCREMENT,
    `NhanVienId` VARCHAR(20) NOT NULL,
    `TenDangNhap` VARCHAR(50) NOT NULL,
    `MatKhauHash` VARCHAR(255) NOT NULL,
    `VaiTroId` INT NOT NULL,
    `TrangThaiTaiKhoan` VARCHAR(20) NOT NULL DEFAULT 'ChoDuyet',
    `SoLanDangNhapSai` INT NOT NULL DEFAULT 0,
    `ThoiDiemMoKhoaTam` DATETIME NULL,
    `NgayTao` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `NguoiDuyetId` VARCHAR(20) NULL,
    PRIMARY KEY (`TaiKhoanId`),
    UNIQUE KEY `UQ_TaiKhoan_NhanVien` (`NhanVienId`),
    UNIQUE KEY `UQ_TaiKhoan_TenDangNhap` (`TenDangNhap`),
    CONSTRAINT `CHK_TaiKhoan_TrangThai` CHECK (`TrangThaiTaiKhoan` IN ('ChoDuyet', 'DangHoatDong', 'TuChoi', 'DangKhoa')),
    CONSTRAINT `FK_TaiKhoan_NhanVien` FOREIGN KEY (`NhanVienId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_TaiKhoan_VaiTro` FOREIGN KEY (`VaiTroId`) 
        REFERENCES `VAI_TRO` (`VaiTroId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_TaiKhoan_NguoiDuyet` FOREIGN KEY (`NguoiDuyetId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tài khoản đăng nhập và bảo mật';

-- Bảng 7: NHAT_KY_HE_THONG (Nhật ký kiểm toán hệ thống)
DROP TABLE IF EXISTS `NHAT_KY_HE_THONG`;
CREATE TABLE `NHAT_KY_HE_THONG` (
    `NhatKyId` BIGINT NOT NULL AUTO_INCREMENT,
    `TaiKhoanId` INT NULL,
    `ThoiDiem` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `LoaiThaoTac` VARCHAR(50) NOT NULL,
    `DoiTuongTacDong` VARCHAR(100) NULL,
    `MoTaChiTiet` TEXT NOT NULL,
    PRIMARY KEY (`NhatKyId`),
    KEY `IDX_NhatKy_ThoiDiem` (`ThoiDiem`),
    CONSTRAINT `FK_NhatKy_TaiKhoan` FOREIGN KEY (`TaiKhoanId`) 
        REFERENCES `TAI_KHOAN` (`TaiKhoanId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký thao tác hệ thống';


-- ============================================================
-- PHÂN HỆ 2: TÀI SẢN THIẾT BỊ (4 BẢNG)
-- ============================================================

-- Bảng 8: NHOM_THIET_BI (Danh mục nhóm máy và cụm chi tiết con)
DROP TABLE IF EXISTS `NHOM_THIET_BI`;
CREATE TABLE `NHOM_THIET_BI` (
    `NhomThietBiId` VARCHAR(30) NOT NULL,
    `TenNhom` VARCHAR(100) NOT NULL,
    `LaCumChiTiet` TINYINT(1) NOT NULL DEFAULT 0,
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`NhomThietBiId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục nhóm thiết bị và cụm con';

-- Bảng 9: DANH_MUC_MA_LOI (Danh mục mã sự cố chuẩn hóa)
DROP TABLE IF EXISTS `DANH_MUC_MA_LOI`;
CREATE TABLE `DANH_MUC_MA_LOI` (
    `MaLoiId` VARCHAR(20) NOT NULL,
    `TenLoaiSuCo` VARCHAR(100) NOT NULL,
    `MoTaDauHieu` VARCHAR(255) NOT NULL,
    PRIMARY KEY (`MaLoiId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục mã lỗi sự cố chuẩn hóa';

-- Bảng 10: THIET_BI (Hồ sơ lý lịch tài sản thiết bị)
DROP TABLE IF EXISTS `THIET_BI`;
CREATE TABLE `THIET_BI` (
    `ThietBiId` VARCHAR(30) NOT NULL,
    `TenThietBi` VARCHAR(150) NOT NULL,
    `ThietBiChaId` VARCHAR(30) NULL,
    `NhomThietBiId` VARCHAR(30) NOT NULL,
    `ViTriLapDatId` VARCHAR(30) NOT NULL,
    `BoPhanQuanLyId` VARCHAR(30) NOT NULL,
    `Model` VARCHAR(50) NOT NULL,
    `Serial` VARCHAR(50) NOT NULL,
    `NhaSanXuat` VARCHAR(100) NOT NULL,
    `NgayDuaVaoSuDung` DATE NOT NULL,
    `ChuKyBaoTriKhuyenNghiNgay` INT NOT NULL DEFAULT 30,
    `CongDungMoTa` TEXT NOT NULL,
    `LuuYAnToan` TEXT NULL,
    `TrangThaiVanHanh` VARCHAR(30) NOT NULL DEFAULT 'DangHoatDong',
    `DuongDanAnh` VARCHAR(255) NULL,
    `DuongDanTaiLieu` VARCHAR(255) NULL,
    PRIMARY KEY (`ThietBiId`),
    UNIQUE KEY `UQ_ThietBi_Serial` (`Serial`),
    CONSTRAINT `CHK_ThietBi_TrangThai` CHECK (`TrangThaiVanHanh` IN ('DangHoatDong', 'DungDoSuCo', 'DangBaoTri', 'DuPhong', 'NgungSuDung')),
    CONSTRAINT `FK_ThietBi_Cha` FOREIGN KEY (`ThietBiChaId`) 
        REFERENCES `THIET_BI` (`ThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_ThietBi_Nhom` FOREIGN KEY (`NhomThietBiId`) 
        REFERENCES `NHOM_THIET_BI` (`NhomThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_ThietBi_ViTri` FOREIGN KEY (`ViTriLapDatId`) 
        REFERENCES `PHAN_XUONG_BO_PHAN` (`DonViId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_ThietBi_BoPhan` FOREIGN KEY (`BoPhanQuanLyId`) 
        REFERENCES `PHAN_XUONG_BO_PHAN` (`DonViId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ lý lịch thiết bị máy móc';

-- Bảng 11: THONG_SO_THIET_BI (Thông số kỹ thuật động - Form Master-Detail 2)
DROP TABLE IF EXISTS `THONG_SO_THIET_BI`;
CREATE TABLE `THONG_SO_THIET_BI` (
    `ThongSoId` BIGINT NOT NULL AUTO_INCREMENT,
    `ThietBiId` VARCHAR(30) NOT NULL,
    `TenThongSo` VARCHAR(100) NOT NULL,
    `GiaTri` VARCHAR(100) NOT NULL,
    `DonViDo` VARCHAR(30) NULL,
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`ThongSoId`),
    KEY `IDX_ThongSo_ThietBi` (`ThietBiId`),
    CONSTRAINT `FK_ThongSo_ThietBi` FOREIGN KEY (`ThietBiId`) 
        REFERENCES `THIET_BI` (`ThietBiId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Thông số kỹ thuật động của thiết bị';


-- ============================================================
-- PHÂN HỆ 3: BẢO TRÌ SỰ CỐ (2 BẢNG)
-- ============================================================

-- Bảng 12: CAU_HINH_MUC_UU_TIEN (Cấu hình SLA và mức ưu tiên)
DROP TABLE IF EXISTS `CAU_HINH_MUC_UU_TIEN`;
CREATE TABLE `CAU_HINH_MUC_UU_TIEN` (
    `MucUuTienId` INT NOT NULL,
    `TenMucUuTien` VARCHAR(30) NOT NULL,
    `ThuTuUuTien` INT NOT NULL,
    `MaMauHienThi` VARCHAR(20) NOT NULL,
    `ThoiHanPhanHoiPhut` INT NOT NULL,
    PRIMARY KEY (`MucUuTienId`),
    UNIQUE KEY `UQ_MucUuTien_Ten` (`TenMucUuTien`),
    UNIQUE KEY `UQ_MucUuTien_ThuTu` (`ThuTuUuTien`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Cấu hình cam kết dịch vụ SLA và mức ưu tiên';

-- Bảng 13: YEU_CAU_BAO_TRI (Phiếu yêu cầu báo hỏng sự cố BM-02)
DROP TABLE IF EXISTS `YEU_CAU_BAO_TRI`;
CREATE TABLE `YEU_CAU_BAO_TRI` (
    `YeuCauId` VARCHAR(30) NOT NULL,
    `ThietBiId` VARCHAR(30) NOT NULL,
    `NguoiBaoId` VARCHAR(20) NOT NULL,
    `ThoiDiemGui` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `MoTaTrieuChung` TEXT NOT NULL,
    `DuongDanAnhSuCo` VARCHAR(255) NULL,
    `MayDangDung` TINYINT(1) NOT NULL DEFAULT 0,
    `MucUuTienDeXuatId` INT NOT NULL,
    `MucUuTienChinhThucId` INT NULL,
    `HanPhanHoi` DATETIME NOT NULL,
    `TrangThaiYeuCau` VARCHAR(30) NOT NULL DEFAULT 'ChoTiepNhan',
    `MaLoiId` VARCHAR(20) NULL,
    `NhanDinhNguyenNhanSoBo` TEXT NULL,
    `HuongXuLy` VARCHAR(50) NULL,
    `NguoiDuyetId` VARCHAR(20) NULL,
    `ThoiDiemDuyet` DATETIME NULL,
    `LyDoTuChoi` VARCHAR(255) NULL,
    PRIMARY KEY (`YeuCauId`),
    KEY `IDX_YeuCau_TrangThai_ThoiDiem` (`TrangThaiYeuCau`, `ThoiDiemGui`),
    KEY `IDX_YeuCau_ThietBi` (`ThietBiId`),
    CONSTRAINT `CHK_YeuCau_TrangThai` CHECK (`TrangThaiYeuCau` IN ('ChoTiepNhan', 'DaPheDuyet', 'TuChoi', 'DangXuLy', 'HoanThanh', 'DaHuy')),
    CONSTRAINT `CHK_YeuCau_HuongXuLy` CHECK (`HuongXuLy` IS NULL OR `HuongXuLy` IN ('SuaChuaKhanCap', 'SuaChuaTheoKeHoach')),
    CONSTRAINT `FK_YeuCau_ThietBi` FOREIGN KEY (`ThietBiId`) 
        REFERENCES `THIET_BI` (`ThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_YeuCau_NguoiBao` FOREIGN KEY (`NguoiBaoId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_YeuCau_UuTienDeXuat` FOREIGN KEY (`MucUuTienDeXuatId`) 
        REFERENCES `CAU_HINH_MUC_UU_TIEN` (`MucUuTienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_YeuCau_UuTienChinhThuc` FOREIGN KEY (`MucUuTienChinhThucId`) 
        REFERENCES `CAU_HINH_MUC_UU_TIEN` (`MucUuTienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_YeuCau_MaLoi` FOREIGN KEY (`MaLoiId`) 
        REFERENCES `DANH_MUC_MA_LOI` (`MaLoiId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_YeuCau_NguoiDuyet` FOREIGN KEY (`NguoiDuyetId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phiếu yêu cầu báo hỏng sự cố';


-- ============================================================
-- PHÂN HỆ 4: BẢO TRÌ PHÒNG NGỪA (3 BẢNG)
-- ============================================================

-- Bảng 14: MAU_CHECKLIST (Mẫu quy trình bảo dưỡng BM-03 Master - Form Master-Detail 1)
DROP TABLE IF EXISTS `MAU_CHECKLIST`;
CREATE TABLE `MAU_CHECKLIST` (
    `MauChecklistId` VARCHAR(30) NOT NULL,
    `TenMau` VARCHAR(150) NOT NULL,
    `NhomThietBiId` VARCHAR(30) NOT NULL,
    `MoTaTieuChuan` TEXT NULL,
    `TrangThaiHieuLuc` VARCHAR(20) NOT NULL DEFAULT 'DangApDung',
    PRIMARY KEY (`MauChecklistId`),
    CONSTRAINT `CHK_MauChecklist_TrangThai` CHECK (`TrangThaiHieuLuc` IN ('DangApDung', 'NgungApDung')),
    CONSTRAINT `FK_MauChecklist_Nhom` FOREIGN KEY (`NhomThietBiId`) 
        REFERENCES `NHOM_THIET_BI` (`NhomThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Mẫu checklist tiêu chuẩn bảo trì';

-- Bảng 15: HANG_MUC_CHECKLIST (Các bước kiểm tra BM-03 Detail)
DROP TABLE IF EXISTS `HANG_MUC_CHECKLIST`;
CREATE TABLE `HANG_MUC_CHECKLIST` (
    `HangMucId` INT NOT NULL AUTO_INCREMENT,
    `MauChecklistId` VARCHAR(30) NOT NULL,
    `NhomBoPhan` VARCHAR(100) NOT NULL,
    `NoiDungCongViec` VARCHAR(255) NOT NULL,
    `KieuKetQua` VARCHAR(20) NOT NULL,
    `TieuChuanYeuCau` VARCHAR(255) NOT NULL,
    `GiaTriMin` DECIMAL(12,2) NULL,
    `GiaTriMax` DECIMAL(12,2) NULL,
    `DonViDo` VARCHAR(30) NULL,
    `ThuTuThucHien` INT NOT NULL DEFAULT 1,
    PRIMARY KEY (`HangMucId`),
    KEY `IDX_HangMuc_MauChecklist` (`MauChecklistId`),
    CONSTRAINT `CHK_HangMuc_KieuKetQua` CHECK (`KieuKetQua` IN ('Dat_KhongDat', 'NhapSoDo')),
    CONSTRAINT `FK_HangMuc_MauChecklist` FOREIGN KEY (`MauChecklistId`) 
        REFERENCES `MAU_CHECKLIST` (`MauChecklistId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết các hạng mục kiểm tra checklist';

-- Bảng 16: KE_HOACH_BAO_TRI (Kế hoạch bảo trì định kỳ theo máy)
DROP TABLE IF EXISTS `KE_HOACH_BAO_TRI`;
CREATE TABLE `KE_HOACH_BAO_TRI` (
    `KeHoachId` VARCHAR(30) NOT NULL,
    `TenKeHoach` VARCHAR(150) NOT NULL,
    `ThietBiId` VARCHAR(30) NOT NULL,
    `MauChecklistId` VARCHAR(30) NOT NULL,
    `DonViChuKy` VARCHAR(20) NOT NULL,
    `GiaTriChuKy` INT NOT NULL,
    `SoNgaySinhLenhTruoc` INT NOT NULL DEFAULT 3,
    `ThoiGianDuKienPhut` INT NOT NULL DEFAULT 60,
    `NgayBatDauApDung` DATE NOT NULL,
    `NgayHoanThanhGanNhat` DATE NULL,
    `NgayDenHanTiepTheo` DATE NOT NULL,
    `TrangThaiKeHoach` VARCHAR(30) NOT NULL DEFAULT 'DangApDung',
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`KeHoachId`),
    KEY `IDX_KeHoach_ThietBi` (`ThietBiId`),
    KEY `IDX_KeHoach_DenHan` (`NgayDenHanTiepTheo`, `TrangThaiKeHoach`),
    CONSTRAINT `CHK_KeHoach_DonViChuKy` CHECK (`DonViChuKy` IN ('Ngay', 'Tuan', 'Thang', 'Quy', 'Nam')),
    CONSTRAINT `CHK_KeHoach_TrangThai` CHECK (`TrangThaiKeHoach` IN ('DangApDung', 'TamDung', 'NgungApDung')),
    CONSTRAINT `FK_KeHoach_ThietBi` FOREIGN KEY (`ThietBiId`) 
        REFERENCES `THIET_BI` (`ThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_KeHoach_MauChecklist` FOREIGN KEY (`MauChecklistId`) 
        REFERENCES `MAU_CHECKLIST` (`MauChecklistId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Kế hoạch bảo trì định kỳ theo chu kỳ';


-- ============================================================
-- PHÂN HỆ 5: LỆNH & QUÁ TRÌNH BẢO TRÌ (5 BẢNG)
-- ============================================================

-- Bảng 17: LENH_BAO_TRI (Lệnh bảo trì trung tâm BM-04 & BM-06)
DROP TABLE IF EXISTS `LENH_BAO_TRI`;
CREATE TABLE `LENH_BAO_TRI` (
    `LenhBaoTriId` VARCHAR(30) NOT NULL,
    `ThietBiId` VARCHAR(30) NOT NULL,
    `LoaiLenh` VARCHAR(30) NOT NULL,
    `YeuCauId` VARCHAR(30) NULL,
    `KeHoachId` VARCHAR(30) NULL,
    `NoiDungCongViec` TEXT NOT NULL,
    `MucUuTienId` INT NOT NULL,
    `ThoiGianDuKienBatDau` DATETIME NOT NULL,
    `ThoiGianDuKienKetThuc` DATETIME NOT NULL,
    `ThoiDiemThucTeBatDau` DATETIME NULL,
    `ThoiDiemThucTeKetThuc` DATETIME NULL,
    `LyDoTamDung` VARCHAR(255) NULL,
    `TrangThaiLenh` VARCHAR(30) NOT NULL DEFAULT 'MoiTao',
    `TinhTrangSauBaoTri` VARCHAR(30) NULL,
    `NguoiBanGiaoId` VARCHAR(20) NULL,
    `NguoiKyNhanId` VARCHAR(20) NULL,
    `ThoiDiemNghiemThu` DATETIME NULL,
    `NhanXetNghiemThu` TEXT NULL,
    `NguoiTaoId` VARCHAR(20) NULL,
    `NguoiDuyetDongId` VARCHAR(20) NULL,
    PRIMARY KEY (`LenhBaoTriId`),
    KEY `IDX_LenhBaoTri_ThietBi_ThoiGian` (`ThietBiId`, `ThoiGianDuKienBatDau`),
    KEY `IDX_LenhBaoTri_TrangThai` (`TrangThaiLenh`, `MucUuTienId`),
    CONSTRAINT `CHK_Lenh_LoaiLenh` CHECK (`LoaiLenh` IN ('BaoTriSuCo', 'BaoTriPhongNgua')),
    CONSTRAINT `CHK_Lenh_TrangThai` CHECK (`TrangThaiLenh` IN ('MoiTao', 'DaPhanCong', 'DangThucHien', 'TamDung', 'DaHoanThanh', 'DaNghiemThu', 'DaDong', 'DaHuy')),
    CONSTRAINT `CHK_Lenh_TinhTrang` CHECK (`TinhTrangSauBaoTri` IS NULL OR `TinhTrangSauBaoTri` IN ('DatChuan', 'CanTheoDoi')),
    CONSTRAINT `FK_Lenh_ThietBi` FOREIGN KEY (`ThietBiId`) 
        REFERENCES `THIET_BI` (`ThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_YeuCau` FOREIGN KEY (`YeuCauId`) 
        REFERENCES `YEU_CAU_BAO_TRI` (`YeuCauId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_KeHoach` FOREIGN KEY (`KeHoachId`) 
        REFERENCES `KE_HOACH_BAO_TRI` (`KeHoachId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_UuTien` FOREIGN KEY (`MucUuTienId`) 
        REFERENCES `CAU_HINH_MUC_UU_TIEN` (`MucUuTienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_NguoiBanGiao` FOREIGN KEY (`NguoiBanGiaoId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_NguoiKyNhan` FOREIGN KEY (`NguoiKyNhanId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_NguoiTao` FOREIGN KEY (`NguoiTaoId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_Lenh_NguoiDuyetDong` FOREIGN KEY (`NguoiDuyetDongId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Lệnh bảo trì trung tâm Work Order';

-- Bảng 18: CHUYEN_MON_KTV (Bảng trung gian M-N giữa KTV và Nhóm máy)
DROP TABLE IF EXISTS `CHUYEN_MON_KTV`;
CREATE TABLE `CHUYEN_MON_KTV` (
    `NhanVienId` VARCHAR(20) NOT NULL,
    `NhomThietBiId` VARCHAR(30) NOT NULL,
    `GhiChu` VARCHAR(100) NULL,
    PRIMARY KEY (`NhanVienId`, `NhomThietBiId`),
    CONSTRAINT `FK_ChuyenMon_NhanVien` FOREIGN KEY (`NhanVienId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_ChuyenMon_Nhom` FOREIGN KEY (`NhomThietBiId`) 
        REFERENCES `NHOM_THIET_BI` (`NhomThietBiId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chuyên môn năng lực kỹ thuật viên';

-- Bảng 19: PHAN_CONG_KTV (Bảng trung gian M-N giữa Lệnh bảo trì và KTV - Form Master-Detail 3)
DROP TABLE IF EXISTS `PHAN_CONG_KTV`;
CREATE TABLE `PHAN_CONG_KTV` (
    `PhanCongId` BIGINT NOT NULL AUTO_INCREMENT,
    `LenhBaoTriId` VARCHAR(30) NOT NULL,
    `KyThuatVienId` VARCHAR(20) NOT NULL,
    `NguoiPhanCongId` VARCHAR(20) NOT NULL,
    `ThoiDiemPhanCong` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `KhungGioBatDau` DATETIME NOT NULL,
    `KhungGioKetThuc` DATETIME NOT NULL,
    `VaiTroTrongLenh` VARCHAR(30) NOT NULL DEFAULT 'ThoChinh',
    `SoGioCongThucTe` DECIMAL(6,2) NOT NULL DEFAULT 0.00,
    `TrangThaiPhanCong` VARCHAR(30) NOT NULL DEFAULT 'DaGiao',
    PRIMARY KEY (`PhanCongId`),
    KEY `IDX_PhanCong_Lenh` (`LenhBaoTriId`),
    KEY `IDX_PhanCong_KTV_KhungGio` (`KyThuatVienId`, `KhungGioBatDau`, `KhungGioKetThuc`),
    CONSTRAINT `CHK_PhanCong_VaiTro` CHECK (`VaiTroTrongLenh` IN ('ThoChinh', 'ThoPhu')),
    CONSTRAINT `CHK_PhanCong_TrangThai` CHECK (`TrangThaiPhanCong` IN ('DaGiao', 'DangLam', 'HoanThanh', 'HuyDieuChuyen')),
    CONSTRAINT `FK_PhanCong_Lenh` FOREIGN KEY (`LenhBaoTriId`) 
        REFERENCES `LENH_BAO_TRI` (`LenhBaoTriId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_PhanCong_KTV` FOREIGN KEY (`KyThuatVienId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_PhanCong_NguoiGiao` FOREIGN KEY (`NguoiPhanCongId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phân công kỹ thuật viên thực hiện lệnh bảo trì';

-- Bảng 20: KET_QUA_CHECKLIST (Bảng trung gian M-N giữa Lệnh bảo trì và Hạng mục checklist)
DROP TABLE IF EXISTS `KET_QUA_CHECKLIST`;
CREATE TABLE `KET_QUA_CHECKLIST` (
    `KetQuaId` BIGINT NOT NULL AUTO_INCREMENT,
    `LenhBaoTriId` VARCHAR(30) NOT NULL,
    `HangMucId` INT NOT NULL,
    `GiaTriDoThucTe` DECIMAL(12,2) NULL,
    `KetLuan` VARCHAR(20) NOT NULL,
    `NguoiKiemTraId` VARCHAR(20) NOT NULL,
    `ThoiDiemKiemTra` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `GhiChu` VARCHAR(255) NULL,
    PRIMARY KEY (`KetQuaId`),
    KEY `IDX_KetQua_Lenh` (`LenhBaoTriId`),
    CONSTRAINT `CHK_KetQua_KetLuan` CHECK (`KetLuan` IN ('Dat', 'KhongDat')),
    CONSTRAINT `FK_KetQua_Lenh` FOREIGN KEY (`LenhBaoTriId`) 
        REFERENCES `LENH_BAO_TRI` (`LenhBaoTriId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_KetQua_HangMuc` FOREIGN KEY (`HangMucId`) 
        REFERENCES `HANG_MUC_CHECKLIST` (`HangMucId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_KetQua_KTV` FOREIGN KEY (`NguoiKiemTraId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Kết quả kiểm tra chi tiết các hạng mục checklist';

-- Bảng 21: BAO_CAO_SU_CO (Báo cáo sự cố chuyên sâu - Quan hệ 1-1 với Lệnh sự cố)
DROP TABLE IF EXISTS `BAO_CAO_SU_CO`;
CREATE TABLE `BAO_CAO_SU_CO` (
    `BaoCaoSuCoId` BIGINT NOT NULL AUTO_INCREMENT,
    `LenhBaoTriId` VARCHAR(30) NOT NULL,
    `MaLoiChinhThucId` VARCHAR(20) NOT NULL,
    `CoDungMay` TINYINT(1) NOT NULL DEFAULT 0,
    `ThoiDiemXayRa` DATETIME NULL,
    `ThoiDiemKhoiPhuc` DATETIME NULL,
    `MoTaHuHong` TEXT NOT NULL,
    `NguyenNhanGocRe` TEXT NOT NULL,
    `MucDoAnhHuongPhanTram` INT NOT NULL DEFAULT 100,
    `BienPhapKhacPhuc` TEXT NOT NULL,
    `NguoiLapBaoCaoId` VARCHAR(20) NOT NULL,
    PRIMARY KEY (`BaoCaoSuCoId`),
    UNIQUE KEY `UQ_BaoCao_Lenh` (`LenhBaoTriId`),
    CONSTRAINT `CHK_BaoCao_AnhHuong` CHECK (`MucDoAnhHuongPhanTram` BETWEEN 0 AND 100),
    CONSTRAINT `FK_BaoCao_Lenh` FOREIGN KEY (`LenhBaoTriId`) 
        REFERENCES `LENH_BAO_TRI` (`LenhBaoTriId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_BaoCao_MaLoi` FOREIGN KEY (`MaLoiChinhThucId`) 
        REFERENCES `DANH_MUC_MA_LOI` (`MaLoiId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_BaoCao_KTV` FOREIGN KEY (`NguoiLapBaoCaoId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Báo cáo phân tích sự cố và dừng máy';


-- ============================================================
-- PHÂN HỆ 6: QUẢN LÝ PHỤ TÙNG & KHO HIỆN VẬT (6 BẢNG)
-- ============================================================

-- Bảng 22: PHU_TUNG (Danh mục phụ tùng và tồn kho vật lý phi tài chính)
DROP TABLE IF EXISTS `PHU_TUNG`;
CREATE TABLE `PHU_TUNG` (
    `PhuTungId` VARCHAR(30) NOT NULL,
    `TenPhuTung` VARCHAR(150) NOT NULL,
    `LoaiPhuTung` VARCHAR(50) NOT NULL,
    `QuyCach` VARCHAR(150) NOT NULL,
    `DonViTinh` VARCHAR(20) NOT NULL,
    `SoLuongTonHienCo` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `SoLuongDangDuTru` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `MucTonToiThieu` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `MoTa` VARCHAR(255) NULL,
    PRIMARY KEY (`PhuTungId`),
    KEY `IDX_PhuTung_CanhBaoTon` (`SoLuongTonHienCo`, `SoLuongDangDuTru`, `MucTonToiThieu`),
    CONSTRAINT `CHK_PhuTung_Loai` CHECK (`LoaiPhuTung` IN ('CoKhi', 'Dien', 'TieuHao', 'DungCu'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục phụ tùng và tồn kho hiện vật';

-- Bảng 23: DINH_MUC_LINH_KIEN (Bảng trung gian M-N giữa Thiết bị và Phụ tùng)
DROP TABLE IF EXISTS `DINH_MUC_LINH_KIEN`;
CREATE TABLE `DINH_MUC_LINH_KIEN` (
    `DinhMucId` BIGINT NOT NULL AUTO_INCREMENT,
    `ThietBiId` VARCHAR(30) NOT NULL,
    `PhuTungId` VARCHAR(30) NOT NULL,
    `SoLuongDinhMuc` DECIMAL(10,2) NOT NULL DEFAULT 1.00,
    `ThayTheDinhKy` TINYINT(1) NOT NULL DEFAULT 0,
    `ViTriLapDat` VARCHAR(100) NULL,
    PRIMARY KEY (`DinhMucId`),
    UNIQUE KEY `UQ_DinhMuc_ThietBi_PhuTung` (`ThietBiId`, `PhuTungId`),
    CONSTRAINT `FK_DinhMuc_ThietBi` FOREIGN KEY (`ThietBiId`) 
        REFERENCES `THIET_BI` (`ThietBiId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_DinhMuc_PhuTung` FOREIGN KEY (`PhuTungId`) 
        REFERENCES `PHU_TUNG` (`PhuTungId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Định mức linh kiện phụ tùng cho thiết bị';

-- Bảng 24: PHIEU_CAP_NHAT_TON_KHO (Phiếu nhập kho ngoài / kiểm kê điều chỉnh)
DROP TABLE IF EXISTS `PHIEU_CAP_NHAT_TON_KHO`;
CREATE TABLE `PHIEU_CAP_NHAT_TON_KHO` (
    `PhieuCapNhatId` VARCHAR(30) NOT NULL,
    `LoaiCapNhat` VARCHAR(30) NOT NULL,
    `MaPhieuKhoNgoai` VARCHAR(50) NULL,
    `ThoiDiemLap` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `NguoiLapId` VARCHAR(20) NOT NULL,
    `NguoiXacNhanId` VARCHAR(20) NULL,
    `ThoiDiemXacNhan` DATETIME NULL,
    `TrangThaiPhieu` VARCHAR(30) NOT NULL DEFAULT 'ChoXacNhan',
    `GhiChu` VARCHAR(255) NULL,
    PRIMARY KEY (`PhieuCapNhatId`),
    CONSTRAINT `CHK_PhieuCapNhat_Loai` CHECK (`LoaiCapNhat` IN ('BoSungTuKhoNgoai', 'DieuChinhKiemKe')),
    CONSTRAINT `CHK_PhieuCapNhat_TrangThai` CHECK (`TrangThaiPhieu` IN ('ChoXacNhan', 'DaXacNhan', 'TuChoi')),
    CONSTRAINT `FK_PhieuCapNhat_NguoiLap` FOREIGN KEY (`NguoiLapId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_PhieuCapNhat_NguoiXacNhan` FOREIGN KEY (`NguoiXacNhanId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phiếu cập nhật tồn kho phi tài chính';

-- Bảng 25: CHI_TIET_CAP_NHAT_TON_KHO (Bảng trung gian M-N giữa Phiếu cập nhật và Phụ tùng)
DROP TABLE IF EXISTS `CHI_TIET_CAP_NHAT_TON_KHO`;
CREATE TABLE `CHI_TIET_CAP_NHAT_TON_KHO` (
    `ChiTietCapNhatId` BIGINT NOT NULL AUTO_INCREMENT,
    `PhieuCapNhatId` VARCHAR(30) NOT NULL,
    `PhuTungId` VARCHAR(30) NOT NULL,
    `SoLuongThayDoi` DECIMAL(10,2) NOT NULL,
    `GhiChu` VARCHAR(255) NULL,
    PRIMARY KEY (`ChiTietCapNhatId`),
    KEY `IDX_ChiTietCapNhat_Phieu` (`PhieuCapNhatId`),
    CONSTRAINT `FK_ChiTietCapNhat_Phieu` FOREIGN KEY (`PhieuCapNhatId`) 
        REFERENCES `PHIEU_CAP_NHAT_TON_KHO` (`PhieuCapNhatId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_ChiTietCapNhat_PhuTung` FOREIGN KEY (`PhuTungId`) 
        REFERENCES `PHU_TUNG` (`PhuTungId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết số lượng phụ tùng biến động kho';

-- Bảng 26: PHIEU_DU_TRU_XUAT_DUNG (Phiếu dự trù và xuất dùng BM-05 Master - Form Master-Detail 4)
DROP TABLE IF EXISTS `PHIEU_DU_TRU_XUAT_DUNG`;
CREATE TABLE `PHIEU_DU_TRU_XUAT_DUNG` (
    `PhieuXuatId` VARCHAR(30) NOT NULL,
    `LenhBaoTriId` VARCHAR(30) NOT NULL,
    `NguoiLapDuTruId` VARCHAR(20) NOT NULL,
    `ThoiDiemLap` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `NguoiKhaiBaoId` VARCHAR(20) NULL,
    `ThoiDiemKhaiBao` DATETIME NULL,
    `NguoiDuyetVuotId` VARCHAR(20) NULL,
    `ThoiDiemDuyetVuot` DATETIME NULL,
    `NguoiXacNhanXuatId` VARCHAR(20) NULL,
    `ThoiDiemXacNhan` DATETIME NULL,
    `TrangThaiPhieu` VARCHAR(30) NOT NULL DEFAULT 'DangDuTru',
    `GhiChu` TEXT NULL,
    PRIMARY KEY (`PhieuXuatId`),
    KEY `IDX_PhieuXuat_Lenh` (`LenhBaoTriId`),
    CONSTRAINT `CHK_PhieuXuat_TrangThai` CHECK (`TrangThaiPhieu` IN ('DangDuTru', 'DaKhaiBaoThucDung', 'ChoDuyetVuotDuTru', 'DaXacNhanTruTon', 'DaHuy')),
    CONSTRAINT `FK_PhieuXuat_Lenh` FOREIGN KEY (`LenhBaoTriId`) 
        REFERENCES `LENH_BAO_TRI` (`LenhBaoTriId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_PhieuXuat_NguoiLap` FOREIGN KEY (`NguoiLapDuTruId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `FK_PhieuXuat_NguoiKhaiBao` FOREIGN KEY (`NguoiKhaiBaoId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_PhieuXuat_NguoiDuyetVuot` FOREIGN KEY (`NguoiDuyetVuotId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `FK_PhieuXuat_NguoiXacNhan` FOREIGN KEY (`NguoiXacNhanXuatId`) 
        REFERENCES `NHAN_VIEN` (`NhanVienId`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phiếu dự trù và xuất dùng phụ tùng bảo trì';

-- Bảng 27: CHI_TIET_PHIEU_XUAT (Bảng trung gian M-N giữa Phiếu xuất dùng và Phụ tùng - BM-05 Detail)
DROP TABLE IF EXISTS `CHI_TIET_PHIEU_XUAT`;
CREATE TABLE `CHI_TIET_PHIEU_XUAT` (
    `ChiTietId` BIGINT NOT NULL AUTO_INCREMENT,
    `PhieuXuatId` VARCHAR(30) NOT NULL,
    `PhuTungId` VARCHAR(30) NOT NULL,
    `SoLuongDuTru` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `SoLuongThucDung` DECIMAL(10,2) NULL,
    `GhiChuGiaiTrinh` VARCHAR(255) NULL,
    PRIMARY KEY (`ChiTietId`),
    KEY `IDX_ChiTietXuat_Phieu` (`PhieuXuatId`),
    CONSTRAINT `FK_ChiTietXuat_Phieu` FOREIGN KEY (`PhieuXuatId`) 
        REFERENCES `PHIEU_DU_TRU_XUAT_DUNG` (`PhieuXuatId`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `FK_ChiTietXuat_PhuTung` FOREIGN KEY (`PhuTungId`) 
        REFERENCES `PHU_TUNG` (`PhuTungId`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết danh mục phụ tùng xuất dùng thực tế';

-- Kích hoạt lại ràng buộc khóa ngoại
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- KẾT THÚC DDL SCRIPT (HOÀN TẤT 27 BẢNG CHUẨN 3NF)
-- ============================================================
