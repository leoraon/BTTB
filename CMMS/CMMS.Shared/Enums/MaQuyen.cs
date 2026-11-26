namespace CMMS.Shared.Enums;

/// <summary>
/// Tập mã quyền chuẩn hóa (khớp 100% với bảng VAI_TRO_QUYEN trong seed_data.sql &amp; seed-data.sql).
/// </summary>
public static class MaQuyen
{
    // Vai trò 1: Quản trị hệ thống
    public const string USER_MANAGE = "USER_MANAGE";
    public const string ROLE_MANAGE = "ROLE_MANAGE";
    public const string SYSTEM_CONFIG = "SYSTEM_CONFIG";
    public const string AUDIT_VIEW = "AUDIT_VIEW";
    public const string BACKUP_EXECUTE = "BACKUP_EXECUTE";

    // Vai trò 2: Quản lý nhà máy
    public const string ACCOUNT_APPROVE = "ACCOUNT_APPROVE";
    public const string STOCK_ADJUST_APPROVE = "STOCK_ADJUST_APPROVE";
    public const string REPORT_VIEW_ALL = "REPORT_VIEW_ALL";
    public const string KPI_MONITOR = "KPI_MONITOR";

    // Vai trò 3: Trưởng bộ phận bảo trì
    public const string DEVICE_MANAGE = "DEVICE_MANAGE";
    public const string REQUEST_APPROVE = "REQUEST_APPROVE";
    public const string WO_CREATE_DISPATCH = "WO_CREATE_DISPATCH";
    public const string STOCK_PROPOSAL_CREATE = "STOCK_PROPOSAL_CREATE";
    public const string CHECKLIST_MANAGE = "CHECKLIST_MANAGE";
    public const string REPORT_MAINTENANCE_VIEW = "REPORT_MAINTENANCE_VIEW";

    // Vai trò 4: Kỹ thuật viên
    public const string WO_EXECUTE = "WO_EXECUTE";
    public const string CHECKLIST_RECORD = "CHECKLIST_RECORD";
    public const string MATERIAL_DECLARE = "MATERIAL_DECLARE";
    public const string INCIDENT_REPORT_CREATE = "INCIDENT_REPORT_CREATE";

    // Vai trò 5: Người dùng thiết bị
    public const string REQUEST_CREATE = "REQUEST_CREATE";
    public const string WO_ACCEPTANCE_SIGN = "WO_ACCEPTANCE_SIGN";
    public const string DEVICE_STATUS_VIEW = "DEVICE_STATUS_VIEW";

    public sealed record QuyenInfo(string Code, string TenHienThi, string NhomChucNang);

    /// <summary>
    /// Danh mục 22 mã quyền chuẩn dùng để hiển thị trên màn hình Phân quyền vai trò.
    /// </summary>
    public static IReadOnlyList<QuyenInfo> All { get; } =
    [
        new(USER_MANAGE, "Quản lý hồ sơ nhân sự, đề xuất & khóa/mở khóa tài khoản", "1. Quản lý hệ thống"),
        new(ROLE_MANAGE, "Quản lý danh mục vai trò và thiết lập phân quyền chức năng", "1. Quản lý hệ thống"),
        new(SYSTEM_CONFIG, "Cấu hình tham số hệ thống & mức độ ưu tiên SLA", "1. Quản lý hệ thống"),
        new(AUDIT_VIEW, "Tra cứu nhật ký thao tác hệ thống", "1. Quản lý hệ thống"),
        new(BACKUP_EXECUTE, "Sao lưu và quản trị dữ liệu hệ thống", "1. Quản lý hệ thống"),
        new(ACCOUNT_APPROVE, "Phê duyệt kích hoạt hoặc từ chối tài khoản mới", "1. Quản lý hệ thống"),

        new(DEVICE_MANAGE, "Quản lý danh mục dùng chung & hồ sơ thiết bị 2 cấp", "2. Tài sản thiết bị"),
        new(DEVICE_STATUS_VIEW, "Tra cứu thông tin vận hành và lưu ý an toàn thiết bị", "2. Tài sản thiết bị"),

        new(REQUEST_CREATE, "Lập, sửa, hủy và theo dõi yêu cầu báo hỏng thiết bị", "3. Bảo trì sự cố"),
        new(REQUEST_APPROVE, "Đánh giá mã lỗi, chốt mức ưu tiên và duyệt/từ chối báo hỏng", "3. Bảo trì sự cố"),

        new(CHECKLIST_MANAGE, "Quản lý mẫu checklist & lập kế hoạch bảo trì định kỳ", "4. Bảo trì phòng ngừa"),

        new(WO_CREATE_DISPATCH, "Khởi tạo, lập lịch, phân công KTV và phê duyệt đóng lệnh", "5. Lệnh bảo trì"),
        new(WO_EXECUTE, "Tiếp nhận và thực hiện lệnh bảo trì, ghi nhận giờ công", "5. Lệnh bảo trì"),
        new(CHECKLIST_RECORD, "Cập nhật kết quả kiểm tra checklist định kỳ", "5. Lệnh bảo trì"),
        new(INCIDENT_REPORT_CREATE, "Lập báo cáo xử lý sự cố và thời gian dừng máy", "5. Lệnh bảo trì"),
        new(WO_ACCEPTANCE_SIGN, "Ký xác nhận biên bản nghiệm thu bàn giao thiết bị", "5. Lệnh bảo trì"),

        new(STOCK_PROPOSAL_CREATE, "Quản lý danh mục phụ tùng, định mức, dự trù/xuất & đề xuất nhập kho", "6. Phụ tùng & Kho"),
        new(STOCK_ADJUST_APPROVE, "Xác nhận bổ sung tồn kho từ kho ngoài & điều chỉnh kiểm kê", "6. Phụ tùng & Kho"),
        new(MATERIAL_DECLARE, "Khai báo số lượng phụ tùng thực tế sử dụng", "6. Phụ tùng & Kho"),

        new(KPI_MONITOR, "Giám sát Dashboard tổng hợp thời gian thực", "7. Báo cáo & Thống kê"),
        new(REPORT_VIEW_ALL, "Xem và kết xuất toàn bộ báo cáo quản trị BC1–BC7", "7. Báo cáo & Thống kê"),
        new(REPORT_MAINTENANCE_VIEW, "Xem và kết xuất các báo cáo bảo trì BC1–BC7", "7. Báo cáo & Thống kê")
    ];

    public static bool IsValid(string code) => All.Any(x => x.Code == code);
}
