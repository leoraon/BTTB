# CMMS — Hệ thống Quản lý Bảo trì Thiết bị Nhà máy Gia công Cơ khí (.NET 10 + MySQL 8.0)

Bộ mã nguồn hoàn chỉnh cho **Backend API (HTTPS/REST + EF Core 10 + JWT + Excel/PDF)** và **Shared DTOs/Enums** của 7 phân hệ (27 chức năng chính, 62 chức năng lá, 27 bảng 3NF).

---

## 1. Cấu trúc Thư mục Đã dựng

```text
CMMS/
├── CMMS.slnx                      # Solution mở bằng Visual Studio
├── .gitignore                     # Chặn file rác build & appsettings.Development.json
├── Database/                      # Đã tích hợp đầy đủ 6 file CSDL trong Solution
│   ├── create-database.sql        # DDL 27 bảng chuẩn 3NF (cmms_db) - CHẠY FILE NÀY ĐẦU TIÊN
│   ├── seed_data.sql              # Dữ liệu mẫu chính (đủ 22 mã quyền khớp 100% Backend) - CHẠY FILE NÀY THỨ HAI
│   ├── seed-data.sql              # Dữ liệu mẫu bản cũ (lưu tham khảo, không chạy đè lên seed_data.sql)
│   ├── erd-diagram.mmd            # Sơ đồ ERD (Mermaid)
│   ├── schema.dbml                # Đặc tả schema DBML
│   └── thiet-ke-csdl-vat-ly.md    # Tài liệu thiết kế CSDL vật lý chi tiết
├── CMMS.Shared/                   # Enums (MaQuyen, CmmsConstants) & toàn bộ DTOs dùng chung
├── CMMS.Backend/                  # ASP.NET Core 10 Web API (27 Entities, 14 Services, 14 Controllers)
│   ├── Entities/                  # 27 class Entity + CmmsDbContext
│   ├── Security/                  # JWT Bearer + [RequirePermission]
│   ├── Helpers/                   # AuditLogHelper, FileStorageHelper, Exporters (Excel/PDF)
│   ├── Services/                  # Toàn bộ thuật toán xử lý nghiệp vụ & Transaction ACID
│   ├── Controllers/               # 14 REST API Controllers
│   └── wwwroot/bieu-mau/          # 6 biểu mẫu chuẩn BM-01..BM-06 (.docx/.pdf)
└── CMMS.Backend.Tests/            # Bộ test tự động xUnit kiểm thử nghiệp vụ & HTTP Pipeline
```

---

## 2. Cách Đưa Thẳng Code vào Project `E:\Desktop\CMMS` trên Máy Bạn

1. **Đóng Visual Studio** đang mở `E:\Desktop\CMMS` (nếu đang mở) để tránh khóa file.
2. Copy **2 thư mục** `CMMS.Shared` và `CMMS.Backend` (cùng `.gitignore`, `Database/`) từ:
   `C:\Users\admin\.gemini\antigravity\scratch\CMMS\`
   sang thư mục:
   `E:\Desktop\CMMS\` (chọn **Replace / Ghi đè** nếu có file trùng như `Program.cs`, `CMMS.Backend.csproj`, `appsettings.json`).
   *(Lưu ý: Không cần copy `CMMS.Backend.Tests` nếu bạn chỉ muốn giữ 3 project chính `Shared`, `Backend`, `WinForms` trong `E:\Desktop\CMMS\CMMS.slnx`.)*
3. Mở lại `E:\Desktop\CMMS\CMMS.slnx` bằng Visual Studio, nhấn **Ctrl+Shift+B** để khôi phục gói NuGet và Build.
4. Mở `E:\Desktop\CMMS\CMMS.Backend\appsettings.Development.json` và thay đúng **mật khẩu MySQL `root`** trên máy bạn:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=cmms_db;User=root;Password=MAT_KHAU_MYSQL_CUA_BAN;"
     }
   }
   ```

---

## 3. Chạy API & Test ngay trên Trình duyệt (HTTPS + Scalar)

1. Lần đầu tiên chạy HTTPS trên máy Windows, mở PowerShell gõ 1 lần duy nhất:
   ```powershell
   dotnet dev-certs https --trust
   ```
2. Đặt **`CMMS.Backend`** làm *Startup Project* và nhấn **F5**.
3. Trình duyệt sẽ mở **`https://localhost:7077/scalar/v1`** hiển thị đầy đủ tất cả API của 7 phân hệ.
4. Thử gọi **`POST /api/auth/login`** với một trong 5 tài khoản mẫu (mật khẩu mặc định **`123456`** — hệ thống sẽ tự động nâng cấp mật khẩu seed sang BCrypt thật ngay lần đăng nhập đầu tiên):

| Vai trò | Tên đăng nhập (`seed_data.sql`) | Tên đăng nhập (`seed-data.sql`) | Mật khẩu |
| :--- | :--- | :--- | :---: |
| 1. Quản trị hệ thống | `admin.hung` | `admin` | `123456` |
| 2. Quản lý nhà máy | `manager.an` | `manager.an` | `123456` |
| 3. Trưởng BP bảo trì | `lead.trong` | `lead.trong` | `123456` |
| 4. Kỹ thuật viên | `ktv.bale` | `tech.bale` | `123456` |
| 5. Người dùng thiết bị | `op.cuong` | `op.cuong` | `123456` |

5. Copy chuỗi `token` nhận được, dán vào ô **Bearer Token** trên Scalar (hoặc gửi kèm Header `Authorization: Bearer <token>` từ WinForms `ApiClient`) để gọi mọi API khác.
