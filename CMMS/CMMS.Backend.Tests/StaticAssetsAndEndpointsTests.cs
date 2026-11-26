using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CMMS.Backend.Entities;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CMMS.Backend.Tests;

public class StaticAssetsAndEndpointsTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly WebApplicationFactory<Program> _factory;

    public StaticAssetsAndEndpointsTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<CmmsDbContext>>();
                services.RemoveAll<DbContextOptions<CmmsDbContext>>();
                services.RemoveAll<CmmsDbContext>();
                services.AddDbContext<CmmsDbContext>(opt => opt.UseSqlite(_conn));

                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CmmsDbContext>();
                db.Database.EnsureCreated();
                TestDbContextHandle.Seed(db);
            });
        });
    }

    [Fact]
    public void SeedData_All121StaticAssets_MustExistPhysically_WithValidHeaders()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var sqlPath = Path.Combine(repoRoot, "Database", "seed_data.sql");
        var wwwroot = Path.Combine(repoRoot, "CMMS.Backend", "wwwroot");

        Assert.True(File.Exists(sqlPath));
        var sql = File.ReadAllText(sqlPath, Encoding.UTF8);

        var paths = Regex.Matches(sql, @"'(/[^']+)'")
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        Assert.Equal(131, paths.Count);

        foreach (var rel in paths)
        {
            var full = Path.Combine(wwwroot, rel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(full), $"Missing physical asset for seed path: {rel} ({full})");

            var bytes = ReadSharedBytes(full);
            Assert.True(bytes.Length > 1000, $"Physical asset too small ({bytes.Length} bytes): {rel}");

            var ext = Path.GetExtension(full).ToLowerInvariant();
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    Assert.True(bytes.Length > 15_000, $"JPEG photo unexpectedly small ({bytes.Length} bytes): {rel}");
                    Assert.True(bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF, $"Invalid JPEG header for {rel}");
                    break;
                case ".pdf":
                    Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
                    break;
                case ".docx":
                    Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2));
                    break;
                default:
                    Assert.Fail($"Unexpected asset extension: {rel}");
                    break;
            }
        }
    }

    private static byte[] ReadSharedBytes(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task StaticMediaEndpoints_DeviceCardExport_BackupJson_AndSyncPermissions_WorkEndToEnd()
    {
        var client = _factory.CreateClient();

        // 1. Đăng nhập với Người dùng thiết bị (op.cuong)
        var opLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { TenDangNhap = "op.cuong", MatKhau = "123456" });
        var opToken = (await opLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opToken);

        // Chặn sync-work-orders và backup-json đối với Người dùng thiết bị (403)
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/kehoach/sync-work-orders", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/hethong/backup-json")).StatusCode);

        // Tải toàn bộ 6 biểu mẫu chuẩn BM-01..BM-06 qua HTTP -> 200 OK
        foreach (var bm in new[] { "BM-01", "BM-02", "BM-03", "BM-04", "BM-05", "BM-06" })
        {
            var res = await client.GetAsync($"/api/bieumau/{bm}/download");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var b = await res.Content.ReadAsByteArrayAsync();
            Assert.True(b.Length > 1500);
        }

        // Xem ảnh thiết bị, tài liệu PDF hướng dẫn, và xuất Phiếu lý lịch PDF qua HTTP
        var imgRes = await client.GetAsync("/api/thietbi/TB-CNC-01/anh");
        Assert.Equal(HttpStatusCode.OK, imgRes.StatusCode);
        var imgBytes = await imgRes.Content.ReadAsByteArrayAsync();
        Assert.True(imgBytes[0] == 0xFF && imgBytes[1] == 0xD8);

        var pdfRes = await client.GetAsync("/api/thietbi/TB-CNC-01/tai-lieu");
        Assert.Equal(HttpStatusCode.OK, pdfRes.StatusCode);
        var pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));

        var lyLichPdf = await client.GetAsync("/api/thietbi/TB-CNC-01/ly-lich/export?format=pdf");
        Assert.Equal(HttpStatusCode.OK, lyLichPdf.StatusCode);
        var llBytes = await lyLichPdf.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(llBytes, 0, 5));

        // Tạo yêu cầu báo hỏng gắn ảnh /anh-su-co/YC-2026-CM001.jpg và tải lại ảnh qua GET /api/yeucau/{id}/anh
        var ycCreate = await client.PostAsJsonAsync("/api/yeucau", new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01",
            MoTaTrieuChung = "Kiểm tra ảnh sự cố qua HTTP",
            DuongDanAnhSuCo = "/anh-su-co/YC-2026-CM001.jpg",
            MayDangDung = false,
            MucUuTienDeXuatId = 3
        });
        Assert.Equal(HttpStatusCode.OK, ycCreate.StatusCode);
        var ycId = (await ycCreate.Content.ReadFromJsonAsync<ApiResponse<YeuCauBaoTriDto>>())!.Data!.YeuCauId;

        var ycImg = await client.GetAsync($"/api/yeucau/{ycId}/anh");
        Assert.Equal(HttpStatusCode.OK, ycImg.StatusCode);
        var ycImgBytes = await ycImg.Content.ReadAsByteArrayAsync();
        Assert.True(ycImgBytes[0] == 0xFF && ycImgBytes[1] == 0xD8);

        // 2. Đăng nhập Admin -> gọi GET /api/hethong/backup-json -> 200 OK, JSON không chứa MatKhauHash & ghi audit SaoLuuDuLieu
        var adminLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { TenDangNhap = "admin.hung", MatKhau = "123456" });
        var adminToken = (await adminLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var backupRes = await client.GetAsync("/api/hethong/backup-json");
        Assert.Equal(HttpStatusCode.OK, backupRes.StatusCode);
        var jsonText = await backupRes.Content.ReadAsStringAsync();
        Assert.DoesNotContain("MatKhauHash", jsonText);
        using var doc = JsonDocument.Parse(jsonText);
        Assert.Equal("cmms_db", doc.RootElement.GetProperty("Database").GetString());

        // 3. Đăng nhập Trưởng BP (lead.trong) -> sync-work-orders được phép -> 200 OK
        var leadLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { TenDangNhap = "lead.trong", MatKhau = "123456" });
        var leadToken = (await leadLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leadToken);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/kehoach/sync-work-orders", null)).StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _conn.Dispose();
    }
}
