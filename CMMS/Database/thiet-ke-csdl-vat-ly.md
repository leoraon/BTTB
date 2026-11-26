# TÀI LIỆU THIẾT KẾ CƠ SỞ DỮ LIỆU VẬT LÝ
## HỆ THỐNG QUẢN LÝ BẢO TRÌ THIẾT BỊ (CMMS - COMPUTERIZED MAINTENANCE MANAGEMENT SYSTEM)

---

## 1. TỔNG QUAN VÀ QUY CHUẨN THIẾT KẾ

### 1.1. Phạm vi & Mục tiêu
Tài liệu này đặc tả thiết kế Cơ sở dữ liệu (CSDL) vật lý cho Hệ thống Quản lý Bảo trì Thiết bị (CMMS) trong môi trường nhà máy gia công cơ khí. Thiết kế được tối ưu hóa cho hệ quản trị cơ sở dữ liệu **MySQL 8.0+**, hướng tới việc đảm bảo tính toàn vẹn dữ liệu, hiệu năng truy vấn, khả năng mở rộng và đáp ứng đầy đủ các quy trình bảo trì (sự cố và phòng ngừa), quản lý thiết bị, kho phụ tùng và nhân sự kỹ thuật.

### 1.2. Môi trường và Tham số Kỹ thuật
- **Hệ quản trị CSDL**: MySQL Server 8.0 hoặc cao hơn.
- **Storage Engine**: `InnoDB` (hỗ trợ giao dịch ACID, khóa ngoại `FOREIGN KEY`, row-level locking).
- **Character Set**: `utf8mb4` (hỗ trợ đầy đủ tiếng Việt có dấu, ký tự unicode mở rộng và biểu tượng).
- **Collation**: `utf8mb4_unicode_ci` (so sánh không phân biệt hoa/thường theo chuẩn quốc tế).
- **Quy tắc phân vùng thời gian**: Đồng bộ chuẩn UTC/GMT+7 (`DATETIME`, `DATE`).

### 1.3. Quy chuẩn Thiết kế Bắt buộc
1. **Chuẩn hóa bậc 3 (3NF)**:
   - Toàn bộ 27 bảng vật lý đạt chuẩn 3NF, loại bỏ hoàn toàn các phụ thuộc hàm từng phần và phụ thuộc hàm bắc cầu.
   - **Tuyệt đối không lưu trữ thuộc tính suy dẫn vật lý**:
     - `TongGioDungMay`: Được tính động từ `TIMESTAMPDIFF(MINUTE, ThoiDiemXayRa, ThoiDiemKhoiPhuc) / 60.0`.
     - `TonKhoKhaDung`: Được tính động từ `SoLuongTonHienCo - SoLuongDangDuTru`.
     - `TuoiThoMay`: Được tính động từ `DATEDIFF(CURRENT_DATE, NgayDuaVaoSuDung)`.
2. **Xử lý triệt để quan hệ Nhiều - Nhiều ($M-N$)**:
   - Toàn bộ quan hệ $M-N$ giữa các thực thể đều được phân rã thành các bảng trung gian (thực thể kết hợp) độc lập, có khóa chính (PK đơn lẻ hoặc Composite PK) và các khóa ngoại (FK) rõ ràng.
   - 7 bảng trung gian gồm: `VAI_TRO_QUYEN`, `CHUYEN_MON_KTV`, `PHAN_CONG_KTV`, `KET_QUA_CHECKLIST`, `DINH_MUC_LINH_KIEN`, `CHI_TIET_CAP_NHAT_TON_KHO`, `CHI_TIET_PHIEU_XUAT`.
3. **Quy tắc định danh (Naming Convention)**:
   - **Tên bảng**: Viết HOA toàn bộ bằng tiếng Việt không dấu, nối bằng dấu gạch dưới `_` (`UPPER_SNAKE_CASE`). Ví dụ: `NHAN_VIEN`, `THIET_BI`, `PHIEU_DU_TRU_XUAT_DUNG`.
   - **Tên thuộc tính (Cột)**: Viết hoa chữ cái đầu mỗi từ (`PascalCase`), có hậu tố `Id` cho khóa chính và khóa ngoại. Ví dụ: `NhanVienId`, `TenThietBi`, `ThoiGianDuKienBatDau`.

---

## 2. DANH MỤC 27 BẢNG VẬT LÝ THEO 6 PHÂN HỆ

| STT | Tên bảng vật lý | Tên nghiệp vụ tiếng Việt | Phân loại | Khóa chính (PK) |
| :---: | :--- | :--- | :---: | :--- |
| **Phân hệ 1** | **Hệ thống & Tổ chức** | | | |
| 1 | `NHAN_VIEN` | Danh sách Nhân viên & KTV | Master | `NhanVienId` (VARCHAR(20)) |
| 2 | `TAI_KHOAN` | Tài khoản Người dùng | Dependent (1-1) | `TaiKhoanId` (INT AUTO_INCREMENT) |
| 3 | `VAI_TRO` | Danh mục Vai trò Hệ thống | Lookup | `VaiTroId` (INT) |
| 4 | `VAI_TRO_QUYEN` | Phân quyền Vai trò | **Bảng trung gian (M-N)** | `(VaiTroId, MaQuyen)` |
| 5 | `NHAT_KY_HE_THONG` | Nhật ký Kiểm toán Thao tác | Log / Audit | `NhatKyId` (BIGINT AUTO_INCREMENT) |
| 6 | `THU_VIEN_BIEU_MAU` | Thư viện Biểu mẫu Quy trình | Lookup / Resource | `BieuMauId` (VARCHAR(30)) |
| 7 | `PHAN_XUONG_BO_PHAN` | Đơn vị Phân xưởng / Bộ phận | Recursive Master | `DonViId` (VARCHAR(30)) |
| **Phân hệ 2** | **Tài sản Thiết bị** | | | |
| 8 | `NHOM_THIET_BI` | Phân nhóm Thiết bị & Cụm máy | Lookup | `NhomThietBiId` (VARCHAR(30)) |
| 9 | `DANH_MUC_MA_LOI` | Danh mục Mã lỗi Chuẩn hóa | Lookup | `MaLoiId` (VARCHAR(20)) |
| 10 | `THIET_BI` | Hồ sơ Lý lịch Máy móc | Recursive Master | `ThietBiId` (VARCHAR(30)) |
| 11 | `THONG_SO_THIET_BI` | Chỉ số Kỹ thuật Thiết bị | Detail | `ThongSoId` (BIGINT AUTO_INCREMENT) |
| **Phân hệ 3** | **Bảo trì Sự cố (Corrective Maintenance)** | | | |
| 12 | `CAU_HINH_MUC_UU_TIEN` | Cấu hình SLA & Mức ưu tiên | Configuration | `MucUuTienId` (INT) |
| 13 | `YEU_CAU_BAO_TRI` | Phiếu Yêu cầu Báo hỏng (BM-02) | Transaction | `YeuCauId` (VARCHAR(30)) |
| **Phân hệ 4** | **Bảo trì Phòng ngừa (Preventive Maintenance)** | | | |
| 14 | `MAU_CHECKLIST` | Mẫu Quy trình Checklist (BM-03 M) | Master Template | `MauChecklistId` (VARCHAR(30)) |
| 15 | `HANG_MUC_CHECKLIST` | Hạng mục Kiểm tra Checklist (BM-03 D)| Detail Item | `HangMucId` (INT AUTO_INCREMENT) |
| 16 | `KE_HOACH_BAO_TRI` | Kế hoạch Bảo trì Định kỳ | Master Schedule | `KeHoachId` (VARCHAR(30)) |
| **Phân hệ 5** | **Lệnh & Quá trình Thực hiện Bảo trì** | | | |
| 17 | `LENH_BAO_TRI` | Lệnh Bảo trì Trung tâm (BM-04 & 06) | Transaction Core | `LenhBaoTriId` (VARCHAR(30)) |
| 18 | `CHUYEN_MON_KTV` | Hồ sơ Năng lực Chuyên môn KTV | **Bảng trung gian (M-N)** | `(NhanVienId, NhomThietBiId)` |
| 19 | `PHAN_CONG_KTV` | Phân công Kỹ thuật viên | **Bảng trung gian (M-N)** | `PhanCongId` (BIGINT AUTO_INCREMENT) |
| 20 | `KET_QUA_CHECKLIST` | Kết quả Thực hiện Checklist | **Bảng trung gian (M-N)** | `KetQuaId` (BIGINT AUTO_INCREMENT) |
| 21 | `BAO_CAO_SU_CO` | Báo cáo Phân tích Sự cố Gốc | Dependent (1-1) | `BaoCaoSuCoId` (BIGINT AUTO_INCREMENT) |
| **Phân hệ 6** | **Quản lý Kho Phụ tùng & Hiện vật** | | | |
| 22 | `PHU_TUNG` | Danh mục Phụ tùng & Tồn kho Hiện vật | Master | `PhuTungId` (VARCHAR(30)) |
| 23 | `DINH_MUC_LINH_KIEN` | Định mức Linh kiện theo Máy | **Bảng trung gian (M-N)** | `DinhMucId` (BIGINT AUTO_INCREMENT) |
| 24 | `PHIEU_CAP_NHAT_TON_KHO` | Phiếu Cập nhật Tồn kho (Nhập/Kiểm kê)| Transaction Master | `PhieuCapNhatId` (VARCHAR(30)) |
| 25 | `CHI_TIET_CAP_NHAT_TON_KHO`| Chi tiết Dòng Cập nhật Kho | **Bảng trung gian (M-N)** | `ChiTietCapNhatId` (BIGINT AUTO_INCREMENT) |
| 26 | `PHIEU_DU_TRU_XUAT_DUNG` | Phiếu Dự trù & Xuất dùng Phụ tùng (BM-05)| Transaction Master | `PhieuXuatId` (VARCHAR(30)) |
| 27 | `CHI_TIET_PHIEU_XUAT` | Chi tiết Xuất Phụ tùng Thực tế | **Bảng trung gian (M-N)** | `ChiTietId` (BIGINT AUTO_INCREMENT) |

---

## 3. ĐẶC TẢ CHI TIẾT CẤU TRÚC 27 BẢNG VẬT LÝ

### Phân hệ 1: Hệ thống & Tổ chức

#### 1. Bảng `NHAN_VIEN`
Lưu trữ thông tin cán bộ công nhân viên liên quan trong nhà máy và hồ sơ kỹ thuật của kỹ thuật viên bảo trì.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `NhanVienId` | VARCHAR | 20 | **PK** | NOT NULL | Mã định danh nhân viên (VD: NV001, KTV001) |
| `HoTen` | VARCHAR | 100 | | NOT NULL | Họ và tên đầy đủ |
| `SoDienThoai` | VARCHAR | 15 | | NOT NULL | Số điện thoại liên lạc |
| `Email` | VARCHAR | 100 | | NOT NULL, UNIQUE | Địa chỉ email công việc |
| `DonViId` | VARCHAR | 30 | **FK** | NOT NULL | Mã phân xưởng/phòng ban trực thuộc $\rightarrow$ `PHAN_XUONG_BO_PHAN(DonViId)` |
| `ChucVu` | VARCHAR | 50 | | NOT NULL | Chức danh công tác (Kỹ thuật viên, Tổ trưởng, Quản đốc...) |
| `ChuyenNganh` | VARCHAR | 50 | | NULL | Chuyên ngành kỹ thuật (*Cơ khí, Điện, Thủy lực, Tự động hóa...* - dùng cho KTV) |
| `BacTho` | VARCHAR | 20 | | NULL | Bậc thợ kỹ thuật (VD: 3/7, 5/7, 7/7 - dùng cho KTV) |
| `CaLamViec` | VARCHAR | 20 | | NULL, CHECK (`CaLamViec` IN ('Ca 1', 'Ca 2', 'Ca 3', 'HanhChinh')) | Ca làm việc phân công mặc định |
| `TrangThaiSanSang`| VARCHAR | 20 | | NULL, CHECK (`TrangThaiSanSang` IN ('SanSang', 'DangBan', 'NghiPhep')) | Trạng thái sẵn sàng tiếp nhận lệnh (KTV) |
| `TrangThai` | VARCHAR | 20 | | NOT NULL, DEFAULT 'DangLamViec', CHECK (`TrangThai` IN ('DangLamViec', 'DaNghiViec')) | Trạng thái nhân sự (không xóa để bảo toàn lịch sử) |

- **Foreign Keys**:
  - `FK_NhanVien_DonVi`: `DonViId` REFERENCES `PHAN_XUONG_BO_PHAN(DonViId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 2. Bảng `TAI_KHOAN`
Quản lý tài khoản đăng nhập người dùng, cơ chế xác thực và trạng thái kích hoạt tài khoản.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `TaiKhoanId` | INT | - | **PK** | NOT NULL, AUTO_INCREMENT | Mã định danh tài khoản tự tăng |
| `NhanVienId` | VARCHAR | 20 | **FK** | NOT NULL, UNIQUE | Liên kết duy nhất 1 - 0..1 với nhân viên $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `TenDangNhap` | VARCHAR | 50 | | NOT NULL, UNIQUE | Tên đăng nhập duy nhất trên hệ thống |
| `MatKhauHash` | VARCHAR | 255 | | NOT NULL | Mật khẩu đã băm bằng thuật toán an toàn (BCrypt/Argon2) |
| `VaiTroId` | INT | - | **FK** | NOT NULL | Vai trò phân quyền chính $\rightarrow$ `VAI_TRO(VaiTroId)` |
| `TrangThaiTaiKhoan`| VARCHAR | 20 | | NOT NULL, DEFAULT 'ChoDuyet', CHECK (`TrangThaiTaiKhoan` IN ('ChoDuyet', 'DangHoatDong', 'TuChoi', 'DangKhoa')) | Trạng thái tài khoản |
| `SoLanDangNhapSai`| INT | - | | NOT NULL, DEFAULT 0 | Đếm số lần đăng nhập sai liên tiếp |
| `ThoiDiemMoKhoaTam`| DATETIME | - | | NULL | Thời điểm tài khoản tự động hết hạn khóa tạm thời |
| `NgayTao` | DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm khởi tạo tài khoản |
| `NguoiDuyetId` | VARCHAR | 20 | **FK** | NULL | Quản lý nhà máy phê duyệt kích hoạt $\rightarrow$ `NHAN_VIEN(NhanVienId)` |

- **Foreign Keys**:
  - `FK_TaiKhoan_NhanVien`: `NhanVienId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_TaiKhoan_VaiTro`: `VaiTroId` REFERENCES `VAI_TRO(VaiTroId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_TaiKhoan_NguoiDuyet`: `NguoiDuyetId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

#### 3. Bảng `VAI_TRO`
Danh mục 5 vai trò cố định của hệ thống CMMS.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `VaiTroId` | INT | - | **PK** | NOT NULL | Mã vai trò (1: QuanTri, 2: QuanLyNhaMay, 3: TruongBPBaoTri, 4: KyThuatVien, 5: NguoiDungThietBi) |
| `TenVaiTro` | VARCHAR | 50 | | NOT NULL, UNIQUE | Tên gọi chuẩn của vai trò |
| `MoTa` | VARCHAR | 255 | | NULL | Mô tả nhiệm vụ, thẩm quyền và trách nhiệm |

---

#### 4. Bảng `VAI_TRO_QUYEN`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Vai trò và Mã quyền chức năng hệ thống.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `VaiTroId` | INT | - | **PK, FK** | NOT NULL | Mã vai trò $\rightarrow$ `VAI_TRO(VaiTroId)` |
| `MaQuyen` | VARCHAR | 50 | **PK** | NOT NULL | Mã quyền Enum quản lý tại tầng ứng dụng (`DEVICE_VIEW`, `WO_CREATE`, `STOCK_ADJUST`...) |

- **Primary Key**: `PRIMARY KEY (VaiTroId, MaQuyen)`.
- **Foreign Keys**:
  - `FK_VaiTroQuyen_VaiTro`: `VaiTroId` REFERENCES `VAI_TRO(VaiTroId)` ON DELETE CASCADE ON UPDATE CASCADE.

---

#### 5. Bảng `NHAT_KY_HE_THONG`
Lưu trữ nhật ký kiểm toán (Audit Trail) các biến động và thao tác dữ liệu trọng yếu.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `NhatKyId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Mã nhật ký tự tăng |
| `TaiKhoanId` | INT | - | **FK** | NULL | Tài khoản thực hiện thao tác $\rightarrow$ `TAI_KHOAN(TaiKhoanId)` (NULL nếu đăng nhập thất bại) |
| `ThoiDiem` | DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm phát sinh sự kiện |
| `LoaiThaoTac` | VARCHAR | 50 | | NOT NULL | Thao tác (DangNhap, DangNhapThatBai, DangXuat, KhoaTaiKhoan, XacNhanBoSungTonKho, DieuChinhKiemKe...) |
| `DoiTuongTacDong`| VARCHAR | 100 | | NULL | Mã bản ghi hoặc số chứng từ bị tác động (VD: WO-202611-001, PT001...) |
| `MoTaChiTiet` | TEXT | - | | NOT NULL | Nội dung chi tiết thao tác, tham số thay đổi |

- **Foreign Keys**:
  - `FK_NhatKy_TaiKhoan`: `TaiKhoanId` REFERENCES `TAI_KHOAN(TaiKhoanId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

#### 6. Bảng `THU_VIEN_BIEU_MAU`
Quản lý danh mục 6 biểu mẫu nghiệp vụ tiêu chuẩn (BM-01 đến BM-06) và đường dẫn file mẫu phục vụ tải về.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `BieuMauId` | VARCHAR | 30 | **PK** | NOT NULL | Mã định danh biểu mẫu chuẩn (VD: BM-01 đến BM-06) |
| `TenBieuMau` | VARCHAR | 150 | | NOT NULL | Tên chuẩn hóa của biểu mẫu quy trình |
| `LoaiBieuMau` | VARCHAR | 50 | | NOT NULL, CHECK (`LoaiBieuMau` IN ('LyLichThietBi', 'BaoHong', 'Checklist', 'LenhSuaChua', 'DuTruXuatDung', 'NghiemThuBanGiao')) | Phân loại biểu mẫu theo giai đoạn nghiệp vụ |
| `DinhDangTep` | VARCHAR | 10 | | NOT NULL, CHECK (`DinhDangTep` IN ('DOCX', 'PDF')) | Định dạng tệp tin cho phép tải |
| `DuongDanTep` | VARCHAR | 255 | | NOT NULL | Đường dẫn lưu trữ vật lý trên server |
| `MoTa` | VARCHAR | 255 | | NULL | Hướng dẫn tóm tắt mục đích sử dụng biểu mẫu |

---

#### 7. Bảng `PHAN_XUONG_BO_PHAN`
Quản lý cơ cấu phòng ban và phân cấp vị trí lắp đặt máy móc (Mô hình cây phân cấp đệ quy cha - con).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `DonViId` | VARCHAR | 30 | **PK** | NOT NULL | Mã phân xưởng, bộ phận, dây chuyền (VD: PX-COKHI, DC-CNC01) |
| `TenDonVi` | VARCHAR | 100 | | NOT NULL | Tên gọi phân xưởng/dây chuyền/bộ phận |
| `DonViChaId` | VARCHAR | 30 | **FK** | NULL | Mã đơn vị cấp cha đệ quy $\rightarrow$ `PHAN_XUONG_BO_PHAN(DonViId)` |
| `LoaiDonVi` | VARCHAR | 30 | | NOT NULL, CHECK (`LoaiDonVi` IN ('PhanXuong', 'DayChuyen', 'BoPhanQuanLy')) | Cấp bậc phân loại đơn vị |
| `NguoiPhuTrachId`| VARCHAR | 20 | **FK** | NULL | Quản đốc hoặc Trưởng phòng phụ trách $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `MoTa` | VARCHAR | 255 | | NULL | Ghi chú vị trí hoặc chức năng |

- **Foreign Keys**:
  - `FK_PhanXuong_Cha`: `DonViChaId` REFERENCES `PHAN_XUONG_BO_PHAN(DonViId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_PhanXuong_NguoiPhuTrach`: `NguoiPhuTrachId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

### Phân hệ 2: Tài sản Thiết bị

#### 8. Bảng `NHOM_THIET_BI`
Phân nhóm dòng máy móc chính và các loại cụm chi tiết con.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `NhomThietBiId`| VARCHAR | 30 | **PK** | NOT NULL | Mã nhóm thiết bị (VD: NHOM-TIEN, NHOM-PHAY, CUM-TRUCCHINH) |
| `TenNhom` | VARCHAR | 100 | | NOT NULL | Tên nhóm máy hoặc nhóm cụm con |
| `LaCumChiTiet` | TINYINT | 1 | | NOT NULL, DEFAULT 0 | 0: Nhóm máy chính độc lập; 1: Nhóm cụm chi tiết con |
| `MoTa` | VARCHAR | 255 | | NULL | Mô tả đặc tính kỹ thuật chung của nhóm |

---

#### 9. Bảng `DANH_MUC_MA_LOI`
Danh mục mã hóa sự cố hư hỏng chuẩn hóa hỗ trợ tra cứu nhanh và thống kê Pareto sự cố.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `MaLoiId` | VARCHAR | 20 | **PK** | NOT NULL | Mã chuẩn sự cố (VD: ERR-OVERHEAT, ERR-VIBRATION, ERR-LEAK) |
| `TenLoaiSuCo` | VARCHAR | 100 | | NOT NULL | Tên gọi chuẩn của loại sự cố (Quá nhiệt, Rung lắc, Rò rỉ dầu...) |
| `MoTaDauHieu` | VARCHAR | 255 | | NOT NULL | Dấu hiệu nhận biết hiện tượng bất thường bên ngoài |

---

#### 10. Bảng `THIET_BI`
Hồ sơ lý lịch tài sản thiết bị, hỗ trợ quan hệ máy cha - cụm chi tiết con tối đa 2 cấp (đệ quy).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `ThietBiId` | VARCHAR | 30 | **PK** | NOT NULL | Mã thiết bị duy nhất (VD: TB-CNC-001, CUM-SPINDLE-01) |
| `TenThietBi` | VARCHAR | 150 | | NOT NULL | Tên thiết bị hoặc cụm chi tiết máy |
| `ThietBiChaId` | VARCHAR | 30 | **FK** | NULL | Mã thiết bị cha đệ quy $\rightarrow$ `THIET_BI(ThietBiId)` (tối đa 2 cấp) |
| `NhomThietBiId`| VARCHAR | 30 | **FK** | NOT NULL | Thuộc nhóm thiết bị $\rightarrow$ `NHOM_THIET_BI(NhomThietBiId)` |
| `ViTriLapDatId`| VARCHAR | 30 | **FK** | NOT NULL | Vị trí phân xưởng đặt máy $\rightarrow$ `PHAN_XUONG_BO_PHAN(DonViId)` |
| `BoPhanQuanLyId`| VARCHAR | 30 | **FK** | NOT NULL | Bộ phận trực tiếp quản lý/vận hành $\rightarrow$ `PHAN_XUONG_BO_PHAN(DonViId)` |
| `Model` | VARCHAR | 50 | | NOT NULL | Model máy từ hãng sản xuất |
| `Serial` | VARCHAR | 50 | | NOT NULL, UNIQUE | Số serial máy duy nhất |
| `NhaSanXuat` | VARCHAR | 100 | | NOT NULL | Hãng chế tạo thiết bị |
| `NgayDuaVaoSuDung`| DATE | - | | NOT NULL | Mốc bắt đầu đưa vào sử dụng (dùng tính tuổi máy động) |
| `ChuKyBaoTriKhuyenNghiNgay`| INT | - | | NOT NULL, DEFAULT 30 | Chu kỳ bảo dưỡng định kỳ khuyến nghị (số ngày) |
| `CongDungMoTa` | TEXT | - | | NOT NULL | Mô tả công dụng và vị trí trong dây chuyền sản xuất |
| `LuuYAnToan` | TEXT | - | | NULL | Quy định an toàn vận hành đặc thù |
| `TrangThaiVanHanh`| VARCHAR | 30 | | NOT NULL, DEFAULT 'DangHoatDong', CHECK (`TrangThaiVanHanh` IN ('DangHoatDong', 'DungDoSuCo', 'DangBaoTri', 'DuPhong', 'NgungSuDung')) | Trạng thái máy hiện tại |
| `DuongDanAnh` | VARCHAR | 255 | | NULL | Đường dẫn file ảnh chụp máy |
| `DuongDanTaiLieu`| VARCHAR | 255 | | NULL | Đường dẫn tệp sổ tay hướng dẫn kỹ thuật PDF |

- **Foreign Keys**:
  - `FK_ThietBi_Cha`: `ThietBiChaId` REFERENCES `THIET_BI(ThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_ThietBi_Nhom`: `NhomThietBiId` REFERENCES `NHOM_THIET_BI(NhomThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_ThietBi_ViTri`: `ViTriLapDatId` REFERENCES `PHAN_XUONG_BO_PHAN(DonViId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_ThietBi_BoPhan`: `BoPhanQuanLyId` REFERENCES `PHAN_XUONG_BO_PHAN(DonViId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 11. Bảng `THONG_SO_THIET_BI`
Lưu trữ động các thông số kỹ thuật đa dạng của máy móc (Form Master - Detail 2).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `ThongSoId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `ThietBiId` | VARCHAR | 30 | **FK** | NOT NULL | Thiết bị sở hữu $\rightarrow$ `THIET_BI(ThietBiId)` |
| `TenThongSo` | VARCHAR | 100 | | NOT NULL | Tên chỉ số kỹ thuật (Công suất, Điện áp, Tốc độ quay, Tải trọng...) |
| `GiaTri` | VARCHAR | 100 | | NOT NULL | Giá trị thông số |
| `DonViDo` | VARCHAR | 30 | | NULL | Đơn vị đo kỹ thuật (kW, rpm, bar, mm, kg...) |
| `MoTa` | VARCHAR | 255 | | NULL | Ghi chú hoặc điều kiện đo |

- **Foreign Keys**:
  - `FK_ThongSo_ThietBi`: `ThietBiId` REFERENCES `THIET_BI(ThietBiId)` ON DELETE CASCADE ON UPDATE CASCADE.

---

### Phân hệ 3: Bảo trì Sự cố

#### 12. Bảng `CAU_HINH_MUC_UU_TIEN`
Cấu hình cam kết dịch vụ (SLA) và hiển thị trực quan các mức ưu tiên xử lý sự cố.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `MucUuTienId` | INT | - | **PK** | NOT NULL | Mã mức ưu tiên (1: Khẩn cấp, 2: Cao, 3: Trung bình, 4: Thấp) |
| `TenMucUuTien` | VARCHAR | 30 | | NOT NULL, UNIQUE | Tên gọi mức ưu tiên |
| `ThuTuUuTien` | INT | - | | NOT NULL, UNIQUE | Thứ tự sắp xếp điều phối công việc (1 $\rightarrow$ 4) |
| `MaMauHienThi` | VARCHAR | 20 | | NOT NULL | Mã màu HEX giao diện (#FF0000, #FFA500, #FFFF00, #008000) |
| `ThoiHanPhanHoiPhut`| INT | - | | NOT NULL | Thời hạn SLA phản hồi theo phút (30, 120, 480, 1440) |

---

#### 13. Bảng `YEU_CAU_BAO_TRI`
Phiếu tiếp nhận báo hỏng sự cố từ người dùng vận hành thiết bị (Biểu mẫu BM-02).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `YeuCauId` | VARCHAR | 30 | **PK** | NOT NULL | Số hiệu phiếu yêu cầu báo hỏng (VD: YC-202611-001) |
| `ThietBiId` | VARCHAR | 30 | **FK** | NOT NULL | Thiết bị gặp sự cố $\rightarrow$ `THIET_BI(ThietBiId)` |
| `NguoiBaoId` | VARCHAR | 20 | **FK** | NOT NULL | Người phát hiện lập báo hỏng $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemGui` | DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm gửi phiếu báo hỏng |
| `MoTaTrieuChung`| TEXT | - | | NOT NULL | Chi tiết dấu hiệu/triệu chứng sự cố quan sát được |
| `DuongDanAnhSuCo`| VARCHAR | 255 | | NULL | Ảnh chụp hiện trường sự cố |
| `MayDangDung` | TINYINT | 1 | | NOT NULL, DEFAULT 0 | Cờ đánh dấu máy có đang bị dừng hoạt động hay không |
| `MucUuTienDeXuatId`| INT | - | **FK** | NOT NULL | Mức ưu tiên do người báo đề xuất $\rightarrow$ `CAU_HINH_MUC_UU_TIEN(MucUuTienId)` |
| `MucUuTienChinhThucId`| INT | - | **FK** | NULL | Mức ưu tiên do Trưởng BP chốt duyệt $\rightarrow$ `CAU_HINH_MUC_UU_TIEN(MucUuTienId)` |
| `HanPhanHoi` | DATETIME | - | | NOT NULL | Mốc hạn chót phản hồi cố định (`ThoiDiemGui + ThoiHanPhanHoiPhut`) |
| `TrangThaiYeuCau`| VARCHAR | 30 | | NOT NULL, DEFAULT 'ChoTiepNhan', CHECK (`TrangThaiYeuCau` IN ('ChoTiepNhan', 'DaPheDuyet', 'TuChoi', 'DangXuLy', 'HoanThanh', 'DaHuy')) | Trạng thái tiếp nhận phiếu |
| `MaLoiId` | VARCHAR | 20 | **FK** | NULL | Phân loại mã lỗi sơ bộ $\rightarrow$ `DANH_MUC_MA_LOI(MaLoiId)` |
| `NhanDinhNguyenNhanSoBo`| TEXT | - | | NULL | Nhận định ban đầu của Trưởng BP bảo trì |
| `HuongXuLy` | VARCHAR | 50 | | NULL, CHECK (`HuongXuLy` IN ('SuaChuaKhanCap', 'SuaChuaTheoKeHoach')) | Hướng khắc phục |
| `NguoiDuyetId` | VARCHAR | 20 | **FK** | NULL | Trưởng BP phê duyệt/từ chối $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemDuyet`| DATETIME | - | | NULL | Thời điểm thực tế bấm duyệt (đối soát SLA) |
| `LyDoTuChoi` | VARCHAR | 255 | | NULL | Bắt buộc giải trình khi từ chối yêu cầu |

- **Foreign Keys**:
  - `FK_YeuCau_ThietBi`: `ThietBiId` REFERENCES `THIET_BI(ThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_YeuCau_NguoiBao`: `NguoiBaoId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_YeuCau_UuTienDeXuat`: `MucUuTienDeXuatId` REFERENCES `CAU_HINH_MUC_UU_TIEN(MucUuTienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_YeuCau_UuTienChinhThuc`: `MucUuTienChinhThucId` REFERENCES `CAU_HINH_MUC_UU_TIEN(MucUuTienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_YeuCau_MaLoi`: `MaLoiId` REFERENCES `DANH_MUC_MA_LOI(MaLoiId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_YeuCau_NguoiDuyet`: `NguoiDuyetId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

### Phân hệ 4: Bảo trì Phòng ngừa

#### 14. Bảng `MAU_CHECKLIST`
Mẫu tiêu chuẩn quy trình kiểm tra bảo dưỡng định kỳ theo nhóm máy (BM-03 Master - Form Master - Detail 1).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `MauChecklistId`| VARCHAR | 30 | **PK** | NOT NULL | Mã mẫu checklist chuẩn (VD: CK-TIEN-HANGNGAY, CK-CNC-HANGTHANG) |
| `TenMau` | VARCHAR | 150 | | NOT NULL | Tên chuẩn hóa của mẫu checklist |
| `NhomThietBiId`| VARCHAR | 30 | **FK** | NOT NULL | Nhóm máy áp dụng $\rightarrow$ `NHOM_THIET_BI(NhomThietBiId)` |
| `MoTaTieuChuan`| TEXT | - | | NULL | Hướng dẫn tiêu chuẩn kỹ thuật thực hiện |
| `TrangThaiHieuLuc`| VARCHAR | 20 | | NOT NULL, DEFAULT 'DangApDung', CHECK (`TrangThaiHieuLuc` IN ('DangApDung', 'NgungApDung')) | Trạng thái hiệu lực (không xóa khi đã có lịch sử) |

- **Foreign Keys**:
  - `FK_MauChecklist_Nhom`: `NhomThietBiId` REFERENCES `NHOM_THIET_BI(NhomThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 15. Bảng `HANG_MUC_CHECKLIST`
Các hạng mục kiểm tra chi tiết và ngưỡng đo tiêu chuẩn của mẫu checklist (BM-03 Detail).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `HangMucId` | INT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `MauChecklistId`| VARCHAR | 30 | **FK** | NOT NULL | Thuộc mẫu checklist $\rightarrow$ `MAU_CHECKLIST(MauChecklistId)` |
| `NhomBoPhan` | VARCHAR | 100 | | NOT NULL | Cụm bộ phận máy cần kiểm tra (Hệ thủy lực, Động cơ, Băng máy...) |
| `NoiDungCongViec`| VARCHAR | 255 | | NOT NULL | Thao tác kiểm tra cụ thể |
| `KieuKetQua` | VARCHAR | 20 | | NOT NULL, CHECK (`KieuKetQua` IN ('Dat_KhongDat', 'NhapSoDo')) | Hình thức ghi nhận kết quả kiểm tra |
| `TieuChuanYeuCau`| VARCHAR | 255 | | NOT NULL | Tiêu chuẩn kỹ thuật định mức đạt |
| `GiaTriMin` | DECIMAL | 12,2 | | NULL | Ngưỡng dưới cho phép (với kiểu NhapSoDo) |
| `GiaTriMax` | DECIMAL | 12,2 | | NULL | Ngưỡng trên cho phép (với kiểu NhapSoDo) |
| `DonViDo` | VARCHAR | 30 | | NULL | Đơn vị đo lường (Độ C, bar, mm, rpm...) |
| `ThuTuThucHien`| INT | - | | NOT NULL, DEFAULT 1 | Thứ tự các bước thực hiện |

- **Foreign Keys**:
  - `FK_HangMuc_MauChecklist`: `MauChecklistId` REFERENCES `MAU_CHECKLIST(MauChecklistId)` ON DELETE CASCADE ON UPDATE CASCADE.

---

#### 16. Bảng `KE_HOACH_BAO_TRI`
Quản lý kế hoạch bảo trì định kỳ chu kỳ lặp cố định theo từng thiết bị cụ thể.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `KeHoachId` | VARCHAR | 30 | **PK** | NOT NULL | Mã định danh kế hoạch (VD: KH-TB001-Q1) |
| `TenKeHoach` | VARCHAR | 150 | | NOT NULL | Tên kế hoạch bảo trì định kỳ |
| `ThietBiId` | VARCHAR | 30 | **FK** | NOT NULL | Thiết bị áp dụng $\rightarrow$ `THIET_BI(ThietBiId)` |
| `MauChecklistId`| VARCHAR | 30 | **FK** | NOT NULL | Mẫu checklist thực hiện $\rightarrow$ `MAU_CHECKLIST(MauChecklistId)` |
| `DonViChuKy` | VARCHAR | 20 | | NOT NULL, CHECK (`DonViChuKy` IN ('Ngay', 'Tuan', 'Thang', 'Quy', 'Nam')) | Đơn vị đo chu kỳ |
| `GiaTriChuKy` | INT | - | | NOT NULL | Giá trị chu kỳ cố định (VD: 30 ngày, 3 tháng) |
| `SoNgaySinhLenhTruoc`| INT | - | | NOT NULL, DEFAULT 3 | Số ngày hệ thống tự động sinh Work Order trước hạn |
| `ThoiGianDuKienPhut`| INT | - | | NOT NULL, DEFAULT 60 | Thời lượng dự kiến thực hiện (phút) |
| `NgayBatDauApDung`| DATE | - | | NOT NULL | Ngày bắt đầu áp dụng kế hoạch |
| `NgayHoanThanhGanNhat`| DATE | - | | NULL | Mốc ngày hoàn tất kỳ bảo trì gần nhất |
| `NgayDenHanTiepTheo`| DATE | - | | NOT NULL | Mốc ngày đến hạn kỳ tiếp theo (tự tính khi đóng lệnh kỳ trước) |
| `TrangThaiKeHoach`| VARCHAR | 30 | | NOT NULL, DEFAULT 'DangApDung', CHECK (`TrangThaiKeHoach` IN ('DangApDung', 'TamDung', 'NgungApDung')) | Trạng thái vận hành kế hoạch |
| `MoTa` | VARCHAR | 255 | | NULL | Ghi chú điều kiện vận hành đặc biệt |

- **Foreign Keys**:
  - `FK_KeHoach_ThietBi`: `ThietBiId` REFERENCES `THIET_BI(ThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_KeHoach_MauChecklist`: `MauChecklistId` REFERENCES `MAU_CHECKLIST(MauChecklistId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

### Phân hệ 5: Lệnh & Quá trình Bảo trì

#### 17. Bảng `LENH_BAO_TRI`
Thực thể giao dịch trung tâm điều phối toàn bộ các hoạt động sửa chữa sự cố và bảo dưỡng định kỳ (BM-04 & BM-06).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `LenhBaoTriId` | VARCHAR | 30 | **PK** | NOT NULL | Số hiệu lệnh bảo trì (VD: WO-202611-001) |
| `ThietBiId` | VARCHAR | 30 | **FK** | NOT NULL | Thiết bị cần bảo trì $\rightarrow$ `THIET_BI(ThietBiId)` |
| `LoaiLenh` | VARCHAR | 30 | | NOT NULL, CHECK (`LoaiLenh` IN ('BaoTriSuCo', 'BaoTriPhongNgua')) | Nguồn gốc phát sinh lệnh |
| `YeuCauId` | VARCHAR | 30 | **FK** | NULL | Liên kết báo hỏng $\rightarrow$ `YEU_CAU_BAO_TRI(YeuCauId)` (nếu là sự cố) |
| `KeHoachId` | VARCHAR | 30 | **FK** | NULL | Liên kết kế hoạch $\rightarrow$ `KE_HOACH_BAO_TRI(KeHoachId)` (nếu là phòng ngừa) |
| `NoiDungCongViec`| TEXT | - | | NOT NULL | Nội dung chi tiết các hạng mục sửa chữa/bảo dưỡng |
| `MucUuTienId` | INT | - | **FK** | NOT NULL | Mức ưu tiên xử lý lệnh $\rightarrow$ `CAU_HINH_MUC_UU_TIEN(MucUuTienId)` |
| `ThoiGianDuKienBatDau`| DATETIME | - | | NOT NULL | Thời điểm dự kiến bắt đầu |
| `ThoiGianDuKienKetThuc`| DATETIME | - | | NOT NULL | Mốc dự kiến xong mới nhất (chốt làm mốc đánh giá SLA đúng hạn) |
| `ThoiDiemThucTeBatDau`| DATETIME | - | | NULL | Thời điểm thực tế KTV bấm bắt đầu làm việc |
| `ThoiDiemThucTeKetThuc`| DATETIME | - | | NULL | Thời điểm thực tế KTV bấm hoàn thành |
| `LyDoTamDung` | VARCHAR | 255 | | NULL | Lý do giải trình khi tạm dừng lệnh |
| `TrangThaiLenh` | VARCHAR | 30 | | NOT NULL, DEFAULT 'MoiTao', CHECK (`TrangThaiLenh` IN ('MoiTao', 'DaPhanCong', 'DangThucHien', 'TamDung', 'DaHoanThanh', 'DaNghiemThu', 'DaDong', 'DaHuy')) | Trạng thái vòng đời lệnh bảo trì |
| `TinhTrangSauBaoTri`| VARCHAR | 30 | | NULL, CHECK (`TinhTrangSauBaoTri` IN ('DatChuan', 'CanTheoDoi')) | Đánh giá chất lượng sau bảo dưỡng |
| `NguoiBanGiaoId`| VARCHAR | 20 | **FK** | NULL | KTV đại diện bàn giao thiết bị $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `NguoiKyNhanId` | VARCHAR | 20 | **FK** | NULL | Tổ trưởng/Người vận hành ký nhận bàn giao $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemNghiemThu`| DATETIME | - | | NULL | Mốc thời gian ký nhận bàn giao |
| `NhanXetNghiemThu`| TEXT | - | | NULL | Ý kiến nhận xét nghiệm thu |
| `NguoiTaoId` | VARCHAR | 20 | **FK** | NULL | Trưởng BP tạo lệnh $\rightarrow$ `NHAN_VIEN(NhanVienId)` (NULL nếu tự sinh) |
| `NguoiDuyetDongId`| VARCHAR | 20 | **FK** | NULL | Trưởng BP duyệt đóng lệnh $\rightarrow$ `NHAN_VIEN(NhanVienId)` |

- **Foreign Keys**:
  - `FK_Lenh_ThietBi`: `ThietBiId` REFERENCES `THIET_BI(ThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_Lenh_YeuCau`: `YeuCauId` REFERENCES `YEU_CAU_BAO_TRI(YeuCauId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_Lenh_KeHoach`: `KeHoachId` REFERENCES `KE_HOACH_BAO_TRI(KeHoachId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_Lenh_UuTien`: `MucUuTienId` REFERENCES `CAU_HINH_MUC_UU_TIEN(MucUuTienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_Lenh_NguoiBanGiao`: `NguoiBanGiaoId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_Lenh_NguoiKyNhan`: `NguoiKyNhanId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_Lenh_NguoiTao`: `NguoiTaoId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_Lenh_NguoiDuyetDong`: `NguoiDuyetDongId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

#### 18. Bảng `CHUYEN_MON_KTV`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Kỹ thuật viên và Nhóm thiết bị thành thạo.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `NhanVienId` | VARCHAR | 20 | **PK, FK** | NOT NULL | Kỹ thuật viên $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `NhomThietBiId`| VARCHAR | 30 | **PK, FK** | NOT NULL | Nhóm máy thành thạo $\rightarrow$ `NHOM_THIET_BI(NhomThietBiId)` |
| `GhiChu` | VARCHAR | 100 | | NULL | Mức độ thành thạo (*Chính / Phụ*) |

- **Primary Key**: `PRIMARY KEY (NhanVienId, NhomThietBiId)`.
- **Foreign Keys**:
  - `FK_ChuyenMon_NhanVien`: `NhanVienId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_ChuyenMon_Nhom`: `NhomThietBiId` REFERENCES `NHOM_THIET_BI(NhomThietBiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
- **Ràng buộc nghiệp vụ**: Mỗi kỹ thuật viên được gắn tối đa 3 nhóm thiết bị (kiểm soát tại tầng ứng dụng).

---

#### 19. Bảng `PHAN_CONG_KTV`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Lệnh bảo trì và Kỹ thuật viên (Form Master - Detail 3).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `PhanCongId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `LenhBaoTriId` | VARCHAR | 30 | **FK** | NOT NULL | Lệnh được giao $\rightarrow$ `LENH_BAO_TRI(LenhBaoTriId)` |
| `KyThuatVienId` | VARCHAR | 20 | **FK** | NOT NULL | KTV thực hiện $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `NguoiPhanCongId`| VARCHAR | 20 | **FK** | NOT NULL | Trưởng BP phân công $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemPhanCong`| DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm giao việc |
| `KhungGioBatDau`| DATETIME | - | | NOT NULL | Khung giờ dự kiến bắt đầu (chống trùng lịch KTV) |
| `KhungGioKetThuc`| DATETIME | - | | NOT NULL | Khung giờ dự kiến kết thúc |
| `VaiTroTrongLenh`| VARCHAR | 30 | | NOT NULL, DEFAULT 'ThoChinh', CHECK (`VaiTroTrongLenh` IN ('ThoChinh', 'ThoPhu')) | Trách nhiệm trong ca sửa chữa |
| `SoGioCongThucTe`| DECIMAL | 6,2 | | NOT NULL, DEFAULT 0.00 | Tổng số giờ công thực tế của KTV trên lệnh |
| `TrangThaiPhanCong`| VARCHAR | 30 | | NOT NULL, DEFAULT 'DaGiao', CHECK (`TrangThaiPhanCong` IN ('DaGiao', 'DangLam', 'HoanThanh', 'HuyDieuChuyen')) | Trạng thái thực hiện của KTV |

- **Foreign Keys**:
  - `FK_PhanCong_Lenh`: `LenhBaoTriId` REFERENCES `LENH_BAO_TRI(LenhBaoTriId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_PhanCong_KTV`: `KyThuatVienId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_PhanCong_NguoiGiao`: `NguoiPhanCongId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 20. Bảng `KET_QUA_CHECKLIST`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Lệnh bảo trì phòng ngừa và Hạng mục checklist.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `KetQuaId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `LenhBaoTriId` | VARCHAR | 30 | **FK** | NOT NULL | Lệnh phòng ngừa $\rightarrow$ `LENH_BAO_TRI(LenhBaoTriId)` |
| `HangMucId` | INT | - | **FK** | NOT NULL | Hạng mục kiểm tra $\rightarrow$ `HANG_MUC_CHECKLIST(HangMucId)` |
| `GiaTriDoThucTe`| DECIMAL | 12,2 | | NULL | Số đo thực tế đo được (với kiểu NhapSoDo) |
| `KetLuan` | VARCHAR | 20 | | NOT NULL, CHECK (`KetLuan` IN ('Dat', 'KhongDat')) | Kết luận đánh giá |
| `NguoiKiemTraId`| VARCHAR | 20 | **FK** | NOT NULL | KTV thực hiện đo đạc $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemKiemTra`| DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm ghi nhận kết quả |
| `GhiChu` | VARCHAR | 255 | | NULL | Ghi chú bất thường phát hiện |

- **Foreign Keys**:
  - `FK_KetQua_Lenh`: `LenhBaoTriId` REFERENCES `LENH_BAO_TRI(LenhBaoTriId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_KetQua_HangMuc`: `HangMucId` REFERENCES `HANG_MUC_CHECKLIST(HangMucId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_KetQua_KTV`: `NguoiKiemTraId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 21. Bảng `BAO_CAO_SU_CO`
Báo cáo phân tích chuyên sâu về sự cố và nguyên nhân gốc rễ (Quan hệ 1 – 1 mở rộng với `LENH_BAO_TRI`).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `BaoCaoSuCoId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `LenhBaoTriId` | VARCHAR | 30 | **FK** | NOT NULL, UNIQUE | Quan hệ 1 – 1 với Lệnh sự cố $\rightarrow$ `LENH_BAO_TRI(LenhBaoTriId)` |
| `MaLoiChinhThucId`| VARCHAR | 20 | **FK** | NOT NULL | Mã sự cố chính thức sau khi sửa $\rightarrow$ `DANH_MUC_MA_LOI(MaLoiId)` |
| `CoDungMay` | TINYINT | 1 | | NOT NULL, DEFAULT 0 | Cờ đánh dấu sự cố có làm dừng máy hay không |
| `ThoiDiemXayRa`| DATETIME | - | | NULL | Thời điểm máy ngừng hoạt động (bắt buộc khi CoDungMay = 1) |
| `ThoiDiemKhoiPhuc`| DATETIME | - | | NULL | Thời điểm máy hoạt động lại (bắt buộc khi CoDungMay = 1) |
| `MoTaHuHong` | TEXT | - | | NOT NULL | Mô tả chi tiết hỏng hóc kỹ thuật |
| `NguyenNhanGocRe`| TEXT | - | | NOT NULL | Phân tích nguyên nhân gốc rễ (5-Why, Fishbone) |
| `MucDoAnhHuongPhanTram`| INT | - | | NOT NULL, DEFAULT 100, CHECK (`MucDoAnhHuongPhanTram` BETWEEN 0 AND 100) | Tỷ lệ suy giảm năng suất (%) |
| `BienPhapKhacPhuc`| TEXT | - | | NOT NULL | Biện pháp kỹ thuật đã xử lý |
| `NguoiLapBaoCaoId`| VARCHAR | 20 | **FK** | NOT NULL | KTV lập báo cáo $\rightarrow$ `NHAN_VIEN(NhanVienId)` |

- **Foreign Keys**:
  - `FK_BaoCao_Lenh`: `LenhBaoTriId` REFERENCES `LENH_BAO_TRI(LenhBaoTriId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_BaoCao_MaLoi`: `MaLoiChinhThucId` REFERENCES `DANH_MUC_MA_LOI(MaLoiId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_BaoCao_KTV`: `NguoiLapBaoCaoId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
- **Quy chuẩn 3NF**: `TongGioDungMay` là giá trị suy dẫn, **tính động** khi truy vấn:
  `CASE WHEN CoDungMay = 1 THEN TIMESTAMPDIFF(MINUTE, ThoiDiemXayRa, ThoiDiemKhoiPhuc) / 60.0 ELSE 0 END`.

---

### Phân hệ 6: Quản lý Phụ tùng & Kho Hiện vật

#### 22. Bảng `PHU_TUNG`
Quản lý danh mục phụ tùng, vật tư tiêu hao và số lượng tồn kho hiện vật phi tài chính (1 kho chung).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `PhuTungId` | VARCHAR | 30 | **PK** | NOT NULL | Mã định danh phụ tùng (VD: PT-VONGBI-6205, PT-DAU-THUYLUC) |
| `TenPhuTung` | VARCHAR | 150 | | NOT NULL | Tên phụ tùng, vật tư, linh kiện, dụng cụ |
| `LoaiPhuTung` | VARCHAR | 50 | | NOT NULL, CHECK (`LoaiPhuTung` IN ('CoKhi', 'Dien', 'TieuHao', 'DungCu')) | Phân loại chủng loại vật tư |
| `QuyCach` | VARCHAR | 150 | | NOT NULL | Quy cách kỹ thuật, thông số kích thước |
| `DonViTinh` | VARCHAR | 20 | | NOT NULL | Đơn vị tính (Cái, Bộ, Lít, Mét, Hộp...) |
| `SoLuongTonHienCo`| DECIMAL | 10,2 | | NOT NULL, DEFAULT 0.00 | Số lượng tồn kho vật lý thực tế trong kho |
| `SoLuongDangDuTru`| DECIMAL | 10,2 | | NOT NULL, DEFAULT 0.00 | Số lượng đang giữ chỗ cho các phiếu bảo trì mở |
| `MucTonToiThieu`| DECIMAL | 10,2 | | NOT NULL, DEFAULT 0.00 | Ngưỡng tồn tối thiểu kích hoạt cảnh báo thiếu hàng |
| `MoTa` | VARCHAR | 255 | | NULL | Mô tả ghi chú thêm |

- **Quy chuẩn 3NF**: `TonKhoKhaDung` tính động: `SoLuongTonHienCo - SoLuongDangDuTru`.

---

#### 23. Bảng `DINH_MUC_LINH_KIEN`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Thiết bị và Phụ tùng định mức chuẩn.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `DinhMucId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `ThietBiId` | VARCHAR | 30 | **FK** | NOT NULL | Thiết bị áp dụng $\rightarrow$ `THIET_BI(ThietBiId)` |
| `PhuTungId` | VARCHAR | 30 | **FK** | NOT NULL | Phụ tùng định mức $\rightarrow$ `PHU_TUNG(PhuTungId)` |
| `SoLuongDinhMuc`| DECIMAL | 10,2 | | NOT NULL, DEFAULT 1.00 | Số lượng linh kiện chuẩn gắn trên máy |
| `ThayTheDinhKy`| TINYINT | 1 | | NOT NULL, DEFAULT 0 | Đánh dấu linh kiện cần thay định kỳ (gợi ý tự động trong dự trù) |
| `ViTriLapDat` | VARCHAR | 100 | | NULL | Vị trí lắp cụ thể trên máy (Trục Z, Cụm bơm dầu...) |

- **Unique Constraint**: `UNIQUE KEY UQ_ThietBi_PhuTung (ThietBiId, PhuTungId)`.
- **Foreign Keys**:
  - `FK_DinhMuc_ThietBi`: `ThietBiId` REFERENCES `THIET_BI(ThietBiId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_DinhMuc_PhuTung`: `PhuTungId` REFERENCES `PHU_TUNG(PhuTungId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 24. Bảng `PHIEU_CAP_NHAT_TON_KHO`
Quản lý phiếu bổ sung kho ngoài định kỳ hoặc điều chỉnh tồn kho do kiểm kê (Phi tài chính).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `PhieuCapNhatId`| VARCHAR | 30 | **PK** | NOT NULL | Số phiếu cập nhật kho (VD: CNK-202611-001) |
| `LoaiCapNhat` | VARCHAR | 30 | | NOT NULL, CHECK (`LoaiCapNhat` IN ('BoSungTuKhoNgoai', 'DieuChinhKiemKe')) | Phân loại mục đích cập nhật |
| `MaPhieuKhoNgoai`| VARCHAR | 50 | | NULL | Số chứng từ tham chiếu của kho vật tư công ty (văn bản) |
| `ThoiDiemLap` | DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm lập đề xuất cập nhật |
| `NguoiLapId` | VARCHAR | 20 | **FK** | NOT NULL | Trưởng BP bảo trì lập phiếu $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `NguoiXacNhanId`| VARCHAR | 20 | **FK** | NULL | Quản lý nhà máy phê duyệt $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemXacNhan`| DATETIME | - | | NULL | Thời điểm chính thức khớp số lượng vào kho vật lý |
| `TrangThaiPhieu`| VARCHAR | 30 | | NOT NULL, DEFAULT 'ChoXacNhan', CHECK (`TrangThaiPhieu` IN ('ChoXacNhan', 'DaXacNhan', 'TuChoi')) | Trạng thái phê duyệt |
| `GhiChu` | VARCHAR | 255 | | NULL | Giải trình lý do điều chỉnh kiểm kê hoặc đợt nhập |

- **Foreign Keys**:
  - `FK_PhieuCapNhat_NguoiLap`: `NguoiLapId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_PhieuCapNhat_NguoiXacNhan`: `NguoiXacNhanId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

#### 25. Bảng `CHI_TIET_CAP_NHAT_TON_KHO`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Phiếu cập nhật kho và Phụ tùng.

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `ChiTietCapNhatId`| BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `PhieuCapNhatId`| VARCHAR | 30 | **FK** | NOT NULL | Phiếu cập nhật cha $\rightarrow$ `PHIEU_CAP_NHAT_TON_KHO(PhieuCapNhatId)` |
| `PhuTungId` | VARCHAR | 30 | **FK** | NOT NULL | Phụ tùng được cập nhật $\rightarrow$ `PHU_TUNG(PhuTungId)` |
| `SoLuongThayDoi`| DECIMAL | 10,2 | | NOT NULL | Số lượng biến động (+ khi nhập; +/- khi kiểm kê) |
| `GhiChu` | VARCHAR | 255 | | NULL | Ghi chú dòng phụ tùng |

- **Foreign Keys**:
  - `FK_ChiTietCapNhat_Phieu`: `PhieuCapNhatId` REFERENCES `PHIEU_CAP_NHAT_TON_KHO(PhieuCapNhatId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_ChiTietCapNhat_PhuTung`: `PhuTungId` REFERENCES `PHU_TUNG(PhuTungId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

#### 26. Bảng `PHIEU_DU_TRU_XUAT_DUNG`
Phiếu dự trù và xuất dùng vật tư phụ tùng gắn liền với Lệnh bảo trì (BM-05 Master - Form Master - Detail 4).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `PhieuXuatId` | VARCHAR | 30 | **PK** | NOT NULL | Số phiếu xuất dùng (VD: PX-202611-001) |
| `LenhBaoTriId` | VARCHAR | 30 | **FK** | NOT NULL | Lệnh bảo trì liên kết $\rightarrow$ `LENH_BAO_TRI(LenhBaoTriId)` |
| `NguoiLapDuTruId`| VARCHAR | 20 | **FK** | NOT NULL | Trưởng BP lập dự trù $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemLap` | DATETIME | - | | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Thời điểm lập dự trù |
| `NguoiKhaiBaoId`| VARCHAR | 20 | **FK** | NULL | KTV khai báo thực dùng $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemKhaiBao`| DATETIME | - | | NULL | Thời điểm KTV khai báo thực dùng |
| `NguoiDuyetVuotId`| VARCHAR | 20 | **FK** | NULL | Quản lý duyệt vượt dự trù $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemDuyetVuot`| DATETIME | - | | NULL | Thời điểm phê duyệt vượt dự trù |
| `NguoiXacNhanXuatId`| VARCHAR | 20 | **FK** | NULL | Trưởng BP xác nhận trừ kho $\rightarrow$ `NHAN_VIEN(NhanVienId)` |
| `ThoiDiemXacNhan`| DATETIME | - | | NULL | Thời điểm trừ kho vật lý, giải phóng giữ chỗ và đóng sổ |
| `TrangThaiPhieu`| VARCHAR | 30 | | NOT NULL, DEFAULT 'DangDuTru', CHECK (`TrangThaiPhieu` IN ('DangDuTru', 'DaKhaiBaoThucDung', 'ChoDuyetVuotDuTru', 'DaXacNhanTruTon', 'DaHuy')) | Vòng đời trạng thái phiếu xuất |
| `GhiChu` | TEXT | - | | NULL | Ghi chú chung tình hình vật tư |

- **Foreign Keys**:
  - `FK_PhieuXuat_Lenh`: `LenhBaoTriId` REFERENCES `LENH_BAO_TRI(LenhBaoTriId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_PhieuXuat_NguoiLap`: `NguoiLapDuTruId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE RESTRICT ON UPDATE CASCADE.
  - `FK_PhieuXuat_NguoiKhaiBao`: `NguoiKhaiBaoId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_PhieuXuat_NguoiDuyetVuot`: `NguoiDuyetVuotId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.
  - `FK_PhieuXuat_NguoiXacNhan`: `NguoiXacNhanXuatId` REFERENCES `NHAN_VIEN(NhanVienId)` ON DELETE SET NULL ON UPDATE CASCADE.

---

#### 27. Bảng `CHI_TIET_PHIEU_XUAT`
Bảng trung gian tách quan hệ Nhiều - Nhiều ($M-N$) giữa Phiếu xuất dùng và Phụ tùng (BM-05 Detail).

| Tên cột | Kiểu dữ liệu | Kích thước | Khóa | Ràng buộc | Mô tả ý nghĩa nghiệp vụ |
| :--- | :--- | :---: | :---: | :--- | :--- |
| `ChiTietId` | BIGINT | - | **PK** | NOT NULL, AUTO_INCREMENT | Khóa chính tự tăng |
| `PhieuXuatId` | VARCHAR | 30 | **FK** | NOT NULL | Phiếu xuất dùng cha $\rightarrow$ `PHIEU_DU_TRU_XUAT_DUNG(PhieuXuatId)` |
| `PhuTungId` | VARCHAR | 30 | **FK** | NOT NULL | Phụ tùng xuất dùng $\rightarrow$ `PHU_TUNG(PhuTungId)` |
| `SoLuongDuTru` | DECIMAL | 10,2 | | NOT NULL, DEFAULT 0.00 | Số lượng dự trù giữ chỗ trước sửa chữa |
| `SoLuongThucDung`| DECIMAL | 10,2 | | NULL | Số lượng KTV khai báo thực tế đã sử dụng |
| `GhiChuGiaiTrinh`| VARCHAR | 255 | | NULL | Giải trình khi thực dùng lệch so với dự trù |

- **Foreign Keys**:
  - `FK_ChiTietXuat_Phieu`: `PhieuXuatId` REFERENCES `PHIEU_DU_TRU_XUAT_DUNG(PhieuXuatId)` ON DELETE CASCADE ON UPDATE CASCADE.
  - `FK_ChiTietXuat_PhuTung`: `PhuTungId` REFERENCES `PHU_TUNG(PhuTungId)` ON DELETE RESTRICT ON UPDATE CASCADE.

---

## 4. CHIẾN LƯỢC ĐÁNH CHỈ MỤC (INDEXING STRATEGY)
Để tối ưu hóa hiệu năng truy vấn cho các báo cáo định kỳ (BC1 $\rightarrow$ BC7) và các màn hình tra cứu lớn:
1. **Bảng `LENH_BAO_TRI`**:
   - `IDX_LenhBaoTri_ThietBi_ThoiGian` trên `(ThietBiId, ThoiGianDuKienBatDau)`: Tối ưu tra cứu lịch sử sửa chữa của máy.
   - `IDX_LenhBaoTri_TrangThai` trên `(TrangThaiLenh, MucUuTienId)`: Tối ưu lọc danh sách lệnh đang mở trên Dashboard.
2. **Bảng `YEU_CAU_BAO_TRI`**:
   - `IDX_YeuCau_TrangThai_ThoiDiem` trên `(TrangThaiYeuCau, ThoiDiemGui)`: Tối ưu hàng đợi xử lý sự cố chưa tiếp nhận.
3. **Bảng `PHU_TUNG`**:
   - `IDX_PhuTung_TonKho` trên `(SoLuongTonHienCo, SoLuongDangDuTru, MucTonToiThieu)`: Tối ưu cảnh báo phụ tùng sắp hết hàng.
4. **Bảng `NHAT_KY_HE_THONG`**:
   - `IDX_NhatKy_ThoiDiem` trên `(ThoiDiem)`: Tối ưu tra cứu log theo khoảng thời gian kiểm toán.
