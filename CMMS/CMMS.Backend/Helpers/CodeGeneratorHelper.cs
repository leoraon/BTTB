using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Helpers;

public static class CodeGeneratorHelper
{
    public static async Task<string> NextMonthlyCodeAsync(
        IQueryable<string> existingIds, string prefix, DateTime? now = null)
    {
        var ym = (now ?? DateTime.Now).ToString("yyyyMM");
        var head = $"{prefix}-{ym}-";
        var matches = await existingIds
            .Where(id => id.StartsWith(head))
            .ToListAsync();

        int maxSeq = 0;
        foreach (var id in matches)
        {
            var suffix = id[head.Length..];
            if (int.TryParse(suffix, out var n) && n > maxSeq)
                maxSeq = n;
        }

        return $"{head}{maxSeq + 1:D3}";
    }
}
