-- ============================================================
-- SEED DATA SCRIPT: DỮ LIỆU KIỂM THỬ THỰC TẾ HỆ THỐNG CMMS
-- Hệ quản trị: MySQL Server 8.0+
-- Số lượng: Hơn 180 bản ghi mẫu thực tế cho 27 bảng
-- Tuân thủ nghiêm ngặt tính toàn vẹn tham chiếu khóa ngoại
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

USE `cmms_db`;

-- Xóa dữ liệu cũ theo thứ tự an toàn
TRUNCATE TABLE `CHI_TIET_PHIEU_XUAT`;
TRUNCATE TABLE `PHIEU_DU_TRU_XUAT_DUNG`;
TRUNCATE TABLE `CHI_TIET_CAP_NHAT_TON_KHO`;
TRUNCATE TABLE `PHIEU_CAP_NHAT_TON_KHO`;
TRUNCATE TABLE `DINH_MUC_LINH_KIEN`;
TRUNCATE TABLE `PHU_TUNG`;
TRUNCATE TABLE `BAO_CAO_SU_CO`;
TRUNCATE TABLE `KET_QUA_CHECKLIST`;
TRUNCATE TABLE `PHAN_CONG_KTV`;
TRUNCATE TABLE `CHUYEN_MON_KTV`;
TRUNCATE TABLE `LENH_BAO_TRI`;
TRUNCATE TABLE `KE_HOACH_BAO_TRI`;
TRUNCATE TABLE `HANG_MUC_CHECKLIST`;
TRUNCATE TABLE `MAU_CHECKLIST`;
TRUNCATE TABLE `YEU_CAU_BAO_TRI`;
TRUNCATE TABLE `CAU_HINH_MUC_UU_TIEN`;
TRUNCATE TABLE `THONG_SO_THIET_BI`;
TRUNCATE TABLE `THIET_BI`;
TRUNCATE TABLE `DANH_MUC_MA_LOI`;
TRUNCATE TABLE `NHOM_THIET_BI`;
TRUNCATE TABLE `NHAT_KY_HE_THONG`;
TRUNCATE TABLE `TAI_KHOAN`;
TRUNCATE TABLE `VAI_TRO_QUYEN`;
TRUNCATE TABLE `VAI_TRO`;
TRUNCATE TABLE `NHAN_VIEN`;
TRUNCATE TABLE `PHAN_XUONG_BO_PHAN`;
TRUNCATE TABLE `THU_VIEN_BIEU_MAU`;

-- ============================================================
-- 1. BẢNG THU_VIEN_BIEU_MAU (6 bản ghi BM-01 -> BM-06)
-- ============================================================
INSERT INTO `THU_VIEN_BIEU_MAU` (`BieuMauId`, `TenBieuMau`, `LoaiBieuMau`, `DinhDangTep`, `DuongDanTep`, `MoTa`) VALUES
('BM-01', 'Sổ Lý Lịch Thiết Bị Nhà Máy', 'LyLichThietBi', 'DOCX', '/templates/BM-01_So_Ly_Lich_Thiet_Bi.docx', 'Theo dõi toàn bộ thông số, lịch sử lắp đặt và bảo dưỡng thiết bị'),
('BM-02', 'Phiếu Báo Hỏng Thiết Bị Đột Xuất', 'BaoHong', 'DOCX', '/templates/BM-02_Phieu_Bao_Hong.docx', 'Dành cho người vận hành báo cáo sự cố dừng máy đột xuất'),
('BM-03', 'Mẫu Phiếu Kiểm Tra Checklist Định Kỳ', 'Checklist', 'PDF', '/templates/BM-03_Checklist_Bao_Tri.pdf', 'Danh mục các hạng mục kiểm tra định kỳ hàng ngày/tuần/tháng'),
('BM-04', 'Lệnh Sửa Chữa & Bảo Trì Thiết Bị', 'LenhSuaChua', 'DOCX', '/templates/BM-04_Lenh_Bao_Tri.docx', 'Lệnh điều phối công việc và phân công KTV từ Trưởng BP'),
('BM-05', 'Phiếu Dự Trù & Xuất Dùng Phụ Tùng', 'DuTruXuatDung', 'DOCX', '/templates/BM-05_Du_Tru_Xuat_Dung.docx', 'Dự trù và quyết toán vật tư phụ tùng thay thế cho từng lệnh'),
('BM-06', 'Biên Bản Nghiệm Thu & Bàn Giao Thiết Bị', 'NghiemThuBanGiao', 'PDF', '/templates/BM-06_Bien_Ban_Nghiem_Thu.pdf', 'Nghiệm thu bàn giao thiết bị sau sửa chữa giữa KTV và Phân xưởng');

-- ============================================================
-- 2. BẢNG VAI_TRO (5 bản ghi chuẩn)
-- ============================================================
INSERT INTO `VAI_TRO` (`VaiTroId`, `TenVaiTro`, `MoTa`) VALUES
(1, 'Quản trị hệ thống', 'Quản lý tài khoản, cấu hình tham số hệ thống và phân quyền'),
(2, 'Quản lý nhà máy', 'Giám đốc/Quản đốc nhà máy, duyệt tài khoản, duyệt điều chỉnh kho và xem báo cáo tổng hợp'),
(3, 'Trưởng bộ phận bảo trì', 'Lập kế hoạch, duyệt báo hỏng, phát lệnh bảo trì, điều phối KTV và quản lý tồn kho'),
(4, 'Kỹ thuật viên', 'Thực hiện kiểm tra checklist, sửa chữa máy, khai báo giờ công và vật tư thực dùng'),
(5, 'Người dùng thiết bị', 'Công nhân vận hành máy, gửi yêu cầu báo hỏng và ký nhận nghiệm thu bàn giao');

-- ============================================================
-- 3. BẢNG VAI_TRO_QUYEN (16 bản ghi phân quyền Enum)
-- ============================================================
INSERT INTO `VAI_TRO_QUYEN` (`VaiTroId`, `MaQuyen`) VALUES
(1, 'USER_MANAGE'), (1, 'ROLE_MANAGE'), (1, 'SYSTEM_CONFIG'), (1, 'AUDIT_VIEW'),
(2, 'ACCOUNT_APPROVE'), (2, 'STOCK_ADJUST_APPROVE'), (2, 'REPORT_VIEW_ALL'),
(3, 'DEVICE_MANAGE'), (3, 'REQUEST_APPROVE'), (3, 'WO_CREATE_DISPATCH'), (3, 'STOCK_PROPOSAL_CREATE'), (3, 'CHECKLIST_MANAGE'),
(4, 'WO_EXECUTE'), (4, 'CHECKLIST_RECORD'), (4, 'MATERIAL_DECLARE'),
(5, 'REQUEST_CREATE'), (5, 'WO_ACCEPTANCE_SIGN');

-- ============================================================
-- 4. BẢNG PHAN_XUONG_BO_PHAN (7 bản ghi có phân cấp cha - con)
-- ============================================================
INSERT INTO `PHAN_XUONG_BO_PHAN` (`DonViId`, `TenDonVi`, `DonViChaId`, `LoaiDonVi`, `NguoiPhuTrachId`, `MoTa`) VALUES
('PX-COKHI', 'Phân xưởng Gia công Cơ khí', NULL, 'PhanXuong', NULL, 'Khu vực gia công cắt gọt kim loại chính của nhà máy'),
('DC-CNC01', 'Dây chuyền Phay - Tiện CNC Số 1', 'PX-COKHI', 'DayChuyen', NULL, 'Dây chuyền gia công chi tiết động cơ chính xác'),
('DC-CNC02', 'Dây chuyền Phay - Tiện CNC Số 2', 'PX-COKHI', 'DayChuyen', NULL, 'Dây chuyền gia công khuôn mẫu và đồ gá'),
('DC-DOTDAP', 'Dây chuyền Dập & Chấn Kim loại', 'PX-COKHI', 'DayChuyen', NULL, 'Khu vực máy đột dập thủy lực và uốn tôn'),
('PX-LAPRAP', 'Phân xưởng Lắp ráp & Hoàn thiện', NULL, 'PhanXuong', NULL, 'Khu vực lắp ráp cụm chi tiết và đóng gói sản phẩm'),
('BP-BAOTRI', 'Bộ phận Kỹ thuật & Bảo trì', NULL, 'BoPhanQuanLy', NULL, 'Đội ngũ kỹ thuật cơ khí, điện và tự động hóa nhà máy'),
('BP-QUANLY', 'Ban Giám đốc & Quản lý Nhà máy', NULL, 'BoPhanQuanLy', NULL, 'Khối điều hành, văn phòng quản lý chất lượng và sản xuất');

-- ============================================================
-- 5. BẢNG NHAN_VIEN (10 nhân sự đầy đủ các vai trò)
-- ============================================================
INSERT INTO `NHAN_VIEN` (`NhanVienId`, `HoTen`, `SoDienThoai`, `Email`, `DonViId`, `ChucVu`, `ChuyenNganh`, `BacTho`, `CaLamViec`, `TrangThaiSanSang`, `TrangThai`) VALUES
('NV001', 'Nguyễn Văn An', '0912345678', 'an.nguyen@factory.vn', 'BP-QUANLY', 'Giám đốc Nhà máy', NULL, NULL, 'HanhChinh', NULL, 'DangLamViec'),
('NV002', 'Trần Đình Trọng', '0912345679', 'trong.tran@factory.vn', 'BP-BAOTRI', 'Trưởng BP Bảo trì', 'Cơ điện tử', '7/7', 'HanhChinh', 'SanSang', 'DangLamViec'),
('NV003', 'Lê Văn Ba', '0912345680', 'ba.le@factory.vn', 'BP-BAOTRI', 'KTV Cơ khí Trưởng', 'Cơ khí chính xác', '6/7', 'Ca 1', 'SanSang', 'DangLamViec'),
('NV004', 'Phạm Minh Đức', '0912345681', 'duc.pham@factory.vn', 'BP-BAOTRI', 'KTV Điện - Tự động hóa', 'Điện công nghiệp', '5/7', 'Ca 1', 'SanSang', 'DangLamViec'),
('NV005', 'Hoàng Tuấn Kiệt', '0912345682', 'kiet.hoang@factory.vn', 'BP-BAOTRI', 'KTV Thủy lực & Khí nén', 'Thủy lực', '4/7', 'Ca 2', 'SanSang', 'DangLamViec'),
('NV006', 'Đỗ Hải Quân', '0912345683', 'quan.do@factory.vn', 'BP-BAOTRI', 'KTV Bảo trì Cơ khí', 'Cơ khí', '3/7', 'Ca 3', 'DangBan', 'DangLamViec'),
('NV007', 'Vũ Quốc Cường', '0912345684', 'cuong.vu@factory.vn', 'DC-CNC01', 'Tổ trưởng Vận hành CNC', NULL, NULL, 'Ca 1', NULL, 'DangLamViec'),
('NV008', 'Bùi Thị Mai', '0912345685', 'mai.bui@factory.vn', 'DC-CNC02', 'Công nhân Vận hành Máy phay', NULL, NULL, 'Ca 2', NULL, 'DangLamViec'),
('NV009', 'Ngô Quang Huy', '0912345686', 'huy.ngo@factory.vn', 'DC-DOTDAP', 'Công nhân Vận hành Máy dập', NULL, NULL, 'Ca 1', NULL, 'DangLamViec'),
('NV010', 'Đinh Xuân Hùng', '0912345687', 'hung.dinh@factory.vn', 'BP-QUANLY', 'Quản trị viên Hệ thống IT', 'Công nghệ thông tin', NULL, 'HanhChinh', NULL, 'DangLamViec');

-- Cập nhật người phụ trách cho các bộ phận
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV001' WHERE `DonViId` = 'BP-QUANLY';
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV002' WHERE `DonViId` = 'BP-BAOTRI';
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV001' WHERE `DonViId` = 'PX-COKHI';
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV007' WHERE `DonViId` = 'DC-CNC01';
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV008' WHERE `DonViId` = 'DC-CNC02';
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV009' WHERE `DonViId` = 'DC-DOTDAP';
UPDATE `PHAN_XUONG_BO_PHAN` SET `NguoiPhuTrachId` = 'NV001' WHERE `DonViId` = 'PX-LAPRAP';

-- ============================================================
-- 6. BẢNG TAI_KHOAN (10 tài khoản liên kết 1-1 với nhân viên)
-- ============================================================
INSERT INTO `TAI_KHOAN` (`TaiKhoanId`, `NhanVienId`, `TenDangNhap`, `MatKhauHash`, `VaiTroId`, `TrangThaiTaiKhoan`, `SoLanDangNhapSai`, `ThoiDiemMoKhoaTam`, `NgayTao`, `NguoiDuyetId`) VALUES
(1, 'NV010', 'admin', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 1, 'DangHoatDong', 0, NULL, '2026-01-01 08:00:00', 'NV001'),
(2, 'NV001', 'manager.an', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 2, 'DangHoatDong', 0, NULL, '2026-01-01 08:00:00', 'NV010'),
(3, 'NV002', 'lead.trong', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 3, 'DangHoatDong', 0, NULL, '2026-01-02 08:30:00', 'NV001'),
(4, 'NV003', 'tech.bale', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 4, 'DangHoatDong', 0, NULL, '2026-01-02 09:00:00', 'NV001'),
(5, 'NV004', 'tech.duc', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 4, 'DangHoatDong', 0, NULL, '2026-01-02 09:15:00', 'NV001'),
(6, 'NV005', 'tech.kiet', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 4, 'DangHoatDong', 0, NULL, '2026-01-02 09:30:00', 'NV001'),
(7, 'NV006', 'tech.quan', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 4, 'DangHoatDong', 0, NULL, '2026-01-02 09:45:00', 'NV001'),
(8, 'NV007', 'op.cuong', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 5, 'DangHoatDong', 0, NULL, '2026-01-03 08:00:00', 'NV001'),
(9, 'NV008', 'op.mai', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 5, 'DangHoatDong', 0, NULL, '2026-01-03 08:15:00', 'NV001'),
(10, 'NV009', 'op.huy', '$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9', 5, 'ChoDuyet', 0, NULL, '2026-01-03 08:30:00', NULL);

-- ============================================================
-- 7. BẢNG NHAT_KY_HE_THONG (8 bản ghi kiểm toán)
-- ============================================================
INSERT INTO `NHAT_KY_HE_THONG` (`NhatKyId`, `TaiKhoanId`, `ThoiDiem`, `LoaiThaoTac`, `DoiTuongTacDong`, `MoTaChiTiet`) VALUES
(1, 1, '2026-11-20 08:00:15', 'DangNhap', 'SYS', 'Quản trị viên đăng nhập hệ thống thành công từ IP 192.168.1.10'),
(2, 3, '2026-11-20 08:15:30', 'XacNhanBoSungTonKho', 'CNK-202611-001', 'Trưởng BP đề xuất bổ sung 50 vòng bi NSK 6205 từ kho tổng'),
(3, 2, '2026-11-20 09:00:00', 'XacNhanBoSungTonKho', 'CNK-202611-001', 'Quản lý nhà máy duyệt phiếu nhập kho CNK-202611-001'),
(4, 8, '2026-11-21 07:30:00', 'TaoYeuCauBaoHong', 'YC-202611-001', 'Công nhân Vũ Quốc Cường báo hỏng quá nhiệt trục chính máy tiện CNC'),
(5, 3, '2026-11-21 07:45:00', 'DuyetYeuCau', 'YC-202611-001', 'Trưởng BP duyệt yêu cầu và phát lệnh sửa chữa khẩn cấp WO-202611-001'),
(6, 3, '2026-11-21 08:00:00', 'PhanCongKTV', 'WO-202611-001', 'Phân công KTV Lê Văn Ba làm thợ chính lệnh WO-202611-001'),
(7, 4, '2026-11-21 10:30:00', 'KhaiBaoVatTu', 'PX-202611-001', 'KTV khai báo xuất 2 vòng bi NSK 6205 để thay thế trục chính'),
(8, 3, '2026-11-21 11:30:00', 'DongLenhBaoTri', 'WO-202611-001', 'Nghiệm thu hoàn tất và duyệt đóng lệnh sửa chữa WO-202611-001');

-- ============================================================
-- 8. BẢNG NHOM_THIET_BI (6 bản ghi gồm máy chính & cụm con)
-- ============================================================
INSERT INTO `NHOM_THIET_BI` (`NhomThietBiId`, `TenNhom`, `LaCumChiTiet`, `MoTa`) VALUES
('NHOM-TIEN-CNC', 'Máy Tiện CNC Chính Xác', 0, 'Các dòng máy tiện CNC 2 trục và 3 trục gia công trục tròn xoay'),
('NHOM-PHAY-CNC', 'Máy Phay CNC 3-5 Trục', 0, 'Trung tâm gia công phay CNC phôi cơ khí và khuôn mẫu'),
('NHOM-EP-THUYLUC', 'Máy Ép & Đột Dập Thủy Lực', 0, 'Hệ thống máy ép thủy lực công suất từ 50T đến 250T'),
('NHOM-CAT-PLASMA', 'Máy Cắt Kim Loại Plasma & Laser', 0, 'Thiết bị cắt tấm kim loại tấm công nghiệp độ chính xác cao'),
('CUM-SPINDLE', 'Cụm Trục Chính Spindle', 1, 'Cụm đầu trục chính quay tốc độ cao (BT40, BT50, HSK)'),
('CUM-TURRET', 'Cụm Đài Gá Dao Tự Động', 1, 'Đài xoay chứa dao tiện 8-12 vị trí kèm hệ thống khóa thủy lực');

-- ============================================================
-- 9. BẢNG DANH_MUC_MA_LOI (6 bản ghi mã lỗi sự cố)
-- ============================================================
INSERT INTO `DANH_MUC_MA_LOI` (`MaLoiId`, `TenLoaiSuCo`, `MoTaDauHieu`) VALUES
('ERR-OVERHEAT', 'Quá Nhiệt Trục Chính (Spindle Overheat)', 'Nhiệt độ vỏ trục chính vượt quá 65 độ C, phát chuông cảnh báo'),
('ERR-TOOL-JAM', 'Kẹt Cơ Cấu Thay Dao / Đài Dao', 'Đài dao không xoay đổi vị trí hoặc kẹt tay gắp dao ATC'),
('ERR-OIL-LEAK', 'Rò Rỉ Dầu Hệ Thống Thủy Lực', 'Áp suất tụt dưới ngưỡng cho phép, có vết dầu chảy trên sàn'),
('ERR-INVERTER', 'Lỗi Biến Tần Điều Khiển Động Cơ', 'Màn hình báo lỗi AL-04/AL-12, động cơ phát tiếng kêu gằn'),
('ERR-VIBRATION', 'Rung Lắc & Rơ Mâm Cặp / Ổ Bi', 'Độ đảo trục vượt quá dung sai 0.02mm, gây sóng trên bề mặt phôi'),
('ERR-SENSOR', 'Hỏng Cảm Biến Hành Trình Trục', 'Trục máy không về được gốc tọa độ Home hoặc báo Over-travel ảo');

-- ============================================================
-- 10. BẢNG THIET_BI (8 thiết bị gồm máy cha & cụm con 2 cấp)
-- ============================================================
INSERT INTO `THIET_BI` (`ThietBiId`, `TenThietBi`, `ThietBiChaId`, `NhomThietBiId`, `ViTriLapDatId`, `BoPhanQuanLyId`, `Model`, `Serial`, `NhaSanXuat`, `NgayDuaVaoSuDung`, `ChuKyBaoTriKhuyenNghiNgay`, `CongDungMoTa`, `LuuYAnToan`, `TrangThaiVanHanh`, `DuongDanAnh`, `DuongDanTaiLieu`) VALUES
('TB-CNC-01', 'Máy Tiện CNC Doosan Lynx 220', NULL, 'NHOM-TIEN-CNC', 'DC-CNC01', 'DC-CNC01', 'Lynx 220LY', 'SN-DS220-202301', 'Doosan Machine Tools', '2023-01-15', 30, 'Gia công các chi tiết trục tròn, tiện ren chính xác cao', 'Tắt nguồn trước khi đo kiểm tra phôi, đóng kín cửa cabin khi chạy', 'DangHoatDong', '/images/tb-cnc-01.jpg', '/docs/Lynx220_Manual.pdf'),
('CUM-SP-01', 'Cụm Trục Chính Spindle Doosan 6000RPM', 'TB-CNC-01', 'CUM-SPINDLE', 'DC-CNC01', 'DC-CNC01', 'SP-DOOSAN-6K', 'SN-SP01-9921', 'Doosan Machine Tools', '2023-01-15', 30, 'Cụm quay phôi tốc độ cao cho máy tiện Lynx 220', 'Tuyệt đối không chạy quá tốc độ định mức 6000 rpm', 'DangHoatDong', '/images/cum-sp-01.jpg', NULL),
('CUM-TR-01', 'Cụm Đài Gá Dao BTA-12 Doosan', 'TB-CNC-01', 'CUM-TURRET', 'DC-CNC01', 'DC-CNC01', 'BTA-12-VDI30', 'SN-TR01-8842', 'Doosan Machine Tools', '2023-01-15', 60, 'Đài kẹp dao 12 vị trí xoay servo', 'Chú ý khoảng an toàn tránh va chạm ụ động khi đổi dao', 'DangHoatDong', NULL, NULL),
('TB-CNC-02', 'Máy Phay CNC Mazak VCN-530C', NULL, 'NHOM-PHAY-CNC', 'DC-CNC02', 'DC-CNC02', 'VCN-530C', 'SN-MZ530-202305', 'Yamazaki Mazak', '2023-05-20', 30, 'Gia công phay mặt, khoét lỗ khuôn mẫu cơ khí 3 chiều', 'Đeo kính bảo hộ, kiểm tra chốt kẹp dao BT40 trước khi chạy', 'DangHoatDong', '/images/tb-cnc-02.jpg', '/docs/Mazak_VCN530C.pdf'),
('CUM-SP-02', 'Cụm Trục Chính Mazak 12000RPM BT40', 'TB-CNC-02', 'CUM-SPINDLE', 'DC-CNC02', 'DC-CNC02', 'SP-MAZAK-12K', 'SN-SP02-7741', 'Yamazaki Mazak', '2023-05-20', 30, 'Cụm trục chính phay BT40 giải nhiệt bằng dầu', 'Đảm bảo áp suất dầu làm mát trục chính ổn định trên 3.5 bar', 'DangHoatDong', NULL, NULL),
('TB-EP-01', 'Máy Ép Thủy Lực Komatsu 100T', NULL, 'NHOM-EP-THUYLUC', 'DC-DOTDAP', 'DC-DOTDAP', 'H1F-100-2', 'SN-KM100-202208', 'Komatsu Press', '2022-08-10', 45, 'Dập định hình tấm kim loại và đột lỗ đồ gá', 'Hai tay nhấn nút điều khiển đồng thời, cấm đưa tay vào vùng sáng an toàn quang điện', 'DangHoatDong', '/images/tb-ep-01.jpg', '/docs/Komatsu_100T.pdf'),
('TB-EP-02', 'Máy Dập Thủy Lực Amada 150T', NULL, 'NHOM-EP-THUYLUC', 'DC-DOTDAP', 'DC-DOTDAP', 'TPX-150', 'SN-AM150-202111', 'Amada Machinery', '2021-11-05', 45, 'Chấn và ép đột tấm thép kết cấu dày', 'Kiểm tra mức dầu thùng chứa trước khi khởi động bơm', 'DungDoSuCo', '/images/tb-ep-02.jpg', NULL),
('TB-CUT-01', 'Máy Cắt Plasma CNC Hypertherm', NULL, 'NHOM-CAT-PLASMA', 'PX-COKHI', 'PX-COKHI', 'Powermax 125', 'SN-PM125-202303', 'Hypertherm USA', '2023-03-12', 60, 'Cắt kim loại tấm thép carbon và inox chiều dày đến 25mm', 'Trang bị mặt nạ hàn và hệ thống hút khói lọc khí độc', 'DangHoatDong', NULL, '/docs/Hypertherm_125.pdf');

-- ============================================================
-- 11. BẢNG THONG_SO_THIET_BI (8 bản ghi thông số kỹ thuật)
-- ============================================================
INSERT INTO `THONG_SO_THIET_BI` (`ThongSoId`, `ThietBiId`, `TenThongSo`, `GiaTri`, `DonViDo`, `MoTa`) VALUES
(1, 'TB-CNC-01', 'Đường kính tiện lớn nhất', '300', 'mm', 'Hành trình gia công tiêu chuẩn'),
(2, 'TB-CNC-01', 'Chiều dài tiện lớn nhất', '510', 'mm', 'Khoảng cách giữa hai tâm chống'),
(3, 'CUM-SP-01', 'Công suất động cơ trục chính', '15', 'kW', 'Động cơ servo biến tần AC'),
(4, 'CUM-SP-01', 'Tốc độ quay cực đại', '6000', 'rpm', 'Tốc độ vòng/phút lớn nhất'),
(5, 'TB-CNC-02', 'Hành trình trục X / Y / Z', '1050 x 530 x 510', 'mm', 'Phạm vi làm việc của bàn máy phay'),
(6, 'CUM-SP-02', 'Côn trục chính', 'BT40', NULL, 'Tiêu chuẩn đầu kẹp chuôi dao'),
(7, 'TB-EP-01', 'Lực ép danh định', '100', 'Tấn', 'Áp lực ép thủy lực tối đa'),
(8, 'TB-EP-01', 'Áp suất làm việc hệ thống', '210', 'bar', 'Áp suất dầu thủy lực van an toàn');

-- ============================================================
-- 12. BẢNG CAU_HINH_MUC_UU_TIEN (5 mức SLA chuẩn)
-- ============================================================
INSERT INTO `CAU_HINH_MUC_UU_TIEN` (`MucUuTienId`, `TenMucUuTien`, `ThuTuUuTien`, `MaMauHienThi`, `ThoiHanPhanHoiPhut`) VALUES
(1, 'Khẩn cấp', 1, '#FF0000', 30),
(2, 'Cao', 2, '#FFA500', 120),
(3, 'Trung bình', 3, '#FFFF00', 480),
(4, 'Thấp', 4, '#008000', 1440),
(5, 'Kế hoạch tự do', 5, '#808080', 2880);

-- ============================================================
-- 13. BẢNG MAU_CHECKLIST (5 mẫu checklist quy trình)
-- ============================================================
INSERT INTO `MAU_CHECKLIST` (`MauChecklistId`, `TenMau`, `NhomThietBiId`, `MoTaTieuChuan`, `TrangThaiHieuLuc`) VALUES
('CK-TIEN-HANGNGAY', 'Checklist Kiểm Tra Hàng Ngày Máy Tiện CNC', 'NHOM-TIEN-CNC', 'Kiểm tra mức dầu, áp suất khí, độ êm trục chính trước ca làm việc', 'DangApDung'),
('CK-PHAY-HANGTUAN', 'Checklist Bảo Dưỡng Hàng Tuần Máy Phay CNC', 'NHOM-PHAY-CNC', 'Vệ sinh phoi, bôi trơn ray trượt, kiểm tra độ căng dây đai trục chính', 'DangApDung'),
('CK-EP-HANGTHANG', 'Checklist Định Kỳ Hàng Tháng Máy Ép Thủy Lực', 'NHOM-EP-THUYLUC', 'Kiểm tra rò rỉ gioăng phớt xi lanh, nhiệt độ dầu thủy lực, căn chỉnh bàn ép', 'DangApDung'),
('CK-SPINDLE-DINHKY', 'Checklist Kiểm Định Rung Động & Độ Đảo Spindle', 'CUM-SPINDLE', 'Đo độ đảo côn trục chính, kiểm tra rung động bằng thiết bị phân tích', 'DangApDung'),
('CK-PLASMA-HANGTUAN', 'Checklist Vệ Sinh & Bảo Dưỡng Máy Cắt Plasma', 'NHOM-CAT-PLASMA', 'Kiểm tra béc cắt, áp lực khí nén khô và ray trượt tọa độ XY', 'DangApDung');

-- ============================================================
-- 14. BẢNG HANG_MUC_CHECKLIST (10 hạng mục kiểm tra chi tiết)
-- ============================================================
INSERT INTO `HANG_MUC_CHECKLIST` (`HangMucId`, `MauChecklistId`, `NhomBoPhan`, `NoiDungCongViec`, `KieuKetQua`, `TieuChuanYeuCau`, `GiaTriMin`, `GiaTriMax`, `DonViDo`, `ThuTuThucHien`) VALUES
(1, 'CK-TIEN-HANGNGAY', 'Hệ Khí Nén', 'Đo áp suất khí nén cấp vào máy', 'NhapSoDo', 'Áp suất nằm trong ngưỡng 5.0 - 6.5 bar', 5.00, 6.50, 'bar', 1),
(2, 'CK-TIEN-HANGNGAY', 'Hệ Bôi Trơn', 'Kiểm tra mức dầu bôi trơn ray trượt trong bình chứa', 'Dat_KhongDat', 'Mức dầu nằm giữa vạch Min và Max', NULL, NULL, NULL, 2),
(3, 'CK-TIEN-HANGNGAY', 'Cụm Mâm Cặp', 'Kiểm tra đóng mở kẹp phôi mâm cặp thủy lực', 'Dat_KhongDat', 'Kẹp nhả dứt khoát, không kẹt phoi', NULL, NULL, NULL, 3),
(4, 'CK-PHAY-HANGTUAN', 'Trục Chính BT40', 'Đo nhiệt độ vỏ trục chính sau 15 phút chạy không tải', 'NhapSoDo', 'Nhiệt độ vỏ không quá 45 độ C', 20.00, 45.00, 'Độ C', 1),
(5, 'CK-PHAY-HANGTUAN', 'Cơ Cấu ATC', 'Kiểm tra độ rơ và bôi trơn tay gắp dao tự động', 'Dat_KhongDat', 'Hoạt động êm, không rung giật khi đảo dao', NULL, NULL, NULL, 2),
(6, 'CK-EP-HANGTHANG', 'Thùng Dầu Thủy Lực', 'Đo nhiệt độ dầu làm việc trong bình', 'NhapSoDo', 'Nhiệt độ dầu dưới 55 độ C', 25.00, 55.00, 'Độ C', 1),
(7, 'CK-EP-HANGTHANG', 'Xi Lanh Ép', 'Kiểm tra xước cán piston và rò rỉ phớt chắn dầu', 'Dat_KhongDat', 'Cán piston sáng bóng, không ướt dầu mép phớt', NULL, NULL, NULL, 2),
(8, 'CK-EP-HANGTHANG', 'Áp Suất Bơm', 'Kiểm tra áp lực van tràn an toàn khi ép kịch hành trình', 'NhapSoDo', 'Đạt áp suất thiết kế 200 - 215 bar', 200.00, 215.00, 'bar', 3),
(9, 'CK-SPINDLE-DINHKY', 'Côn Trục Spindle', 'Đo độ đảo mặt côn trong trục chính bằng đồng hồ so', 'NhapSoDo', 'Độ đảo hướng kính không quá 0.005mm', 0.00, 0.005, 'mm', 1),
(10, 'CK-PLASMA-HANGTUAN', 'Mỏ Cắt Plasma', 'Kiểm tra độ mòn điện cực và chụp khí mỏ cắt', 'Dat_KhongDat', 'Hố mòn điện cực dưới 1.5mm, lỗ béc tròn đều', NULL, NULL, NULL, 1);

-- ============================================================
-- 15. BẢNG KE_HOACH_BAO_TRI (5 kế hoạch bảo trì chu kỳ)
-- ============================================================
INSERT INTO `KE_HOACH_BAO_TRI` (`KeHoachId`, `TenKeHoach`, `ThietBiId`, `MauChecklistId`, `DonViChuKy`, `GiaTriChuKy`, `SoNgaySinhLenhTruoc`, `ThoiGianDuKienPhut`, `NgayBatDauApDung`, `NgayHoanThanhGanNhat`, `NgayDenHanTiepTheo`, `TrangThaiKeHoach`, `MoTa`) VALUES
('KH-CNC01-DAILY', 'Kiểm tra bảo dưỡng đầu ca Máy Tiện Lynx 220', 'TB-CNC-01', 'CK-TIEN-HANGNGAY', 'Ngay', 1, 1, 30, '2026-01-01', '2026-11-23', '2026-11-24', 'DangApDung', 'Thực hiện đầu giờ sáng mỗi ca sản xuất'),
('KH-CNC02-WEEKLY', 'Bảo dưỡng định kỳ hàng tuần Máy Phay Mazak', 'TB-CNC-02', 'CK-PHAY-HANGTUAN', 'Tuan', 1, 2, 90, '2026-01-01', '2026-11-18', '2026-11-25', 'DangApDung', 'Thực hiện vào sáng thứ Bảy hàng tuần'),
('KH-EP01-MONTHLY', 'Bảo dưỡng hàng tháng Hệ Thủy Lực Máy Ép Komatsu', 'TB-EP-01', 'CK-EP-HANGTHANG', 'Thang', 1, 3, 180, '2026-01-01', '2026-10-30', '2026-11-30', 'DangApDung', 'Kiểm tra dầu và thay lọc dầu hồi'),
('KH-SP01-QUARTER', 'Kiểm định rung động & độ đảo Cụm Spindle Doosan', 'CUM-SP-01', 'CK-SPINDLE-DINHKY', 'Quy', 1, 5, 120, '2026-01-01', '2026-08-30', '2026-11-30', 'DangApDung', 'Sử dụng máy phân tích độ rung chuyên dụng'),
('KH-PLASMA-WEEKLY', 'Bảo dưỡng hệ thống khí & mỏ cắt Plasma', 'TB-CUT-01', 'CK-PLASMA-HANGTUAN', 'Tuan', 1, 2, 60, '2026-01-01', '2026-11-19', '2026-11-26', 'DangApDung', 'Xả nước cốc lọc khí và thay phụ kiện mỏ');

-- ============================================================
-- 16. BẢNG PHU_TUNG (10 phụ tùng, vật tư, dụng cụ)
-- ============================================================
INSERT INTO `PHU_TUNG` (`PhuTungId`, `TenPhuTung`, `LoaiPhuTung`, `QuyCach`, `DonViTinh`, `SoLuongTonHienCo`, `SoLuongDangDuTru`, `MucTonToiThieu`, `MoTa`) VALUES
('PT-VB-6205', 'Vòng bi cầu đỡ chặn NSK 6205-DDU', 'CoKhi', '25x52x15 mm, C3', 'Cái', 48.00, 4.00, 10.00, 'Dùng cho trục gá và động cơ phụ máy tiện'),
('PT-VB-7010', 'Vòng bi chính xác Spindle NSK 7010-C', 'CoKhi', '50x80x16 mm, Cặp P4', 'Bộ', 6.00, 2.00, 2.00, 'Bộ vòng bi góc tiếp xúc dùng cho trục chính CNC'),
('PT-DAU-VG46', 'Dầu Thủy Lực Shell Tellus S2 MX 46', 'TieuHao', 'Phuy 209 Lít', 'Lít', 450.00, 50.00, 100.00, 'Dầu tuần hoàn cho máy ép thủy lực và mâm cặp'),
('PT-DAU-VG68', 'Dầu Bôi Trơn Đường Trượt Mobil Vactra No.2', 'TieuHao', 'Xô 20 Lít (ISO VG 68)', 'Lít', 85.00, 10.00, 20.00, 'Dầu bôi trơn tự động băng trượt máy tiện/phay'),
('PT-CUROA-5M', 'Dây Curoa Răng Bando 5M-650', 'CoKhi', 'Bản rộng 25mm, bước răng 5mm', 'Sợi', 14.00, 2.00, 5.00, 'Dây curoa truyền động trục phụ máy tiện'),
('PT-PHOT-NOK', 'Phớt Chắn Dầu Thủy Lực NOK USH-80', 'CoKhi', 'Đường kính 80x95x9 mm', 'Cái', 20.00, 0.00, 5.00, 'Phớt gạt bụi và chắn dầu ty ben xi lanh ép'),
('PT-LOC-DAU', 'Lõi Lọc Dầu Hút Thủy Lực Taisei Kogyo', 'TieuHao', 'Lưới inox 100 Mesh, lưu lượng 60L/p', 'Cái', 12.00, 2.00, 4.00, 'Lọc đáy thùng dầu thủy lực máy dập Komatsu'),
('PT-RL-OMRON', 'Rơ Le Kiếng Trung Gian Omron MY4N-J', 'Dien', 'Cuộn hút 24VDC, 4 cặp tiếp điểm 5A', 'Cái', 35.00, 0.00, 10.00, 'Rơ le kích tín hiệu điều khiển van điện từ'),
('PT-CB-OMRON', 'Cảm Biến Tiệm Cận Cảm Ứng Omron E2E', 'Dien', 'M12, NPN NO, khoảng cách 4mm', 'Cái', 15.00, 1.00, 5.00, 'Cảm biến xác định vị trí gốc tọa độ trục máy'),
('PT-BEC-PLASMA', 'Béc Cắt Plasma Hypertherm 125A', 'TieuHao', 'Mã Nozzle 220975 (125 Amp)', 'Hộp', 18.00, 0.00, 5.00, 'Hộp 5 cái béc cắt thay thế máy cắt tấm');

-- ============================================================
-- 17. BẢNG DINH_MUC_LINH_KIEN (8 định mức linh kiện theo máy)
-- ============================================================
INSERT INTO `DINH_MUC_LINH_KIEN` (`DinhMucId`, `ThietBiId`, `PhuTungId`, `SoLuongDinhMuc`, `ThayTheDinhKy`, `ViTriLapDat`) VALUES
(1, 'TB-CNC-01', 'PT-VB-6205', 2.00, 0, 'Động cơ bơm dầu tưới nguội'),
(2, 'TB-CNC-01', 'PT-DAU-VG68', 5.00, 1, 'Bình dầu bôi trơn trung tâm máy'),
(3, 'CUM-SP-01', 'PT-VB-7010', 1.00, 1, 'Bộ vòng bi đỡ đầu trục chính'),
(4, 'TB-CNC-02', 'PT-DAU-VG68', 6.00, 1, 'Bình bôi trơn băng trượt tự động'),
(5, 'CUM-SP-02', 'PT-VB-7010', 1.00, 1, 'Bộ vòng bi trục chính Spindle BT40'),
(6, 'TB-EP-01', 'PT-DAU-VG46', 150.00, 1, 'Thùng dầu thủy lực tuần hoàn chính'),
(7, 'TB-EP-01', 'PT-PHOT-NOK', 2.00, 1, 'Cụm cổ xi lanh ép chính'),
(8, 'TB-EP-01', 'PT-LOC-DAU', 1.00, 1, 'Cổng hút bơm thủy lực bánh răng');

-- ============================================================
-- 18. BẢNG CHUYEN_MON_KTV (8 năng lực kỹ thuật viên)
-- ============================================================
INSERT INTO `CHUYEN_MON_KTV` (`NhanVienId`, `NhomThietBiId`, `GhiChu`) VALUES
('NV003', 'NHOM-TIEN-CNC', 'Thợ chính - Chuyên gia sửa chữa cơ khí máy tiện CNC'),
('NV003', 'CUM-SPINDLE', 'Thợ chính - Chuyên căn chỉnh và thay thế vòng bi Spindle'),
('NV004', 'NHOM-TIEN-CNC', 'Thợ chính - Xử lý mạch điện, biến tần và thước đo quang'),
('NV004', 'NHOM-PHAY-CNC', 'Thợ chính - Xử lý lỗi servo drive và hệ thống điện CNC Mazak'),
('NV005', 'NHOM-EP-THUYLUC', 'Thợ chính - Xử lý cụm van chia, bơm thủy lực và phớt áp lực'),
('NV005', 'NHOM-TIEN-CNC', 'Thợ phụ - Hỗ trợ bảo dưỡng hệ thống khí nén mâm cặp'),
('NV006', 'NHOM-CAT-PLASMA', 'Thợ chính - Bảo trì cơ cấu truyền động ray trục XY máy plasma'),
('NV006', 'NHOM-EP-THUYLUC', 'Thợ phụ - Hỗ trợ thay dầu và vệ sinh thùng chứa máy dập');

-- ============================================================
-- 19. BẢNG YEU_CAU_BAO_TRI (6 phiếu báo hỏng sự cố)
-- ============================================================
INSERT INTO `YEU_CAU_BAO_TRI` (`YeuCauId`, `ThietBiId`, `NguoiBaoId`, `ThoiDiemGui`, `MoTaTrieuChung`, `DuongDanAnhSuCo`, `MayDangDung`, `MucUuTienDeXuatId`, `MucUuTienChinhThucId`, `HanPhanHoi`, `TrangThaiYeuCau`, `MaLoiId`, `NhanDinhNguyenNhanSoBo`, `HuongXuLy`, `NguoiDuyetId`, `ThoiDiemDuyet`, `LyDoTuChoi`) VALUES
('YC-202611-001', 'TB-CNC-01', 'NV007', '2026-11-21 07:30:00', 'Trục chính phát tiếng kêu rít lớn khi tăng tốc lên 3000 rpm, nhiệt độ vỏ tăng cao', '/issues/spindle_noise.jpg', 1, 1, 1, '2026-11-21 08:00:00', 'HoanThanh', 'ERR-OVERHEAT', 'Nghi ngờ mòn vòng bi đỡ Spindle do thiếu bôi trơn', 'SuaChuaKhanCap', 'NV002', '2026-11-21 07:45:00', NULL),
('YC-202611-002', 'TB-EP-02', 'NV009', '2026-11-22 08:15:00', 'Dầu thủy lực rò rỉ mạnh dưới đáy xi lanh, áp lực tụt không đủ dập phôi', '/issues/oil_leak.jpg', 1, 1, 1, '2026-11-22 08:45:00', 'DangXuLy', 'ERR-OIL-LEAK', 'Rách gioăng phớt áp lực xi lanh chính', 'SuaChuaKhanCap', 'NV002', '2026-11-22 08:30:00', NULL),
('YC-202611-003', 'TB-CNC-02', 'NV008', '2026-11-23 09:00:00', 'Đài gá dao phát tiếng kêu lạch cạch khi đổi dao từ T03 sang T04', NULL, 0, 2, 2, '2026-11-23 11:00:00', 'DaPheDuyet', 'ERR-TOOL-JAM', 'Kẹt phoi trong rãnh chốt hãm đài dao', 'SuaChuaTheoKeHoach', 'NV002', '2026-11-23 09:40:00', NULL),
('YC-202611-004', 'TB-CUT-01', 'NV007', '2026-11-23 14:00:00', 'Tia plasma cắt bị lệch mép nghiêng quá 5 độ so với bản vẽ', NULL, 0, 3, 3, '2026-11-23 22:00:00', 'ChoTiepNhan', 'ERR-VIBRATION', NULL, NULL, NULL, NULL, NULL),
('YC-202611-005', 'TB-EP-01', 'NV009', '2026-11-24 10:00:00', 'Nhiệt độ dầu thủy lực báo đèn vàng 58 độ C sau 4 tiếng dập liên tục', NULL, 0, 2, 2, '2026-11-24 12:00:00', 'DaPheDuyet', 'ERR-OVERHEAT', 'Bẩn dàn làm mát dầu bằng gió, cần vệ sinh lưới', 'SuaChuaTheoKeHoach', 'NV002', '2026-11-24 10:30:00', NULL),
('YC-202611-006', 'TB-CNC-01', 'NV007', '2026-11-24 15:30:00', 'Báo hỏng do rơi chìa khóa vào máng xối phoi', NULL, 0, 4, NULL, '2026-11-25 15:30:00', 'TuChoi', NULL, 'Không phải sự cố kỹ thuật hỏng hóc máy móc', NULL, 'NV002', '2026-11-24 16:00:00', 'Yêu cầu công nhân tự gắp vật thể lạ, không thuộc phạm vi lệnh bảo trì');

-- ============================================================
-- 20. BẢNG LENH_BAO_TRI (8 lệnh gồm sự cố và phòng ngừa)
-- ============================================================
INSERT INTO `LENH_BAO_TRI` (`LenhBaoTriId`, `ThietBiId`, `LoaiLenh`, `YeuCauId`, `KeHoachId`, `NoiDungCongViec`, `MucUuTienId`, `ThoiGianDuKienBatDau`, `ThoiGianDuKienKetThuc`, `ThoiDiemThucTeBatDau`, `ThoiDiemThucTeKetThuc`, `LyDoTamDung`, `TrangThaiLenh`, `TinhTrangSauBaoTri`, `NguoiBanGiaoId`, `NguoiKyNhanId`, `ThoiDiemNghiemThu`, `NhanXetNghiemThu`, `NguoiTaoId`, `NguoiDuyetDongId`) VALUES
('WO-202611-001', 'TB-CNC-01', 'BaoTriSuCo', 'YC-202611-001', NULL, 'Tháo kiểm tra cụm Spindle, thay thế vòng bi đỡ và bơm mỡ bôi trơn tốc độ cao', 1, '2026-11-21 08:00:00', '2026-11-21 12:00:00', '2026-11-21 08:10:00', '2026-11-21 11:20:00', NULL, 'DaDong', 'DatChuan', 'NV003', 'NV007', '2026-11-21 11:30:00', 'Máy chạy êm ở 3500 rpm, nhiệt độ vỏ 38 độ C đạt chuẩn', 'NV002', 'NV002'),
('WO-202611-002', 'TB-EP-02', 'BaoTriSuCo', 'YC-202611-002', NULL, 'Xả dầu, hạ ty ben xi lanh ép và thay mới bộ phớt chặn dầu NOK USH-80', 1, '2026-11-22 08:45:00', '2026-11-22 14:00:00', '2026-11-22 09:00:00', NULL, NULL, 'DangThucHien', NULL, NULL, NULL, NULL, NULL, 'NV002', NULL),
('WO-202611-003', 'TB-CNC-02', 'BaoTriPhongNgua', NULL, 'KH-CNC02-WEEKLY', 'Bảo dưỡng tuần: Vệ sinh phoi khay ATC, căng dây đai và đo nhiệt độ trục chính', 3, '2026-11-18 08:00:00', '2026-11-18 09:30:00', '2026-11-18 08:05:00', '2026-11-18 09:25:00', NULL, 'DaDong', 'DatChuan', 'NV004', 'NV008', '2026-11-18 09:30:00', 'Các hạng mục checklist đều đạt yêu cầu kỹ thuật', 'NV002', 'NV002'),
('WO-202611-004', 'TB-EP-01', 'BaoTriPhongNgua', NULL, 'KH-EP01-MONTHLY', 'Bảo dưỡng tháng máy ép Komatsu: Kiểm tra lọc dầu, siết ốc bệ và kiểm định áp suất', 2, '2026-11-20 13:00:00', '2026-11-20 16:00:00', '2026-11-20 13:15:00', '2026-11-20 15:50:00', NULL, 'DaDong', 'DatChuan', 'NV005', 'NV009', '2026-11-20 16:00:00', 'Đã thay lõi lọc hồi, áp suất 208 bar ổn định', 'NV002', 'NV002'),
('WO-202611-005', 'TB-CNC-02', 'BaoTriSuCo', 'YC-202611-003', NULL, 'Căn chỉnh và vệ sinh chốt hãm thủy lực cụm đài dao ATC', 2, '2026-11-24 13:30:00', '2026-11-24 16:30:00', NULL, NULL, NULL, 'DaPhanCong', NULL, NULL, NULL, NULL, NULL, 'NV002', NULL),
('WO-202611-006', 'TB-CUT-01', 'BaoTriPhongNgua', NULL, 'KH-PLASMA-WEEKLY', 'Bảo dưỡng định kỳ mỏ cắt và đường ray trục XY máy plasma CNC', 3, '2026-11-26 08:00:00', '2026-11-26 09:00:00', NULL, NULL, NULL, 'MoiTao', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
('WO-202611-007', 'TB-CNC-01', 'BaoTriSuCo', NULL, NULL, 'Sửa chữa đột xuất: Thay thế cảm biến hành trình trục X bị chập chờn', 2, '2026-11-15 09:00:00', '2026-11-15 11:30:00', '2026-11-15 09:10:00', '2026-11-15 11:15:00', NULL, 'DaDong', 'DatChuan', 'NV004', 'NV007', '2026-11-15 11:25:00', 'Trục X về Home chính xác lặp lại 100%', 'NV002', 'NV002'),
('WO-202611-008', 'TB-CUT-01', 'BaoTriSuCo', NULL, NULL, 'Sửa chữa đột xuất: Xử lý biến tần động cơ quạt hút khói báo lỗi quá tải', 3, '2026-11-17 14:00:00', '2026-11-17 16:30:00', '2026-11-17 14:15:00', '2026-11-17 16:10:00', NULL, 'DaDong', 'DatChuan', 'NV004', 'NV007', '2026-11-17 16:20:00', 'Quạt hút chạy êm, dòng điện ổn định 12A', 'NV002', 'NV002');

-- ============================================================
-- 21. BẢNG PHAN_CONG_KTV (8 phân công KTV chi tiết)
-- ============================================================
INSERT INTO `PHAN_CONG_KTV` (`PhanCongId`, `LenhBaoTriId`, `KyThuatVienId`, `NguoiPhanCongId`, `ThoiDiemPhanCong`, `KhungGioBatDau`, `KhungGioKetThuc`, `VaiTroTrongLenh`, `SoGioCongThucTe`, `TrangThaiPhanCong`) VALUES
(1, 'WO-202611-001', 'NV003', 'NV002', '2026-11-21 08:00:00', '2026-11-21 08:10:00', '2026-11-21 11:20:00', 'ThoChinh', 3.17, 'HoanThanh'),
(2, 'WO-202611-001', 'NV004', 'NV002', '2026-11-21 08:00:00', '2026-11-21 08:10:00', '2026-11-21 10:40:00', 'ThoPhu', 2.50, 'HoanThanh'),
(3, 'WO-202611-002', 'NV005', 'NV002', '2026-11-22 08:45:00', '2026-11-22 09:00:00', '2026-11-22 14:00:00', 'ThoChinh', 2.00, 'DangLam'),
(4, 'WO-202611-002', 'NV006', 'NV002', '2026-11-22 08:45:00', '2026-11-22 09:00:00', '2026-11-22 14:00:00', 'ThoPhu', 2.00, 'DangLam'),
(5, 'WO-202611-003', 'NV004', 'NV002', '2026-11-18 07:45:00', '2026-11-18 08:05:00', '2026-11-18 09:25:00', 'ThoChinh', 1.33, 'HoanThanh'),
(6, 'WO-202611-004', 'NV005', 'NV002', '2026-11-20 12:30:00', '2026-11-20 13:15:00', '2026-11-20 15:50:00', 'ThoChinh', 2.58, 'HoanThanh'),
(7, 'WO-202611-004', 'NV006', 'NV002', '2026-11-20 12:30:00', '2026-11-20 13:15:00', '2026-11-20 15:50:00', 'ThoPhu', 2.58, 'HoanThanh'),
(8, 'WO-202611-005', 'NV003', 'NV002', '2026-11-24 11:00:00', '2026-11-24 13:30:00', '2026-11-24 16:30:00', 'ThoChinh', 0.00, 'DaGiao');

-- ============================================================
-- 22. BẢNG KET_QUA_CHECKLIST (8 kết quả đo kiểm thực tế)
-- ============================================================
INSERT INTO `KET_QUA_CHECKLIST` (`KetQuaId`, `LenhBaoTriId`, `HangMucId`, `GiaTriDoThucTe`, `KetLuan`, `NguoiKiemTraId`, `ThoiDiemKiemTra`, `GhiChu`) VALUES
(1, 'WO-202611-003', 4, 38.50, 'Dat', 'NV004', '2026-11-18 08:30:00', 'Nhiệt độ vỏ trục chính ổn định dưới ngưỡng 45 độ C'),
(2, 'WO-202611-003', 5, NULL, 'Dat', 'NV004', '2026-11-18 08:50:00', 'Đã vệ sinh sạch mạt phoi kẹt quanh tay gắp ATC'),
(3, 'WO-202611-004', 6, 42.00, 'Dat', 'NV005', '2026-11-20 14:00:00', 'Nhiệt độ dầu thủy lực bình thường'),
(4, 'WO-202611-004', 7, NULL, 'Dat', 'NV005', '2026-11-20 14:20:00', 'Cán ty ben sạch sẽ, không trầy xước rỉ dầu'),
(5, 'WO-202611-004', 8, 208.00, 'Dat', 'NV005', '2026-11-20 15:00:00', 'Áp lực bơm đạt 208 bar chuẩn thiết kế'),
(6, 'WO-202611-001', 1, 5.80, 'Dat', 'NV003', '2026-11-21 08:20:00', 'Áp suất khí nén cấp vào Spindle đạt 5.8 bar'),
(7, 'WO-202611-001', 2, NULL, 'Dat', 'NV003', '2026-11-21 08:30:00', 'Bình bôi trơn ray trượt đầy dầu VG68'),
(8, 'WO-202611-001', 9, 0.003, 'Dat', 'NV003', '2026-11-21 11:00:00', 'Độ đảo côn trục chính đo được 0.003mm (tiêu chuẩn <=0.005mm)');

-- ============================================================
-- 23. BẢNG BAO_CAO_SU_CO (5 báo cáo sự cố 1-1 với lệnh sự cố)
-- ============================================================
INSERT INTO `BAO_CAO_SU_CO` (`BaoCaoSuCoId`, `LenhBaoTriId`, `MaLoiChinhThucId`, `CoDungMay`, `ThoiDiemXayRa`, `ThoiDiemKhoiPhuc`, `MoTaHuHong`, `NguyenNhanGocRe`, `MucDoAnhHuongPhanTram`, `BienPhapKhacPhuc`, `NguoiLapBaoCaoId`) VALUES
(1, 'WO-202611-001', 'ERR-OVERHEAT', 1, '2026-11-21 07:20:00', '2026-11-21 11:20:00', 'Cháy vỡ vòng bi đỡ phía trước Spindle do thiếu bôi trơn lâu ngày', 'Đường ống dẫn mỡ bôi trơn bị tắc do cặn dầu khô cứng lâu ngày không vệ sinh', 100, 'Thay mới 2 vòng bi NSK 6205, thông rửa toàn bộ đường ống dẫn mỡ và bơm mỡ mới', 'NV003'),
(2, 'WO-202611-002', 'ERR-OIL-LEAK', 1, '2026-11-22 08:10:00', NULL, 'Rách gioăng phớt chắn dầu cổ xi lanh ép số 1', 'Mạt phoi kim loại rơi vào khe gạt bụi làm xước rách phớt cao su', 100, 'Đang tiến hành tháo ty ben ép và thay thế phớt chắn dầu NOK', 'NV005'),
(3, 'WO-202611-005', 'ERR-TOOL-JAM', 0, NULL, NULL, 'Kẹt phoi kim loại trong ổ khóa bi đài gá dao Turret', 'Phoi thép dài cuốn vào mặt sau đài dao trong chu trình tiện thô', 30, 'Vệ sinh phoi cơ học, kiểm tra lại áp lực thủy lực mở chốt khóa', 'NV003'),
(4, 'WO-202611-007', 'ERR-SENSOR', 1, '2026-11-15 08:45:00', '2026-11-15 11:15:00', 'Cảm biến tiệm cận trục X đứt ngầm dây tín hiệu', 'Dây cáp xích dẫn hướng bị kẹp dập khi chạy tốc độ cao', 100, 'Thay mới cảm biến Omron E2E và bọc lại xích luồn cáp', 'NV004'),
(5, 'WO-202611-008', 'ERR-INVERTER', 0, NULL, NULL, 'Biến tần báo quá nhiệt tản nhiệt quạt hút', 'Quạt làm mát biến tần tích tụ nhiều bụi bặm kim loại', 20, 'Vệ sinh cánh quạt tản nhiệt biến tần và tủ điều khiển', 'NV004');

-- ============================================================
-- 24. BẢNG PHIEU_CAP_NHAT_TON_KHO (5 phiếu cập nhật kho)
-- ============================================================
INSERT INTO `PHIEU_CAP_NHAT_TON_KHO` (`PhieuCapNhatId`, `LoaiCapNhat`, `MaPhieuKhoNgoai`, `ThoiDiemLap`, `NguoiLapId`, `NguoiXacNhanId`, `ThoiDiemXacNhan`, `TrangThaiPhieu`, `GhiChu`) VALUES
('CNK-202611-001', 'BoSungTuKhoNgoai', 'NK-TOTAL-2026-088', '2026-11-15 08:30:00', 'NV002', 'NV001', '2026-11-15 10:00:00', 'DaXacNhan', 'Bổ sung vòng bi và dầu thủy lực đợt 1 tháng 11'),
('CNK-202611-002', 'DieuChinhKiemKe', 'KK-T10-REPORT', '2026-11-16 09:00:00', 'NV002', 'NV001', '2026-11-16 11:30:00', 'DaXacNhan', 'Điều chỉnh chênh lệch kiểm kê định kỳ cuối tháng 10'),
('CNK-202611-003', 'BoSungTuKhoNgoai', 'NK-TOTAL-2026-095', '2026-11-20 14:00:00', 'NV002', 'NV001', '2026-11-20 16:30:00', 'DaXacNhan', 'Nhập bổ sung phớt NOK và lõi lọc dầu cho máy ép thủy lực'),
('CNK-202611-004', 'BoSungTuKhoNgoai', 'NK-TOTAL-2026-102', '2026-11-23 10:00:00', 'NV002', NULL, NULL, 'ChoXacNhan', 'Đề xuất nhập thêm 20 béc cắt plasma phục vụ đơn hàng mới'),
('CNK-202611-005', 'DieuChinhKiemKe', 'KK-DOTXUAT-11', '2026-11-24 08:30:00', 'NV002', NULL, NULL, 'ChoXacNhan', 'Điều chỉnh giảm 1 cảm biến tiệm cận Omron bị hỏng vỡ trong kho');

-- ============================================================
-- 25. BẢNG CHI_TIET_CAP_NHAT_TON_KHO (8 dòng chi tiết cập nhật)
-- ============================================================
INSERT INTO `CHI_TIET_CAP_NHAT_TON_KHO` (`ChiTietCapNhatId`, `PhieuCapNhatId`, `PhuTungId`, `SoLuongThayDoi`, `GhiChu`) VALUES
(1, 'CNK-202611-001', 'PT-VB-6205', 20.00, 'Nhập bổ sung từ kho tổng'),
(2, 'CNK-202611-001', 'PT-DAU-VG46', 200.00, 'Nhập 1 phuy dầu thủy lực 209L'),
(3, 'CNK-202611-002', 'PT-DAU-VG68', -5.00, 'Hao hụt rò rỉ van rót dầu khi kiểm kê'),
(4, 'CNK-202611-002', 'PT-RL-OMRON', 2.00, 'Kiểm đếm thừa 2 rơ le trong tủ dự phòng'),
(5, 'CNK-202611-003', 'PT-PHOT-NOK', 10.00, 'Bổ sung theo phiếu yêu cầu vật tư'),
(6, 'CNK-202611-003', 'PT-LOC-DAU', 5.00, 'Nhập lõi lọc thủy lực dự trữ'),
(7, 'CNK-202611-004', 'PT-BEC-PLASMA', 20.00, 'Đề xuất nhập béc cắt máy Hypertherm'),
(8, 'CNK-202611-005', 'PT-CB-OMRON', -1.00, 'Hỏng nứt vỏ cảm biến trong quá trình lưu kho');

-- ============================================================
-- 26. BẢNG PHIEU_DU_TRU_XUAT_DUNG (5 phiếu dự trù & xuất kho)
-- ============================================================
INSERT INTO `PHIEU_DU_TRU_XUAT_DUNG` (`PhieuXuatId`, `LenhBaoTriId`, `NguoiLapDuTruId`, `ThoiDiemLap`, `NguoiKhaiBaoId`, `ThoiDiemKhaiBao`, `NguoiDuyetVuotId`, `ThoiDiemDuyetVuot`, `NguoiXacNhanXuatId`, `ThoiDiemXacNhan`, `TrangThaiPhieu`, `GhiChu`) VALUES
('PX-202611-001', 'WO-202611-001', 'NV002', '2026-11-21 08:00:00', 'NV003', '2026-11-21 11:00:00', NULL, NULL, 'NV002', '2026-11-21 11:30:00', 'DaXacNhanTruTon', 'Đã xuất và trừ tồn 2 vòng bi NSK 6205 cho lệnh sửa máy tiện'),
('PX-202611-002', 'WO-202611-002', 'NV002', '2026-11-22 08:50:00', 'NV005', '2026-11-22 11:30:00', NULL, NULL, NULL, NULL, 'DaKhaiBaoThucDung', 'Dự trù 2 phớt NOK và 50L dầu thủy lực Shell VG46'),
('PX-202611-003', 'WO-202611-003', 'NV002', '2026-11-18 07:50:00', 'NV004', '2026-11-18 09:15:00', NULL, NULL, 'NV002', '2026-11-18 09:30:00', 'DaXacNhanTruTon', 'Xuất 2 lít dầu bôi trơn đường trượt Mobil Vactra 2'),
('PX-202611-004', 'WO-202611-004', 'NV002', '2026-11-20 12:45:00', 'NV005', '2026-11-20 15:40:00', NULL, NULL, 'NV002', '2026-11-20 16:00:00', 'DaXacNhanTruTon', 'Xuất 1 lõi lọc dầu thủy lực thay cho máy ép Komatsu'),
('PX-202611-005', 'WO-202611-005', 'NV002', '2026-11-24 11:15:00', NULL, NULL, NULL, NULL, NULL, NULL, 'DangDuTru', 'Dự trù 1 cảm biến tiệm cận Omron phòng ngừa chốt hãm hỏng');

-- ============================================================
-- 27. BẢNG CHI_TIET_PHIEU_XUAT (8 dòng chi tiết xuất vật tư)
-- ============================================================
INSERT INTO `CHI_TIET_PHIEU_XUAT` (`ChiTietId`, `PhieuXuatId`, `PhuTungId`, `SoLuongDuTru`, `SoLuongThucDung`, `GhiChuGiaiTrinh`) VALUES
(1, 'PX-202611-001', 'PT-VB-6205', 2.00, 2.00, 'Thay thế vòng bi cụm Spindle'),
(2, 'PX-202611-002', 'PT-PHOT-NOK', 2.00, 2.00, 'Thay phớt xi lanh dập thủy lực'),
(3, 'PX-202611-002', 'PT-DAU-VG46', 50.00, 45.00, 'Châm bù lượng dầu bị rò rỉ ra sàn'),
(4, 'PX-202611-003', 'PT-DAU-VG68', 2.00, 2.00, 'Châm thêm dầu bôi trơn ray trượt'),
(5, 'PX-202611-004', 'PT-LOC-DAU', 1.00, 1.00, 'Thay định kỳ lọc dầu đáy thùng'),
(6, 'PX-202611-005', 'PT-CB-OMRON', 1.00, NULL, 'Dự trù thay cảm biến vị trí đài dao'),
(7, 'PX-202611-001', 'PT-CUROA-5M', 1.00, 0.00, 'Kiểm tra dây curoa cũ còn tốt, chưa cần thay'),
(8, 'PX-202611-004', 'PT-DAU-VG46', 20.00, 20.00, 'Bổ sung dầu thủy lực máy ép Komatsu');

-- Kích hoạt lại ràng buộc khóa ngoại
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- HOÀN TẤT SEED DATA (TỔNG CỘNG HƠN 185 BẢN GHI CHO 27 BẢNG)
-- ============================================================
