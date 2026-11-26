using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMMS.Backend.Entities;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CMMS.Backend.Tests;

public class ApiIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests()
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
    public async Task HttpPipeline_EnforcesJwt401_Role403_AndAllowsAuthorizedRequests()
    {
        var client = _factory.CreateClient();

        // 1. Chưa đăng nhập -> 401 Unauthorized
        var unauth = await client.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        // 2. Đăng nhập với Người dùng thiết bị (op.cuong / 123456)
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { TenDangNhap = "op.cuong", MatKhau = "123456" });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginPayload = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.True(loginPayload!.Success);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Data!.Token);

        // 3. Thử gọi API chỉ dành cho Quản lý nhà máy -> bị chặn 403 Forbidden
        var forbidden = await client.PutAsync("/api/capnhatkho/CNK-001/xac-nhan?laXacNhan=true", null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        // 4. Tải biểu mẫu chuẩn BM-01 (mở chung mọi người dùng, không phân quyền) -> 200 OK
        var templateRes = await client.GetAsync("/api/bieumau/BM-01/download");
        Assert.Equal(HttpStatusCode.OK, templateRes.StatusCode);
        var bytes = await templateRes.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);

        // 5. Truy cập cây thiết bị khi đã xác thực -> 200 OK
        var treeRes = await client.GetAsync("/api/thietbi/tree");
        Assert.Equal(HttpStatusCode.OK, treeRes.StatusCode);

        // 6. OpenAPI specification endpoint (Development) -> 200 OK
        var openApiRes = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApiRes.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _conn.Dispose();
    }
}
