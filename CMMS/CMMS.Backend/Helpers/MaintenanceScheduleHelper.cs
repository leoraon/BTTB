using CMMS.Shared.Enums;

namespace CMMS.Backend.Helpers;

/// <summary>
/// Bộ máy tính toán chu kỳ bảo trì phòng ngừa (ngày/tuần/tháng/quý/năm),
/// bảo toàn mốc ngày cuối tháng / năm nhuận và chọn ca làm việc mặc định theo nhóm thiết bị.
/// </summary>
public static class MaintenanceScheduleHelper
{
    public static DateOnly AddCycle(DateOnly baseDate, string donViChuKy, int giaTriChuKy, int? anchorDay = null)
    {
        int step = Math.Max(1, giaTriChuKy);
        return donViChuKy switch
        {
            CmmsConstants.DonViChuKy.Ngay => baseDate.AddDays(step),
            CmmsConstants.DonViChuKy.Tuan => baseDate.AddDays(step * 7),
            CmmsConstants.DonViChuKy.Thang => AddMonthsPreservingAnchor(baseDate, step, anchorDay),
            CmmsConstants.DonViChuKy.Quy => AddMonthsPreservingAnchor(baseDate, step * 3, anchorDay),
            CmmsConstants.DonViChuKy.Nam => AddMonthsPreservingAnchor(baseDate, step * 12, anchorDay),
            _ => AddMonthsPreservingAnchor(baseDate, step, anchorDay)
        };
    }

    private static DateOnly AddMonthsPreservingAnchor(DateOnly baseDate, int totalMonths, int? anchorDay)
    {
        var rawTarget = baseDate.AddMonths(totalMonths);
        if (anchorDay.HasValue)
        {
            int baseMonthEnd = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
            if (baseDate.Day == baseMonthEnd && anchorDay.Value > baseMonthEnd)
            {
                int targetMonthEnd = DateTime.DaysInMonth(rawTarget.Year, rawTarget.Month);
                return new DateOnly(rawTarget.Year, rawTarget.Month, Math.Min(anchorDay.Value, targetMonthEnd));
            }
        }
        return rawTarget;
    }

    public static DateOnly CalculateNextDueDateAfterCompletion(
        DateOnly currentDueDate,
        DateOnly actualCompletionDate,
        string donViChuKy,
        int giaTriChuKy,
        DateOnly ngayBatDauApDung)
    {
        var nextFromActual = AddCycle(actualCompletionDate, donViChuKy, giaTriChuKy, ngayBatDauApDung.Day);
        if (nextFromActual <= currentDueDate)
            return AddCycle(currentDueDate, donViChuKy, giaTriChuKy, ngayBatDauApDung.Day);

        return nextFromActual;
    }

    public static int EstimateCycleDays(string donViChuKy, int giaTriChuKy)
    {
        int step = Math.Max(1, giaTriChuKy);
        return donViChuKy switch
        {
            CmmsConstants.DonViChuKy.Ngay => step,
            CmmsConstants.DonViChuKy.Tuan => step * 7,
            CmmsConstants.DonViChuKy.Thang => step * 30,
            CmmsConstants.DonViChuKy.Quy => step * 90,
            CmmsConstants.DonViChuKy.Nam => step * 365,
            _ => step * 30
        };
    }

    public static TimeOnly ResolveDefaultStartTime(IEnumerable<string?> qualifiedKtvShifts)
    {
        var shifts = qualifiedKtvShifts
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (shifts.Count == 0
            || shifts.Contains(CmmsConstants.CaLamViec.Ca1)
            || shifts.Contains(CmmsConstants.CaLamViec.HanhChinh))
            return new TimeOnly(8, 0);

        if (shifts.Contains(CmmsConstants.CaLamViec.Ca2))
            return new TimeOnly(14, 0);

        if (shifts.Contains(CmmsConstants.CaLamViec.Ca3))
            return new TimeOnly(22, 0);

        return new TimeOnly(8, 0);
    }
}
