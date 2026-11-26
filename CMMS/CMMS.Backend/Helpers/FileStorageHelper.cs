namespace CMMS.Backend.Helpers;

public class FileStorageHelper(IWebHostEnvironment env)
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private string WebRoot => Path.GetFullPath(string.IsNullOrWhiteSpace(env.WebRootPath)
        ? Path.Combine(env.ContentRootPath, "wwwroot")
        : env.WebRootPath);

    private static bool IsInsideDirectory(string fullPath, string baseDirectory)
    {
        var normalizedFull = Path.GetFullPath(fullPath);
        var normalizedBase = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasTraversalSegment(string relativePath)
        => relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Any(seg => seg == "..");

    public async Task<(bool Ok, string MessageOrRelativePath)> SaveFileAsync(
        IFormFile file, string subFolder, string idPrefix, string[] allowedExtensions)
    {
        if (file == null || file.Length == 0)
            return (false, "Tệp tải lên rỗng.");
        if (file.Length > MaxFileSizeBytes)
            return (false, "Kích thước tệp vượt quá giới hạn 10 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return (false, $"Định dạng tệp '{ext}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}.");

        var folderPath = Path.Combine(WebRoot, "uploads", subFolder);
        Directory.CreateDirectory(folderPath);

        var safePrefix = string.Concat(idPrefix.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        var fileName = $"{safePrefix}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return (true, $"/uploads/{subFolder}/{fileName}");
    }

    public void DeleteRelativeFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || HasTraversalSegment(relativePath)) return;
        var normalized = relativePath.TrimStart('/', '\\').Replace('\\', '/');
        if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var uploadsRoot = Path.Combine(WebRoot, "uploads");
        var trimmed = normalized.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(WebRoot, trimmed));
        if (!IsInsideDirectory(fullPath, uploadsRoot))
            return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public string? ResolveExistingFile(string? relativePath, params string[] fallbackFolders)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || HasTraversalSegment(relativePath)) return null;
        var trimmed = relativePath.TrimStart('/', '\\').Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(trimmed)) return null;

        var direct = Path.GetFullPath(Path.Combine(WebRoot, trimmed));
        if (IsInsideDirectory(direct, WebRoot) && File.Exists(direct))
            return direct;

        var fileName = Path.GetFileName(trimmed);
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        foreach (var folder in fallbackFolders)
        {
            var baseDir = Path.GetFullPath(Path.Combine(WebRoot, folder.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsInsideDirectory(baseDir, WebRoot)) continue;

            var candidate = Path.GetFullPath(Path.Combine(baseDir, fileName));
            if (IsInsideDirectory(candidate, baseDir) && File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    public async Task<byte[]> ReadFileBytesAsync(string fullPath)
    {
        await using var stream = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096, useAsync: true);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
