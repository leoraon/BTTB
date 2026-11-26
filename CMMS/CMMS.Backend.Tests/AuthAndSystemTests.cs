using System.Text;
using CMMS.Backend.Helpers;
using CMMS.Backend.Security;
using CMMS.Backend.Services.Implementations;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace CMMS.Backend.Tests;

public class AuthAndSystemTests
{
    private static AuthService CreateAuthService(TestDbContextHandle h)
    {
        var jwt = new JwtTokenHelper(Options.Create(new JwtSettings
        {
            SecretKey = "CMMS_Test_Secret_Key_Must_Be_At_Least_32_Bytes!",
            Issuer = "CMMS.Backend",
            Audience = "CMMS.WinForms",
            ExpireMinutes = 60
        }));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SeedDefaultPassword"] = "123456"
        }).Build();
        return new AuthService(h.Db, jwt, new AuditLogHelper(h.Db), config);
    }

    [Fact]
    public async Task Login_PlaceholderHash_ShouldUpgradeToBCrypt_AndFiveWrongAttempts_LockFor15Minutes()
    {
        using var h = new TestDbContextHandle();
        var auth = CreateAuthService(h);

        var okRes = await auth.LoginAsync(new LoginRequest { TenDangNhap = "admin.hung", MatKhau = "123456" });
        Assert.True(okRes.Success);
        Assert.NotEmpty(okRes.Data!.Token);
        Assert.Contains(MaQuyen.USER_MANAGE, okRes.Data.Permissions);

        var tk = h.Db.TAI_KHOAN.First(t => t.TenDangNhap == "admin.hung");
        Assert.NotEqual("$2a$12$e8YQ6zD5iF2N7uF7Q/E5y.v5A6sOqE.6i9nNf9qWj5sOqE6i9nNf9", tk.MatKhauHash);

        for (int i = 0; i < 5; i++)
        {
            var bad = await auth.LoginAsync(new LoginRequest { TenDangNhap = "admin.hung", MatKhau = "sai_mat_khau" });
            Assert.False(bad.Success);
        }

        Assert.NotNull(tk.ThoiDiemMoKhoaTam);
        Assert.True(tk.ThoiDiemMoKhoaTam > DateTime.Now);

        var blocked = await auth.LoginAsync(new LoginRequest { TenDangNhap = "admin.hung", MatKhau = "123456" });
        Assert.False(blocked.Success);

        Assert.Contains(h.Db.NHAT_KY_HE_THONG, log => log.LoaiThaoTac == CmmsConstants.LoaiThaoTacLog.KhoaTaiKhoan);
    }

    [Fact]
    public async Task ProposeAndApproveAccount_ReproposeRejected_DeactivateLogs_AndRolePermissions()
    {
        using var h = new TestDbContextHandle();
        var audit = new AuditLogHelper(h.Db);
        var nhanSu = new NhanSuService(h.Db, audit);
        var phanQuyen = new PhanQuyenService(h.Db, audit);
        var auth = CreateAuthService(h);

        // Tạo NV mới -> Đề xuất tài khoản -> Từ chối -> Đề xuất lại được phép -> Duyệt -> Đăng nhập OK
        var nvRes = await nhanSu.CreateNhanVienAsync(new NhanVienUpsertReq
        {
            NhanVienId = "NV099", HoTen = "Nhân Viên Mới", SoDienThoai = "0988888888",
            Email = "nv099@cmms.vn", DonViId = "DC-CNC-01", ChucVu = "Vận hành"
        });
        Assert.True(nvRes.Success);

        var propRes = await nhanSu.DeXuatTaiKhoanAsync(new DeXuatTaiKhoanReq
        {
            NhanVienId = "NV099", TenDangNhap = "op.moi", MatKhauKhoiTao = "secret123", VaiTroId = 5
        }, actorTaiKhoanId: 1);
        Assert.True(propRes.Success);
        Assert.Equal("ChoDuyet", propRes.Data!.TrangThaiTaiKhoan);

        Assert.True((await nhanSu.PheDuyetTaiKhoanAsync(propRes.Data.TaiKhoanId,
            new PheDuyetTaiKhoanReq { PheDuyet = false, GhiChu = "Thiếu thông tin vai trò" }, "NV001", actorTaiKhoanId: 2)).Success);
        Assert.Equal("TuChoi", h.Db.TAI_KHOAN.Find(propRes.Data.TaiKhoanId)!.TrangThaiTaiKhoan);

        // Đề xuất lại sau khi bị TuChoi
        var repropose = await nhanSu.DeXuatTaiKhoanAsync(new DeXuatTaiKhoanReq
        {
            NhanVienId = "NV099", TenDangNhap = "op.moi.v2", MatKhauKhoiTao = "secret123", VaiTroId = 5
        }, actorTaiKhoanId: 1);
        Assert.True(repropose.Success);
        Assert.Equal("ChoDuyet", repropose.Data!.TrangThaiTaiKhoan);

        var loginBeforeApprove = await auth.LoginAsync(new LoginRequest { TenDangNhap = "op.moi.v2", MatKhau = "secret123" });
        Assert.False(loginBeforeApprove.Success);

        var approveRes = await nhanSu.PheDuyetTaiKhoanAsync(repropose.Data.TaiKhoanId,
            new PheDuyetTaiKhoanReq { PheDuyet = true }, "NV001", actorTaiKhoanId: 2);
        Assert.True(approveRes.Success);

        var loginAfterApprove = await auth.LoginAsync(new LoginRequest { TenDangNhap = "op.moi.v2", MatKhau = "secret123" });
        Assert.True(loginAfterApprove.Success);

        // Chuyển DaNghiViec qua UpdateNhanVienAsync -> khóa tài khoản & ghi Nhật ký hệ thống
        var deactivate = await nhanSu.UpdateNhanVienAsync("NV099", new NhanVienUpsertReq
        {
            NhanVienId = "NV099", HoTen = "Nhân Viên Mới", SoDienThoai = "0988888888",
            Email = "nv099@cmms.vn", DonViId = "DC-CNC-01", ChucVu = "Vận hành", TrangThai = CmmsConstants.TrangThaiNhanVien.DaNghiViec
        }, actorTaiKhoanId: 1);
        Assert.True(deactivate.Success);
        Assert.Equal(CmmsConstants.TrangThaiTaiKhoan.DangKhoa, h.Db.TAI_KHOAN.Find(repropose.Data.TaiKhoanId)!.TrangThaiTaiKhoan);
        Assert.Contains(h.Db.NHAT_KY_HE_THONG, l => l.DoiTuongTacDong == "NV099" && l.LoaiThaoTac == CmmsConstants.LoaiThaoTacLog.KhoaTaiKhoan);

        // Cập nhật phân quyền vai trò 5
        var permRes = await phanQuyen.CapNhatQuyenVaiTroAsync(5,
            new CapNhatQuyenVaiTroReq { DanhSachQuyen = [MaQuyen.REQUEST_CREATE, MaQuyen.WO_ACCEPTANCE_SIGN] }, 1);
        Assert.True(permRes.Success);
        Assert.Equal(2, h.Db.VAI_TRO_QUYEN.Count(q => q.VaiTroId == 5));
    }

    [Fact]
    public async Task BieuMau_Download_ShouldResolveAllSixStandardFiles()
    {
        using var h = new TestDbContextHandle();
        var wwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CMMS.Backend", "wwwroot"));
        var env = new FakeWebHostEnv(wwwroot);
        var svc = new BieuMauService(h.Db, new FileStorageHelper(env));

        var all = await svc.GetAllAsync();
        Assert.True(all.Success);
        Assert.Equal(6, all.Data!.Count);

        foreach (var bm in all.Data)
        {
            var dl = await svc.DownloadAsync(bm.BieuMauId);
            Assert.True(dl.Found, $"Missing template file for {bm.BieuMauId}");
            Assert.True(dl.Bytes.Length > 1500, $"Template {bm.BieuMauId} looks too small ({dl.Bytes.Length} bytes)");
            if (bm.DinhDangTep == "PDF")
                Assert.Equal("%PDF-", Encoding.ASCII.GetString(dl.Bytes, 0, 5));
            else
                Assert.Equal("PK", Encoding.ASCII.GetString(dl.Bytes, 0, 2));
        }
    }

    private sealed class FakeWebHostEnv(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "CMMS.Backend";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}
