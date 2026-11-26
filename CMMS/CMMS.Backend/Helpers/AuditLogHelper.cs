using CMMS.Backend.Entities;

namespace CMMS.Backend.Helpers;

public class AuditLogHelper(CmmsDbContext db, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task WriteAsync(int? taiKhoanId, string loaiThaoTac, string? doiTuongTacDong, string moTaChiTiet)
    {
        db.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
        {
            TaiKhoanId = taiKhoanId,
            ThoiDiem = _timeProvider.GetLocalNow().DateTime,
            LoaiThaoTac = loaiThaoTac,
            DoiTuongTacDong = doiTuongTacDong,
            MoTaChiTiet = moTaChiTiet
        });
        await db.SaveChangesAsync();
    }
}
