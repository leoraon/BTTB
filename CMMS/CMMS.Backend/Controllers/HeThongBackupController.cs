using System.Text;
using System.Text.Json;
using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Security;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Controllers;

[Route("api/hethong")]
[Authorize]
[RequirePermission(MaQuyen.BACKUP_EXECUTE)]
public class HeThongBackupController(
    CmmsDbContext db,
    AuditLogHelper audit,
    TimeProvider? timeProvider = null) : ApiControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    [HttpGet("backup-json")]
    public async Task<IActionResult> ExportBackupJson()
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        var taiKhoans = await db.TAI_KHOAN.AsNoTracking()
            .Select(t => new
            {
                t.TaiKhoanId,
                t.NhanVienId,
                t.TenDangNhap,
                t.VaiTroId,
                t.TrangThaiTaiKhoan,
                t.SoLanDangNhapSai,
                t.ThoiDiemMoKhoaTam,
                t.NgayTao,
                t.NguoiDuyetId
            })
            .ToListAsync();

        var snapshot = new
        {
            Database = "cmms_db",
            ExportedAt = now,
            ExportedByTaiKhoanId = CurrentTaiKhoanId,
            Data = new
            {
                VAI_TRO = await db.VAI_TRO.AsNoTracking().ToListAsync(),
                VAI_TRO_QUYEN = await db.VAI_TRO_QUYEN.AsNoTracking().ToListAsync(),
                THU_VIEN_BIEU_MAU = await db.THU_VIEN_BIEU_MAU.AsNoTracking().ToListAsync(),
                PHAN_XUONG_BO_PHAN = await db.PHAN_XUONG_BO_PHAN.AsNoTracking().ToListAsync(),
                NHAN_VIEN = await db.NHAN_VIEN.AsNoTracking().ToListAsync(),
                TAI_KHOAN = taiKhoans,
                NHOM_THIET_BI = await db.NHOM_THIET_BI.AsNoTracking().ToListAsync(),
                DANH_MUC_MA_LOI = await db.DANH_MUC_MA_LOI.AsNoTracking().ToListAsync(),
                THIET_BI = await db.THIET_BI.AsNoTracking().ToListAsync(),
                THONG_SO_THIET_BI = await db.THONG_SO_THIET_BI.AsNoTracking().ToListAsync(),
                CAU_HINH_MUC_UU_TIEN = await db.CAU_HINH_MUC_UU_TIEN.AsNoTracking().ToListAsync(),
                YEU_CAU_BAO_TRI = await db.YEU_CAU_BAO_TRI.AsNoTracking().ToListAsync(),
                MAU_CHECKLIST = await db.MAU_CHECKLIST.AsNoTracking().ToListAsync(),
                HANG_MUC_CHECKLIST = await db.HANG_MUC_CHECKLIST.AsNoTracking().ToListAsync(),
                KE_HOACH_BAO_TRI = await db.KE_HOACH_BAO_TRI.AsNoTracking().ToListAsync(),
                LENH_BAO_TRI = await db.LENH_BAO_TRI.AsNoTracking().ToListAsync(),
                CHUYEN_MON_KTV = await db.CHUYEN_MON_KTV.AsNoTracking().ToListAsync(),
                PHAN_CONG_KTV = await db.PHAN_CONG_KTV.AsNoTracking().ToListAsync(),
                KET_QUA_CHECKLIST = await db.KET_QUA_CHECKLIST.AsNoTracking().ToListAsync(),
                BAO_CAO_SU_CO = await db.BAO_CAO_SU_CO.AsNoTracking().ToListAsync(),
                PHU_TUNG = await db.PHU_TUNG.AsNoTracking().ToListAsync(),
                DINH_MUC_LINH_KIEN = await db.DINH_MUC_LINH_KIEN.AsNoTracking().ToListAsync(),
                PHIEU_CAP_NHAT_TON_KHO = await db.PHIEU_CAP_NHAT_TON_KHO.AsNoTracking().ToListAsync(),
                CHI_TIET_CAP_NHAT_TON_KHO = await db.CHI_TIET_CAP_NHAT_TON_KHO.AsNoTracking().ToListAsync(),
                PHIEU_DU_TRU_XUAT_DUNG = await db.PHIEU_DU_TRU_XUAT_DUNG.AsNoTracking().ToListAsync(),
                CHI_TIET_PHIEU_XUAT = await db.CHI_TIET_PHIEU_XUAT.AsNoTracking().ToListAsync(),
                NHAT_KY_HE_THONG = await db.NHAT_KY_HE_THONG.AsNoTracking().OrderByDescending(n => n.NhatKyId).Take(500).ToListAsync()
            }
        };

        await audit.WriteAsync(
            CurrentTaiKhoanId,
            CmmsConstants.LoaiThaoTacLog.SaoLuuDuLieu,
            "cmms_db",
            $"Xuất bản sao lưu dữ liệu hệ thống JSON ({snapshot.Data.THIET_BI.Count} thiết bị, {snapshot.Data.LENH_BAO_TRI.Count} lệnh bảo trì).");

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"CMMS_Backup_{now:yyyyMMdd_HHmmss}.json");
    }
}
