using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Helpers.Exporters;
using CMMS.Backend.Security;
using CMMS.Backend.Services.Background;
using CMMS.Backend.Services.Implementations;
using CMMS.Backend.Services.Interfaces;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 1. Cấu hình MySQL 8.0 qua MySql.EntityFrameworkCore (.NET 10)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=cmms_db;User=root;Password=;";
builder.Services.AddDbContext<CmmsDbContext>(options => options.UseMySQL(connStr));

// 2. Cấu hình JWT Authentication, Cấu hình Kho & Bộ lập lịch PM tự động
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<InventorySettings>(builder.Configuration.GetSection("InventorySettings"));
builder.Services.Configure<PmSchedulerSettings>(builder.Configuration.GetSection("PmSchedulerSettings"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PmSyncLock>();
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings
{
    SecretKey = "CMMS_Default_Dev_Secret_Key_Change_In_Prod_2026!"
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var idVal = ctx.Principal?.FindFirst("taiKhoanId")?.Value;
                if (!int.TryParse(idVal, out int tkId))
                {
                    ctx.Fail("Token thiếu thông tin tài khoản.");
                    return;
                }

                var db = ctx.HttpContext.RequestServices.GetRequiredService<CmmsDbContext>();
                var timeProvider = ctx.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                var now = timeProvider.GetLocalNow().DateTime;

                var tk = await db.TAI_KHOAN.AsNoTracking()
                    .Include(t => t.NhanVien)
                    .Include(t => t.VaiTro).ThenInclude(v => v.VAI_TRO_QUYEN)
                    .FirstOrDefaultAsync(t => t.TaiKhoanId == tkId, ctx.HttpContext.RequestAborted);

                if (tk == null
                    || tk.TrangThaiTaiKhoan != CmmsConstants.TrangThaiTaiKhoan.DangHoatDong
                    || tk.NhanVien.TrangThai != CmmsConstants.TrangThaiNhanVien.DangLamViec
                    || (tk.ThoiDiemMoKhoaTam.HasValue && tk.ThoiDiemMoKhoaTam.Value > now))
                {
                    ctx.Fail("Phiên đăng nhập không còn hiệu lực.");
                    return;
                }

                if (ctx.Principal?.Identity is ClaimsIdentity identity)
                {
                    var oldClaims = identity.Claims
                        .Where(c => c.Type is "perm" or "vaiTroId" or ClaimTypes.Role)
                        .ToList();
                    foreach (var c in oldClaims)
                        identity.RemoveClaim(c);

                    identity.AddClaim(new Claim("vaiTroId", tk.VaiTroId.ToString()));
                    identity.AddClaim(new Claim(ClaimTypes.Role, tk.VaiTro.TenVaiTro));
                    foreach (var perm in tk.VaiTro.VAI_TRO_QUYEN.Select(q => q.MaQuyen).Distinct())
                        identity.AddClaim(new Claim("perm", perm));
                }
            },
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                    ApiResponse<object>.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.")));
            }
        };
    });

builder.Services.AddAuthorization();

// 3. Đăng ký Helpers, Exporters (Strategy Pattern OOP), Background Service và 14 Services
builder.Services.AddScoped<JwtTokenHelper>();
builder.Services.AddScoped<AuditLogHelper>();
builder.Services.AddScoped<FileStorageHelper>();
builder.Services.AddHostedService<PmAutoSyncBackgroundService>();

builder.Services.AddSingleton<IReportExporter, ExcelReportExporter>();
builder.Services.AddSingleton<IReportExporter, PdfReportExporter>();

// --- SERVICES TV A ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INhanSuService, NhanSuService>();
builder.Services.AddScoped<IPhanQuyenService, PhanQuyenService>();
builder.Services.AddScoped<INhatKyService, NhatKyService>();
builder.Services.AddScoped<IYeuCauService, YeuCauService>();
builder.Services.AddScoped<ILenhBaoTriService, LenhBaoTriService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// --- SERVICES TV B ---
builder.Services.AddScoped<IBieuMauService, BieuMauService>();
builder.Services.AddScoped<IDanhMucService, DanhMucService>();
builder.Services.AddScoped<IThietBiService, ThietBiService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<IKeHoachService, KeHoachService>();
builder.Services.AddScoped<IKhoService, KhoService>();
builder.Services.AddScoped<IBaoCaoService, BaoCaoService>();

var app = builder.Build();

// Middleware bắt lỗi tổng để luôn trả JSON chuẩn ApiResponse
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(
            ApiResponse<object>.Fail($"Lỗi hệ thống: {ex.Message}")));
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("CMMS API Reference (.NET 10)");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
