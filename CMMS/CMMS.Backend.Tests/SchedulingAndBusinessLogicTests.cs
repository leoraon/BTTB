using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Helpers.Exporters;
using CMMS.Backend.Security;
using CMMS.Backend.Services.Implementations;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace CMMS.Backend.Tests;

public class SchedulingAndBusinessLogicTests
{
    [Theory]
    [InlineData(CmmsConstants.DonViChuKy.Ngay, 10, "2026-03-01", "2026-03-01", "2026-03-01", "2026-03-11")]
    [InlineData(CmmsConstants.DonViChuKy.Tuan, 2, "2026-03-01", "2026-03-01", "2026-03-04", "2026-03-18")]
    [InlineData(CmmsConstants.DonViChuKy.Thang, 1, "2026-01-31", "2026-02-28", "2026-02-28", "2026-03-31")]
    [InlineData(CmmsConstants.DonViChuKy.Thang, 1, "2026-01-31", "2026-03-31", "2026-03-31", "2026-04-30")]
    [InlineData(CmmsConstants.DonViChuKy.Thang, 1, "2026-01-31", "2026-04-30", "2026-04-30", "2026-05-31")]
    [InlineData(CmmsConstants.DonViChuKy.Quy, 1, "2026-01-31", "2026-01-31", "2026-02-02", "2026-05-02")]
    [InlineData(CmmsConstants.DonViChuKy.Quy, 1, "2026-01-31", "2026-11-30", "2026-11-30", "2027-02-28")]
    [InlineData(CmmsConstants.DonViChuKy.Nam, 1, "2024-02-29", "2024-02-29", "2024-02-29", "2025-02-28")]
    [InlineData(CmmsConstants.DonViChuKy.Nam, 1, "2024-02-29", "2027-02-28", "2027-02-28", "2028-02-29")]
    [InlineData(CmmsConstants.DonViChuKy.Ngay, 1, "2026-03-01", "2026-03-05", "2026-03-04", "2026-03-06")]
    [InlineData(CmmsConstants.DonViChuKy.Thang, 1, "2026-01-31", "2026-03-31", "2026-02-28", "2026-04-30")]
    public void CycleMath_ShouldHandleAllFiveUnits_MonthEndDrift_LeapYear_AndEarlyCompletion(
        string unit, int value, string anchorStr, string currentDueStr, string actualStr, string expectedStr)
    {
        var anchor = DateOnly.Parse(anchorStr);
        var currentDue = DateOnly.Parse(currentDueStr);
        var actual = DateOnly.Parse(actualStr);
        var expected = DateOnly.Parse(expectedStr);

        var next = MaintenanceScheduleHelper.CalculateNextDueDateAfterCompletion(
            currentDue, actual, unit, value, anchor);

        Assert.Equal(expected, next);
    }

    [Fact]
    public async Task MultiCycleTimeTravel_ShiftAwareSync_UpdateRecomputesDue_ManualActualDates_AndForecast()
    {
        using var h = new TestDbContextHandle();
        var clock = new MutableTimeProvider(new DateTime(2026, 1, 1, 8, 0, 0));
        var audit = new AuditLogHelper(h.Db, clock);
        var syncLock = new PmSyncLock();
        var khSvc = new KeHoachService(h.Db, audit, clock, syncLock);
        var lenhSvc = new LenhBaoTriService(h.Db, audit, clock);
        var ycSvc = new YeuCauService(h.Db, new FileStorageHelper(new DummyEnv(Path.GetTempPath())), clock);

        // Thêm nhóm Robot Hàn có duy nhất KTV Ca 2 (14:00 - 22:00) để kiểm chứng lập lịch theo ca từng loại máy
        h.Db.NHOM_THIET_BI.Add(new NHOM_THIET_BI { NhomThietBiId = "NHOM-ROBOT-HAN", TenNhom = "Robot Hàn", LaCumChiTiet = false });
        h.Db.THIET_BI.Add(new THIET_BI
        {
            ThietBiId = "TB-RB-01", TenThietBi = "Robot Hàn Yaskawa", NhomThietBiId = "NHOM-ROBOT-HAN",
            ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "AR1440", Serial = "SN-RB-01",
            NhaSanXuat = "Yaskawa", NgayDuaVaoSuDung = new DateOnly(2024, 1, 1), ChuKyBaoTriKhuyenNghiNgay = 30,
            CongDungMoTa = "Hàn tự động", TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DangHoatDong
        });
        h.Db.NHAN_VIEN.Add(new NHAN_VIEN
        {
            NhanVienId = "NV009", HoTen = "Vũ Ca Hai", SoDienThoai = "0900000009", Email = "ca2@cmms.vn",
            DonViId = "BP-BAOTRI", ChucVu = "KTV", CaLamViec = CmmsConstants.CaLamViec.Ca2,
            TrangThaiSanSang = CmmsConstants.TrangThaiSanSang.SanSang, TrangThai = CmmsConstants.TrangThaiNhanVien.DangLamViec
        });
        h.Db.CHUYEN_MON_KTV.Add(new CHUYEN_MON_KTV { NhanVienId = "NV009", NhomThietBiId = "NHOM-ROBOT-HAN", GhiChu = "Chính" });
        h.Db.MAU_CHECKLIST.Add(new MAU_CHECKLIST
        {
            MauChecklistId = "CK-RB-01", TenMau = "Checklist Robot", NhomThietBiId = "NHOM-ROBOT-HAN", TrangThaiHieuLuc = "DangApDung",
            HangMucs = [new HANG_MUC_CHECKLIST { NhomBoPhan = "Mỏ hàn", NoiDungCongViec = "Vệ sinh béc", KieuKetQua = "Dat_KhongDat", TieuChuanYeuCau = "Sạch", ThuTuThucHien = 1 }]
        });
        await h.Db.SaveChangesAsync();

        // Chặn SoNgaySinhLenhTruoc > chu kỳ (Ngay = 2 nhưng sinh trước 5 ngày)
        var badLeadTime = await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-ERR-LEAD", TenKeHoach = "Sai ngày sinh lệnh", ThietBiId = "TB-RB-01",
            MauChecklistId = "CK-RB-01", DonViChuKy = "Ngay", GiaTriChuKy = 2, SoNgaySinhLenhTruoc = 5,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 1, 10)
        });
        Assert.False(badLeadTime.Success);

        // Tạo kế hoạch 2 tháng (> khuyến nghị 30 ngày -> cảnh báo)
        var createRes = await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-RB-01", TenKeHoach = "Bảo dưỡng Robot định kỳ", ThietBiId = "TB-RB-01",
            MauChecklistId = "CK-RB-01", DonViChuKy = "Thang", GiaTriChuKy = 2, SoNgaySinhLenhTruoc = 3,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 1, 10)
        });
        Assert.True(createRes.Success);
        Assert.Single(createRes.Warnings);

        // Chặn tạo trùng kế hoạch DangApDung cho cùng máy + mẫu checklist
        var dupPlan = await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-RB-DUP", TenKeHoach = "Trùng", ThietBiId = "TB-RB-01",
            MauChecklistId = "CK-RB-01", DonViChuKy = "Thang", GiaTriChuKy = 1, SoNgaySinhLenhTruoc = 1,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 1, 10)
        });
        Assert.False(dupPlan.Success);

        // Cập nhật chu kỳ trước khi sinh lệnh -> tự cập nhật NgayDenHanTiepTheo theo NgayBatDauApDung mới
        var updRes = await khSvc.UpdateAsync("KH-RB-01", new KeHoachUpsertReq
        {
            TenKeHoach = "Bảo dưỡng Robot định kỳ", ThietBiId = "TB-RB-01", MauChecklistId = "CK-RB-01",
            DonViChuKy = "Thang", GiaTriChuKy = 1, SoNgaySinhLenhTruoc = 3, ThoiGianDuKienPhut = 60,
            NgayBatDauApDung = new DateOnly(2026, 1, 31)
        });
        Assert.True(updRes.Success);
        Assert.Equal(new DateOnly(2026, 1, 31), updRes.Data!.NgayDenHanTiepTheo);

        // Tua thời gian tới 2026-01-29 (31 - 3 = 28 -> đến cửa sổ sinh lệnh) -> sinh lệnh bắt đầu lúc 14:00 (Ca 2)
        clock.SetLocalTime(new DateTime(2026, 1, 29, 9, 0, 0));
        var sync1 = await khSvc.SyncWorkOrdersAsync(3);
        Assert.Equal(1, sync1.Data!.SoLenhMoiSinh);
        var wo1Id = sync1.Data.DanhSachMaLenhMoi[0];
        var wo1 = h.Db.LENH_BAO_TRI.Find(wo1Id)!;
        Assert.Equal(new DateTime(2026, 1, 31, 14, 0, 0), wo1.ThoiGianDuKienBatDau);
        Assert.Equal(3, wo1.MucUuTienId);

        // Chặn đổi ThietBiId / MauChecklistId của kế hoạch khi đang có lệnh mở
        var blockPlanDeviceSwap = await khSvc.UpdateAsync("KH-RB-01", new KeHoachUpsertReq
        {
            TenKeHoach = "Đổi máy khi lệnh mở", ThietBiId = "TB-CNC-01", MauChecklistId = "CK-CNC-01",
            DonViChuKy = "Thang", GiaTriChuKy = 1, SoNgaySinhLenhTruoc = 3, ThoiGianDuKienPhut = 60,
            NgayBatDauApDung = new DateOnly(2026, 1, 31)
        });
        Assert.False(blockPlanDeviceSwap.Success);

        // Tạo kế hoạch CNC đã quá hạn ở ngày 2026-02-02 -> sync tự nâng ưu tiên lên 2 (Cao)
        clock.SetLocalTime(new DateTime(2026, 2, 4, 9, 0, 0));
        await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-CNC-OVD", TenKeHoach = "Kế hoạch quá hạn", ThietBiId = "TB-CNC-01",
            MauChecklistId = "CK-CNC-01", DonViChuKy = "Ngay", GiaTriChuKy = 15, SoNgaySinhLenhTruoc = 2,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 2, 2)
        });
        var syncOverdue = await khSvc.SyncWorkOrdersAsync(3);
        Assert.Equal(1, syncOverdue.Data!.SoLenhMoiSinh);
        var woOverdue = h.Db.LENH_BAO_TRI.Find(syncOverdue.Data.DanhSachMaLenhMoi[0])!;
        Assert.Equal(2, woOverdue.MucUuTienId);

        // Sự cố đột xuất chen giữa trên TB-RB-01 vào 2026-02-01..2026-02-02 KHÔNG làm xê dịch chu kỳ PM
        var yc = await ycSvc.TaoYeuCauAsync(new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-RB-01", MoTaTrieuChung = "Đứt dây hàn đột xuất", MayDangDung = false, MucUuTienDeXuatId = 2
        }, "NV013");
        await ycSvc.DuyetHoacTuChoiAsync(yc.Data!.YeuCauId, new DuyetYeuCauReq
        {
            LaPheDuyet = true, MaLoiId = "LOI-RO-RI-DAU", MucUuTienChinhThucId = 2, HuongXuLy = CmmsConstants.HuongXuLy.SuaChuaTheoKeHoach
        }, "NV002");
        var cmWo = (await lenhSvc.TaoLenhTuYeuCauAsync(new TaoLenhTuYeuCauReq
        {
            YeuCauId = yc.Data.YeuCauId, NoiDungCongViec = "Nối dây",
            ThoiGianDuKienBatDau = new DateTime(2026, 2, 1, 14, 0, 0), ThoiGianDuKienKetThuc = new DateTime(2026, 2, 1, 15, 0, 0)
        }, "NV002", 3)).Data!;
        Assert.True((await lenhSvc.BatDauLenhAsync(cmWo.LenhBaoTriId, "NV009")).Success);
        Assert.True((await lenhSvc.KetThucLenhAsync(cmWo.LenhBaoTriId, new KetThucLenhReq
        {
            ThoiDiemThucTeBatDau = new DateTime(2026, 2, 1, 14, 0, 0),
            ThoiDiemThucTeKetThuc = new DateTime(2026, 2, 1, 15, 0, 0)
        })).Success);
        Assert.True((await lenhSvc.LuuBaoCaoSuCoAsync(cmWo.LenhBaoTriId, new BaoCaoSuCoUpsertReq
        {
            MaLoiChinhThucId = "LOI-RO-RI-DAU", CoDungMay = false,
            MoTaHuHong = "Đứt dây", NguyenNhanGocRe = "Mài mòn", BienPhapKhacPhuc = "Thay dây"
        }, "NV009")).Success);
        Assert.True((await lenhSvc.NghiemThuLenhAsync(cmWo.LenhBaoTriId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV009", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);
        Assert.True((await lenhSvc.DongLenhAsync(cmWo.LenhBaoTriId, "NV002", 3)).Success);
        Assert.Equal(new DateOnly(2026, 1, 31), h.Db.KE_HOACH_BAO_TRI.Find("KH-RB-01")!.NgayDenHanTiepTheo);

        // Thực hiện lệnh PM wo1Id với ngày sửa thực tế nhập tay (2026-02-03 15:00)
        Assert.True((await lenhSvc.PhanCongKtvAsync(wo1Id, new PhanCongKtvReq
        {
            KyThuatVienId = "NV009",
            KhungGioBatDau = new DateTime(2026, 2, 3, 14, 0, 0),
            KhungGioKetThuc = new DateTime(2026, 2, 3, 16, 0, 0)
        }, "NV002")).Success);
        Assert.True((await lenhSvc.BatDauLenhAsync(wo1Id, "NV009")).Success);

        var rbHmId = h.Db.HANG_MUC_CHECKLIST.First(x => x.MauChecklistId == "CK-RB-01").HangMucId;
        Assert.True((await lenhSvc.LuuKetQuaChecklistAsync(wo1Id, [new() { HangMucId = rbHmId, KetLuan = "Dat" }], "NV009")).Success);
        Assert.True((await lenhSvc.KetThucLenhAsync(wo1Id, new KetThucLenhReq
        {
            ThoiDiemThucTeBatDau = new DateTime(2026, 2, 3, 14, 0, 0),
            ThoiDiemThucTeKetThuc = new DateTime(2026, 2, 3, 15, 0, 0)
        })).Success);
        Assert.True((await lenhSvc.NghiemThuLenhAsync(wo1Id, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV009", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);
        Assert.True((await lenhSvc.DongLenhAsync(wo1Id, "NV002", 3)).Success);

        var planAfterClose = h.Db.KE_HOACH_BAO_TRI.Find("KH-RB-01")!;
        Assert.Equal(new DateOnly(2026, 2, 3), planAfterClose.NgayHoanThanhGanNhat);
        Assert.Equal(new DateOnly(2026, 3, 3), planAfterClose.NgayDenHanTiepTheo);

        // Đổi chu kỳ kế hoạch sang Quý (1 quý) khi đã có NgayHoanThanhGanNhat -> tự tính lại từ 2026-02-03 thành 2026-05-03
        var changeToQuarter = await khSvc.UpdateAsync("KH-RB-01", new KeHoachUpsertReq
        {
            TenKeHoach = "Bảo dưỡng Robot định kỳ", ThietBiId = "TB-RB-01", MauChecklistId = "CK-RB-01",
            DonViChuKy = "Quy", GiaTriChuKy = 1, SoNgaySinhLenhTruoc = 3, ThoiGianDuKienPhut = 60,
            NgayBatDauApDung = new DateOnly(2026, 1, 31)
        });
        Assert.True(changeToQuarter.Success);
        Assert.Equal(new DateOnly(2026, 5, 3), changeToQuarter.Data!.NgayDenHanTiepTheo);

        // Tua sang 2026-05-01 -> tự sinh lệnh chu kỳ 2 cho KH-RB-01 lúc 14:00
        clock.SetLocalTime(new DateTime(2026, 5, 1, 8, 0, 0));
        var syncCycle2 = await khSvc.SyncWorkOrdersAsync(3);
        Assert.Equal(1, syncCycle2.Data!.SoLenhMoiSinh);
        var wo2Id = syncCycle2.Data.DanhSachMaLenhMoi[0];
        Assert.Equal(new DateTime(2026, 5, 3, 14, 0, 0), h.Db.LENH_BAO_TRI.Find(wo2Id)!.ThoiGianDuKienBatDau);

        // Dự báo lịch bảo trì 2026-05-01..2026-12-31 -> có đủ kỳ hiện tại (DaCoLenh) + các kỳ quý kế tiếp (2026-08-03, 2026-11-03)
        var forecast = await khSvc.GetLichDuKienAsync(new DateOnly(2026, 5, 1), new DateOnly(2026, 12, 31), "TB-RB-01");
        Assert.True(forecast.Success);
        Assert.Equal(3, forecast.Data!.Count);
        Assert.Equal(new DateOnly(2026, 5, 3), forecast.Data[0].NgayDuKien);
        Assert.Equal("DaCoLenh", forecast.Data[0].TrangThaiDuKien);
        Assert.Equal(new TimeOnly(14, 0), forecast.Data[0].GioBatDauDuKien);
        Assert.Equal(new DateOnly(2026, 8, 3), forecast.Data[1].NgayDuKien);
        Assert.Equal(new DateOnly(2026, 11, 3), forecast.Data[2].NgayDuKien);
    }

    [Fact]
    public async Task BackgroundAutoSync_OnStartup_ShouldGenerateDueWorkOrderAutomatically()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<CmmsDbContext>>();
                services.RemoveAll<DbContextOptions<CmmsDbContext>>();
                services.RemoveAll<CmmsDbContext>();
                services.AddDbContext<CmmsDbContext>(opt => opt.UseSqlite(conn));

                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CmmsDbContext>();
                db.Database.EnsureCreated();
                TestDbContextHandle.Seed(db);

                db.KE_HOACH_BAO_TRI.Add(new KE_HOACH_BAO_TRI
                {
                    KeHoachId = "KH-AUTO-01",
                    TenKeHoach = "Tự động đồng bộ khi khởi động",
                    ThietBiId = "TB-CNC-01",
                    MauChecklistId = "CK-CNC-01",
                    DonViChuKy = CmmsConstants.DonViChuKy.Thang,
                    GiaTriChuKy = 1,
                    SoNgaySinhLenhTruoc = 3,
                    ThoiGianDuKienPhut = 60,
                    NgayBatDauApDung = DateOnly.FromDateTime(DateTime.Today),
                    NgayDenHanTiepTheo = DateOnly.FromDateTime(DateTime.Today),
                    TrangThaiKeHoach = CmmsConstants.TrangThaiKeHoach.DangApDung
                });
                db.SaveChanges();
            });
        });

        var client = factory.CreateClient();
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { TenDangNhap = "lead.trong", MatKhau = "123456" });
        var token = (await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        List<LenhBaoTriDto> orders = [];
        for (int i = 0; i < 40; i++)
        {
            var res = await client.GetFromJsonAsync<ApiResponse<List<LenhBaoTriDto>>>("/api/lenh?loaiLenh=BaoTriPhongNgua");
            orders = res?.Data ?? [];
            if (orders.Any(o => o.KeHoachId == "KH-AUTO-01"))
                break;
            await Task.Delay(75);
        }

        Assert.Contains(orders, o => o.KeHoachId == "KH-AUTO-01");
    }

    [Fact]
    public async Task WorkOrderStateMachine_UnassignRevertsToMoiTao_BlocksContinueWithoutKtv_LocksPostAcceptance_AndKeepsIncidentStoppage()
    {
        using var h = new TestDbContextHandle();
        var clock = new MutableTimeProvider(new DateTime(2026, 3, 10, 8, 0, 0));
        var audit = new AuditLogHelper(h.Db, clock);
        var khSvc = new KeHoachService(h.Db, audit, clock, new PmSyncLock());
        var lenhSvc = new LenhBaoTriService(h.Db, audit, clock);
        var ycSvc = new YeuCauService(h.Db, new FileStorageHelper(new DummyEnv(Path.GetTempPath())), clock);

        await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-SM-01", TenKeHoach = "Kiểm tra vòng đời", ThietBiId = "TB-CNC-01",
            MauChecklistId = "CK-CNC-01", DonViChuKy = "Ngay", GiaTriChuKy = 15, SoNgaySinhLenhTruoc = 3,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 3, 10)
        });
        var pmId = (await khSvc.SyncWorkOrdersAsync(3)).Data!.DanhSachMaLenhMoi[0];

        // 1. Giao KTV -> DaPhanCong; gỡ KTV cuối -> hoàn trả MoiTao
        var pc = await lenhSvc.PhanCongKtvAsync(pmId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = new DateTime(2026, 3, 10, 8, 0, 0), KhungGioKetThuc = new DateTime(2026, 3, 10, 10, 0, 0)
        }, "NV002");
        Assert.Equal(CmmsConstants.TrangThaiLenh.DaPhanCong, h.Db.LENH_BAO_TRI.Find(pmId)!.TrangThaiLenh);
        Assert.True((await lenhSvc.HuyPhanCongKtvAsync(pc.Data!.PhanCongId)).Success);
        Assert.Equal(CmmsConstants.TrangThaiLenh.MoiTao, h.Db.LENH_BAO_TRI.Find(pmId)!.TrangThaiLenh);

        // 2. Giao lại -> Bắt đầu -> Bị chen ngang khẩn cấp -> PM tạm dừng và không còn KTV active -> chặn Tiếp tục
        Assert.True((await lenhSvc.PhanCongKtvAsync(pmId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = new DateTime(2026, 3, 10, 8, 0, 0), KhungGioKetThuc = new DateTime(2026, 3, 10, 10, 0, 0)
        }, "NV002")).Success);
        Assert.True((await lenhSvc.BatDauLenhAsync(pmId, "NV003")).Success);

        var ycUrgent = (await ycSvc.TaoYeuCauAsync(new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Sự cố khẩn", MayDangDung = true, MucUuTienDeXuatId = 1
        }, "NV013")).Data!;
        await ycSvc.DuyetHoacTuChoiAsync(ycUrgent.YeuCauId, new DuyetYeuCauReq
        {
            LaPheDuyet = true, MaLoiId = "LOI-QUA-NHIET", MucUuTienChinhThucId = 1, HuongXuLy = CmmsConstants.HuongXuLy.SuaChuaKhanCap
        }, "NV002");
        var cmId = (await lenhSvc.TaoLenhTuYeuCauAsync(new TaoLenhTuYeuCauReq
        {
            YeuCauId = ycUrgent.YeuCauId, NoiDungCongViec = "Sửa khẩn",
            ThoiGianDuKienBatDau = new DateTime(2026, 3, 10, 8, 0, 0), ThoiGianDuKienKetThuc = new DateTime(2026, 3, 10, 9, 0, 0)
        }, "NV002", 3)).Data!.LenhBaoTriId;

        Assert.True((await lenhSvc.PhanCongKtvAsync(cmId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = new DateTime(2026, 3, 10, 8, 0, 0), KhungGioKetThuc = new DateTime(2026, 3, 10, 9, 0, 0),
            ChoPhepChenNgang = true, LyDoTamDung = "Ưu tiên sự cố"
        }, "NV002")).Success);

        Assert.False((await lenhSvc.TiepTucLenhAsync(pmId)).Success);

        // Chặn bắt đầu thêm lệnh thứ 2 cho NV003 khi NV003 đang DangLam
        Assert.True((await lenhSvc.BatDauLenhAsync(cmId, "NV003")).Success);
        Assert.True((await lenhSvc.PhanCongKtvAsync(pmId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = new DateTime(2026, 3, 10, 10, 0, 0), KhungGioKetThuc = new DateTime(2026, 3, 10, 11, 0, 0)
        }, "NV002")).Success);
        Assert.False((await lenhSvc.TiepTucLenhAsync(pmId)).Success);

        // Kết thúc lệnh khẩn cấp cmId chậm hơn dự kiến (09:30 > 09:00) -> kiểm tra khóa sau DaNghiemThu & IsTreHan = true
        clock.SetLocalTime(new DateTime(2026, 3, 10, 9, 30, 0));
        Assert.True((await lenhSvc.KetThucLenhAsync(cmId, new KetThucLenhReq())).Success);
        Assert.True((await lenhSvc.LuuBaoCaoSuCoAsync(cmId, new BaoCaoSuCoUpsertReq
        {
            MaLoiChinhThucId = "LOI-QUA-NHIET", CoDungMay = true,
            ThoiDiemXayRa = new DateTime(2026, 3, 10, 7, 50, 0), ThoiDiemKhoiPhuc = new DateTime(2026, 3, 10, 9, 30, 0),
            MoTaHuHong = "Nóng", NguyenNhanGocRe = "Mất dầu", BienPhapKhacPhuc = "Bơm dầu"
        }, "NV003")).Success);

        // Chặn người bàn giao trùng người ký nhận
        Assert.False((await lenhSvc.NghiemThuLenhAsync(cmId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV003", TinhTrangSauBaoTri = "DatChuan"
        })).Success);
        Assert.True((await lenhSvc.NghiemThuLenhAsync(cmId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);

        // Sau DaNghiemThu: chặn sửa báo cáo sự cố & chặn hủy lệnh
        Assert.False((await lenhSvc.LuuBaoCaoSuCoAsync(cmId, new BaoCaoSuCoUpsertReq
        {
            MaLoiChinhThucId = "LOI-QUA-NHIET", CoDungMay = false,
            MoTaHuHong = "Sửa lén", NguyenNhanGocRe = "X", BienPhapKhacPhuc = "Y"
        }, "NV003")).Success);
        Assert.False((await lenhSvc.HuyLenhAsync(cmId, new HuyLenhReq { LyDoHuy = "Hủy muộn" }, 3)).Success);

        Assert.True((await lenhSvc.DongLenhAsync(cmId, "NV002", 3)).Success);
        var cmDetail = await lenhSvc.GetDetailAsync(cmId);
        Assert.True(cmDetail.Data!.IsTreHan);

        // Bắt đầu lại lệnh PM (máy -> DangBaoTri); chặn đổi KTV đang DangLam sang NghiPhep; báo hỏng dừng máy mới không ghi đè DangBaoTri nhưng sẽ chuyển DungDoSuCo khi đóng lệnh PM!
        Assert.True((await lenhSvc.TiepTucLenhAsync(pmId)).Success);
        Assert.Equal(CmmsConstants.TrangThaiThietBi.DangBaoTri, h.Db.THIET_BI.Find("TB-CNC-01")!.TrangThaiVanHanh);

        var nhanSuSvc = new NhanSuService(h.Db, audit, clock);
        Assert.False((await nhanSuSvc.UpdateNhanVienAsync("NV003", new NhanVienUpsertReq
        {
            NhanVienId = "NV003", HoTen = "Lê Văn Ba", SoDienThoai = "0900000003", Email = "ba@cmms.vn",
            DonViId = "BP-BAOTRI", ChucVu = "KTV Cơ khí", CaLamViec = "Ca 1",
            TrangThaiSanSang = CmmsConstants.TrangThaiSanSang.NghiPhep, TrangThai = CmmsConstants.TrangThaiNhanVien.DangLamViec
        }, 1)).Success);

        await ycSvc.TaoYeuCauAsync(new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Chập điện mới", MayDangDung = true, MucUuTienDeXuatId = 1
        }, "NV013");
        Assert.Equal(CmmsConstants.TrangThaiThietBi.DangBaoTri, h.Db.THIET_BI.Find("TB-CNC-01")!.TrangThaiVanHanh);
        var hmIds = h.Db.HANG_MUC_CHECKLIST.Where(x => x.MauChecklistId == "CK-CNC-01").OrderBy(x => x.ThuTuThucHien).Select(x => x.HangMucId).ToList();
        Assert.True((await lenhSvc.LuuKetQuaChecklistAsync(pmId,
        [
            new() { HangMucId = hmIds[0], GiaTriDoThucTe = 60 },
            new() { HangMucId = hmIds[1], KetLuan = "Dat" }
        ], "NV003")).Success);
        Assert.True((await lenhSvc.KetThucLenhAsync(pmId, new KetThucLenhReq())).Success);
        Assert.True((await lenhSvc.NghiemThuLenhAsync(pmId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);

        // Sau DaNghiemThu: chặn sửa kết quả checklist
        Assert.False((await lenhSvc.LuuKetQuaChecklistAsync(pmId, [new() { HangMucId = hmIds[0], GiaTriDoThucTe = 55 }], "NV003")).Success);

        Assert.True((await lenhSvc.DongLenhAsync(pmId, "NV002", 3)).Success);
        Assert.Equal(CmmsConstants.TrangThaiThietBi.DungDoSuCo, h.Db.THIET_BI.Find("TB-CNC-01")!.TrangThaiVanHanh);
    }

    [Fact]
    public async Task Security_BlocksApprovalBypass_ImmediateJwtRevocation_AndPathTraversal()
    {
        using var h = new TestDbContextHandle();
        var audit = new AuditLogHelper(h.Db);
        var nhanSu = new NhanSuService(h.Db, audit);
        var phanQuyen = new PhanQuyenService(h.Db, audit);

        await nhanSu.CreateNhanVienAsync(new NhanVienUpsertReq
        {
            NhanVienId = "NV050", HoTen = "Thử Bảo Mật", SoDienThoai = "0950000050",
            Email = "sec@cmms.vn", DonViId = "DC-CNC-01", ChucVu = "Công nhân"
        });
        var pendingAcc = (await nhanSu.DeXuatTaiKhoanAsync(new DeXuatTaiKhoanReq
        {
            NhanVienId = "NV050", TenDangNhap = "op.pending", MatKhauKhoiTao = "secret123", VaiTroId = 5
        }, 1)).Data!;

        // Chặn khóa tài khoản ChoDuyet để lách mở khóa; chặn tự duyệt; chặn từ chối không lý do
        Assert.False((await nhanSu.KhoaMoTaiKhoanAsync(pendingAcc.TaiKhoanId, new KhoaMoTaiKhoanReq { Khoa = true }, 1)).Success);
        Assert.False((await nhanSu.PheDuyetTaiKhoanAsync(pendingAcc.TaiKhoanId, new PheDuyetTaiKhoanReq { PheDuyet = false }, "NV001", 2)).Success);
        Assert.False((await nhanSu.PheDuyetTaiKhoanAsync(pendingAcc.TaiKhoanId, new PheDuyetTaiKhoanReq { PheDuyet = true }, "NV050", 99)).Success);

        // Duyệt -> Nghỉ việc (tự khóa) -> Chặn mở khóa khi nhân viên vẫn DaNghiViec
        Assert.True((await nhanSu.PheDuyetTaiKhoanAsync(pendingAcc.TaiKhoanId, new PheDuyetTaiKhoanReq { PheDuyet = true }, "NV001", 2)).Success);
        Assert.True((await nhanSu.NgungCongTacNhanVienAsync("NV050", 1)).Success);
        Assert.False((await nhanSu.KhoaMoTaiKhoanAsync(pendingAcc.TaiKhoanId, new KhoaMoTaiKhoanReq { Khoa = false }, 1)).Success);

        // Chặn thu hồi ROLE_MANAGE duy nhất của vai trò 1 & chặn gán vai trò khác cho Admin hoạt động cuối cùng giữ ROLE_MANAGE
        Assert.False((await phanQuyen.CapNhatQuyenVaiTroAsync(1, new CapNhatQuyenVaiTroReq
        {
            DanhSachQuyen = [MaQuyen.USER_MANAGE, MaQuyen.SYSTEM_CONFIG]
        }, 1)).Success);
        Assert.False((await phanQuyen.GanVaiTroTaiKhoanAsync(1, 5, 1)).Success);

        // Chặn Path Traversal
        var wwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CMMS.Backend", "wwwroot"));
        var files = new FileStorageHelper(new DummyEnv(wwwroot));
        Assert.Null(files.ResolveExistingFile("/../../appsettings.json", "bieu-mau"));
        Assert.Null(files.ResolveExistingFile("..\\..\\CMMS.Backend.csproj", "tai-lieu"));

        var ycSvc = new YeuCauService(h.Db, files);
        Assert.False((await ycSvc.TaoYeuCauAsync(new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Test path", DuongDanAnhSuCo = "../../appsettings.json", MucUuTienDeXuatId = 3
        }, "NV013")).Success);

        // Kiểm chứng thu hồi quyền & khóa tài khoản có hiệu lực NGAY LẬP TỨC trên JWT đang giữ
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<CmmsDbContext>>();
                services.RemoveAll<DbContextOptions<CmmsDbContext>>();
                services.RemoveAll<CmmsDbContext>();
                services.AddDbContext<CmmsDbContext>(opt => opt.UseSqlite(conn));

                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CmmsDbContext>();
                db.Database.EnsureCreated();
                TestDbContextHandle.Seed(db);
            });
        });

        var client = factory.CreateClient();
        var adminClient = factory.CreateClient();
        var adminToken = (await (await adminClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { TenDangNhap = "admin.hung", MatKhau = "123456" })).Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!.Token;
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var opLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { TenDangNhap = "op.cuong", MatKhau = "123456" });
        var opData = (await opLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opData.Token);

        // Đang hợp lệ -> 200 OK khi gọi tạo yêu cầu
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/yeucau", new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Còn quyền", MucUuTienDeXuatId = 3
        })).StatusCode);

        // Thu hồi quyền REQUEST_CREATE của vai trò 5 -> token đang cầm bị chặn 403 NGAY LẬP TỨC
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync("/api/vaitro/5/quyen", new CapNhatQuyenVaiTroReq
        {
            DanhSachQuyen = [MaQuyen.DEVICE_STATUS_VIEW]
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/yeucau", new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Đã mất quyền", MucUuTienDeXuatId = 3
        })).StatusCode);

        // Khóa tài khoản op.cuong -> token đang cầm bị trả 401 NGAY LẬP TỨC
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/api/taikhoan/{opData.TaiKhoanId}/khoa-mo",
            new KhoaMoTaiKhoanReq { Khoa = true, LyDo = "Khóa khẩn" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/thietbi/tree")).StatusCode);
    }

    [Fact]
    public async Task Inventory_EquipmentGuards_AndBc1Bc3Consistency()
    {
        using var h = new TestDbContextHandle();
        var clock = new MutableTimeProvider(new DateTime(2026, 11, 20, 8, 30, 0));
        var audit = new AuditLogHelper(h.Db, clock);
        var invOpts = Options.Create(new InventorySettings { ReplenishmentStartHour = 7, ReplenishmentEndHour = 10, EnforceReplenishmentWindow = true });
        var kho = new KhoService(h.Db, audit, clock, invOpts);
        var dmSvc = new DanhMucService(h.Db);
        var tbSvc = new ThietBiService(h.Db, new FileStorageHelper(new DummyEnv(Path.GetTempPath())), [new PdfReportExporter(), new ExcelReportExporter()], clock);
        var khSvc = new KeHoachService(h.Db, audit, clock, new PmSyncLock());
        var lenhSvc = new LenhBaoTriService(h.Db, audit, clock);

        // 1. Chặn trùng MaPhieuKhoNgoai
        Assert.True((await kho.TaoPhieuCapNhatTonKhoAsync(new TaoPhieuCapNhatTonKhoReq
        {
            LoaiCapNhat = CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai,
            MaPhieuKhoNgoai = "NK-UNIQUE-01",
            ChiTiet = [new() { PhuTungId = "PT-DAU-VG68", SoLuongThayDoi = 5 }]
        }, "NV002")).Success);
        Assert.False((await kho.TaoPhieuCapNhatTonKhoAsync(new TaoPhieuCapNhatTonKhoReq
        {
            LoaiCapNhat = CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai,
            MaPhieuKhoNgoai = "NK-UNIQUE-01",
            ChiTiet = [new() { PhuTungId = "PT-DAU-VG68", SoLuongThayDoi = 5 }]
        }, "NV002")).Success);

        // Cảnh báo tồn khả dụng âm khi kiểm kê giảm dưới SoLuongDangDuTru
        h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongDangDuTru = 8;
        await h.Db.SaveChangesAsync();
        var kk = (await kho.TaoPhieuCapNhatTonKhoAsync(new TaoPhieuCapNhatTonKhoReq
        {
            LoaiCapNhat = CmmsConstants.LoaiCapNhatKho.DieuChinhKiemKe,
            GhiChu = "Hao hụt kiểm kê",
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThayDoi = -4 }]
        }, "NV002")).Data!;
        var kkConfirm = await kho.XacNhanCapNhatTonKhoAsync(kk.PhieuCapNhatId, true, "NV001", 2);
        Assert.True(kkConfirm.Success);
        Assert.Contains(kkConfirm.Warnings, w => w.Contains("tồn khả dụng âm"));
        h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongTonHienCo = 10;
        h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongDangDuTru = 0;
        await h.Db.SaveChangesAsync();

        // Chặn duyệt vượt vượt quá tồn thực tế & xóa SoLuongThucDung khi từ chối duyệt vượt
        await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-INV-01", TenKeHoach = "PM tháng", ThietBiId = "TB-CNC-01",
            MauChecklistId = "CK-CNC-01", DonViChuKy = "Ngay", GiaTriChuKy = 10, SoNgaySinhLenhTruoc = 3,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 11, 20)
        });
        var woId = (await khSvc.SyncWorkOrdersAsync(3)).Data!.DanhSachMaLenhMoi[0];
        var px = (await kho.TaoPhieuDuTruAsync(new TaoPhieuDuTruReq
        {
            LenhBaoTriId = woId, ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongDuTru = 2 }]
        }, "NV002")).Data!;
        var overOk = await kho.KhaiBaoThucDungAsync(px.PhieuXuatId, new KhaiBaoThucDungReq
        {
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThucDung = 25, GhiChuGiaiTrinh = "Vượt quá cả tồn kho" }]
        }, "NV003");
        Assert.True(overOk.Success);
        Assert.False((await kho.DuyetVuotDuTruAsync(px.PhieuXuatId, new DuyetVuotDuTruReq { LaPheDuyet = true }, "NV002")).Success);
        var rejectOver = await kho.DuyetVuotDuTruAsync(px.PhieuXuatId, new DuyetVuotDuTruReq { LaPheDuyet = false, GhiChu = "Nhập sai số" }, "NV002");
        Assert.True(rejectOver.Success);
        Assert.Null(rejectOver.Data!.ChiTiet[0].SoLuongThucDung);
        Assert.True((await kho.HuyPhieuDuTruAsync(px.PhieuXuatId)).Success);

        // Chặn đổi LaCumChiTiet nhóm đang dùng & chặn đổi nhóm máy đang có kế hoạch hoạt động
        Assert.False((await dmSvc.UpdateNhomThietBiAsync("NHOM-TIEN-CNC", new NhomThietBiDto
        {
            NhomThietBiId = "NHOM-TIEN-CNC", TenNhom = "Đổi sang cụm", LaCumChiTiet = true
        })).Success);

        h.Db.NHOM_THIET_BI.Add(new NHOM_THIET_BI { NhomThietBiId = "NHOM-KHAC", TenNhom = "Nhóm khác", LaCumChiTiet = false });
        await h.Db.SaveChangesAsync();
        Assert.False((await tbSvc.UpdateAsync("TB-CNC-01", new ThietBiUpsertReq
        {
            ThietBiId = "TB-CNC-01", TenThietBi = "Đổi nhóm", NhomThietBiId = "NHOM-KHAC",
            ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "Lynx 220", Serial = "SN-001",
            NhaSanXuat = "Doosan", NgayDuaVaoSuDung = new DateOnly(2023, 1, 15), ChuKyBaoTriKhuyenNghiNgay = 30,
            CongDungMoTa = "Tiện"
        })).Success);

        // 2. Đối soát BC1 vs BC3:
        // Đóng lệnh PM woId (hạn 2026-11-20) vào 2026-11-10 (Đúng hạn) -> KH-INV-01 tự nhảy hạn kế tiếp sang 2026-11-21 (ChuaThucHien trong tháng 11!)
        clock.SetLocalTime(new DateTime(2026, 11, 10, 8, 0, 0));
        Assert.True((await lenhSvc.BatDauLenhAsync(woId, "NV003")).Success);
        var hmIds = h.Db.HANG_MUC_CHECKLIST.Where(x => x.MauChecklistId == "CK-CNC-01").OrderBy(x => x.ThuTuThucHien).Select(x => x.HangMucId).ToList();
        Assert.True((await lenhSvc.LuuKetQuaChecklistAsync(woId,
        [
            new() { HangMucId = hmIds[0], GiaTriDoThucTe = 60 },
            new() { HangMucId = hmIds[1], KetLuan = "Dat" }
        ], "NV003")).Success);
        Assert.True((await lenhSvc.KetThucLenhAsync(woId, new KetThucLenhReq())).Success);
        Assert.True((await lenhSvc.NghiemThuLenhAsync(woId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);
        Assert.True((await lenhSvc.DongLenhAsync(woId, "NV002", 3)).Success);

        // Thêm 1 kế hoạch quá hạn tồn đọng từ tháng 10 (2026-10-15) trên máy thứ 2
        h.Db.THIET_BI.Add(new THIET_BI
        {
            ThietBiId = "TB-CNC-02", TenThietBi = "Máy Tiện 02", NhomThietBiId = "NHOM-TIEN-CNC",
            ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "M2", Serial = "SN-002",
            NhaSanXuat = "Doosan", NgayDuaVaoSuDung = new DateOnly(2024, 1, 1), ChuKyBaoTriKhuyenNghiNgay = 30,
            CongDungMoTa = "Tiện", TrangThaiVanHanh = CmmsConstants.TrangThaiThietBi.DangHoatDong
        });
        h.Db.KE_HOACH_BAO_TRI.Add(new KE_HOACH_BAO_TRI
        {
            KeHoachId = "KH-OCT-OVERDUE", TenKeHoach = "Quá hạn từ tháng 10", ThietBiId = "TB-CNC-02",
            MauChecklistId = "CK-CNC-01", DonViChuKy = "Thang", GiaTriChuKy = 1, SoNgaySinhLenhTruoc = 3,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = new DateOnly(2026, 10, 15),
            NgayDenHanTiepTheo = new DateOnly(2026, 10, 15), TrangThaiKeHoach = CmmsConstants.TrangThaiKeHoach.DangApDung
        });
        await h.Db.SaveChangesAsync();

        clock.SetLocalTime(new DateTime(2026, 11, 20, 10, 0, 0));
        var bcSvc = new BaoCaoService(h.Db, [new ExcelReportExporter(), new PdfReportExporter()], clock);
        var novFilter = new ReportFilterReq { TuNgay = new DateTime(2026, 11, 1), DenNgay = new DateTime(2026, 11, 30) };

        // Chặn TuNgay > DenNgay
        Assert.False((await bcSvc.GetBc1TongHopAsync(new ReportFilterReq
        {
            TuNgay = new DateTime(2026, 11, 30), DenNgay = new DateTime(2026, 11, 1)
        })).Success);

        var bc1 = (await bcSvc.GetBc1TongHopAsync(novFilter)).Data!;
        var bc3 = (await bcSvc.GetBc3TienDoPmAsync(novFilter)).Data!;

        int bc3ClosedCount = bc3.Count(x => x.NhomKetQua is "HoanThanhDungHan" or "HoanThanhTreHan");
        Assert.Equal(bc1.SoKeHoachPmDungHan + bc1.SoKeHoachPmTreHan, bc3ClosedCount);
        Assert.Contains(bc3, x => x.KeHoachId == "KH-INV-01" && x.NhomKetQua == "HoanThanhDungHan" && x.NgayDenHanTiepTheo == new DateOnly(2026, 11, 20));
        Assert.Contains(bc3, x => x.KeHoachId == "KH-INV-01" && x.NhomKetQua == "ChuaThucHien" && x.NgayDenHanTiepTheo == new DateOnly(2026, 11, 30));
        Assert.Contains(bc3, x => x.KeHoachId == "KH-OCT-OVERDUE" && x.NhomKetQua == "DangQuaHan");

        // Dự báo lịch cho kế hoạch đang quá hạn: không sinh ngày "DuKienTuongLai" trong quá khứ (< today)
        var overdueForecast = await khSvc.GetLichDuKienAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31), "TB-CNC-02");
        Assert.True(overdueForecast.Success);
        Assert.Contains(overdueForecast.Data!, x => x.KeHoachId == "KH-OCT-OVERDUE" && x.TrangThaiDuKien == "QuaHan" && x.NgayDuKien == new DateOnly(2026, 10, 15));
        Assert.DoesNotContain(overdueForecast.Data!, x => x.KeHoachId == "KH-OCT-OVERDUE" && x.TrangThaiDuKien == "DuKienTuongLai" && x.NgayDuKien < new DateOnly(2026, 11, 20));

        // Xuất BC7 khi chỉ có phiếu nhập bổ sung -> vẫn ra dữ liệu biến động không bị rỗng
        Assert.True((await kho.XacNhanCapNhatTonKhoAsync(
            h.Db.PHIEU_CAP_NHAT_TON_KHO.First(p => p.MaPhieuKhoNgoai == "NK-UNIQUE-01").PhieuCapNhatId,
            true, "NV001", 2)).Success);
        var bc7Xlsx = await bcSvc.ExportAsync("BC7", "excel", novFilter);
        Assert.True(bc7Xlsx.Ok);
        Assert.Equal("PK", Encoding.ASCII.GetString(bc7Xlsx.Bytes, 0, 2));

        // Chuyển TB-CNC-01 sang NgungSuDung -> tự động tắt kế hoạch KH-INV-01 sang NgungApDung
        Assert.True((await tbSvc.UpdateTrangThaiAsync("TB-CNC-01", CmmsConstants.TrangThaiThietBi.NgungSuDung)).Success);
        Assert.Equal(CmmsConstants.TrangThaiKeHoach.NgungApDung, h.Db.KE_HOACH_BAO_TRI.Find("KH-INV-01")!.TrangThaiKeHoach);
    }

    private sealed class DummyEnv(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "CMMS";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
    }
}
