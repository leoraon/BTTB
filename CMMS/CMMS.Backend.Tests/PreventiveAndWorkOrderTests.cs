using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Services.Implementations;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace CMMS.Backend.Tests;

public class PreventiveAndWorkOrderTests
{
    [Fact]
    public async Task ChecklistLock_PmSyncIdempotency_RescheduleOverdue_Ca3NightShift_AndDuplicateKtv()
    {
        using var h = new TestDbContextHandle();
        var audit = new AuditLogHelper(h.Db);
        var ckSvc = new ChecklistService(h.Db);
        var khSvc = new KeHoachService(h.Db, audit);
        var lenhSvc = new LenhBaoTriService(h.Db, audit);

        // Thêm KTV ca đêm Ca 3 để kiểm tra chuẩn logic qua đêm
        h.Db.NHAN_VIEN.Add(new NHAN_VIEN
        {
            NhanVienId = "NV098", HoTen = "Trần Ca Đêm", SoDienThoai = "0977777777",
            Email = "cadem@cmms.vn", DonViId = "BP-BAOTRI", ChucVu = "KTV",
            CaLamViec = CmmsConstants.CaLamViec.Ca3, TrangThaiSanSang = CmmsConstants.TrangThaiSanSang.SanSang,
            TrangThai = CmmsConstants.TrangThaiNhanVien.DangLamViec
        });
        h.Db.CHUYEN_MON_KTV.Add(new CHUYEN_MON_KTV { NhanVienId = "NV098", NhomThietBiId = "NHOM-TIEN-CNC" });
        await h.Db.SaveChangesAsync();

        // 1. Tạo kế hoạch PM đến hạn hôm nay dùng CK-CNC-01 -> CK-CNC-01 bị khóa chỉnh sửa
        var khRes = await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-TEST-01", TenKeHoach = "Bảo dưỡng tháng CNC", ThietBiId = "TB-CNC-01",
            MauChecklistId = "CK-CNC-01", DonViChuKy = "Thang", GiaTriChuKy = 1, SoNgaySinhLenhTruoc = 3,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            TrangThaiKeHoach = "DangApDung"
        });
        Assert.True(khRes.Success);

        var editLocked = await ckSvc.UpdateAsync("CK-CNC-01", new MauChecklistUpsertReq
        {
            MauChecklistId = "CK-CNC-01", TenMau = "Sửa lén", NhomThietBiId = "NHOM-TIEN-CNC",
            HangMuc = [new HangMucChecklistReq { NhomBoPhan = "A", NoiDungCongViec = "B", KieuKetQua = "Dat_KhongDat", TieuChuanYeuCau = "C" }]
        });
        Assert.False(editLocked.Success);

        // 2. Đồng bộ tự động sinh lệnh 2 lần liên tiếp -> chỉ sinh đúng 1 lệnh mở & ghi audit log PhatLenhBaoTri
        var sync1 = await khSvc.SyncWorkOrdersAsync(actorTaiKhoanId: 3);
        Assert.Equal(1, sync1.Data!.SoLenhMoiSinh);
        Assert.Contains(h.Db.NHAT_KY_HE_THONG, l => l.LoaiThaoTac == CmmsConstants.LoaiThaoTacLog.PhatLenhBaoTri);

        var sync2 = await khSvc.SyncWorkOrdersAsync(actorTaiKhoanId: 3);
        Assert.Equal(0, sync2.Data!.SoLenhMoiSinh);
        var woId = sync1.Data.DanhSachMaLenhMoi[0];

        // 3. Dời lịch sang ngày mai -> hết quá hạn theo mốc mới nhất
        var tomorrow = DateTime.Today.AddDays(1);
        Assert.True((await lenhSvc.DoiLichAsync(woId, new DoiLichLenhReq
        {
            ThoiGianDuKienBatDau = tomorrow.AddHours(8),
            ThoiGianDuKienKetThuc = tomorrow.AddHours(10)
        })).Success);
        Assert.Empty((await khSvc.GetAllAsync(null, null, chiQuaHan: true)).Data!);

        // 4. Ràng buộc Ca 3 qua đêm: 04:00-10:00 bị CHẶN, 22:30-05:30 hôm sau ĐƯỢC PHÉP; chặn gán trùng KTV trên cùng lệnh
        var badCa3 = await lenhSvc.PhanCongKtvAsync(woId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV098", KhungGioBatDau = tomorrow.AddHours(4), KhungGioKetThuc = tomorrow.AddHours(10)
        }, "NV002");
        Assert.False(badCa3.Success);

        var okCa3 = await lenhSvc.PhanCongKtvAsync(woId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV098", KhungGioBatDau = tomorrow.AddHours(22.5), KhungGioKetThuc = tomorrow.AddDays(1).AddHours(5.5)
        }, "NV002");
        Assert.True(okCa3.Success);

        var dupSameWo = await lenhSvc.PhanCongKtvAsync(woId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV098", KhungGioBatDau = tomorrow.AddHours(23), KhungGioKetThuc = tomorrow.AddDays(1).AddHours(4)
        }, "NV002");
        Assert.False(dupSameWo.Success);

        // 5. Ràng buộc KTV <= 3 nhóm máy
        var tooManyGroups = await lenhSvc.CapNhatChuyenMonKtvAsync("NV003", new CapNhatChuyenMonKtvReq
        {
            DanhSachNhom =
            [
                new() { NhomThietBiId = "NHOM-TIEN-CNC" },
                new() { NhomThietBiId = "NHOM-PHAY-CNC" },
                new() { NhomThietBiId = "NHOM-MAY-EP" },
                new() { NhomThietBiId = "NHOM-NEN-KHI" }
            ]
        });
        Assert.False(tooManyGroups.Success);
    }

    [Fact]
    public async Task FullWorkOrderLifecycle_StrictAcceptance_KtvReadiness_CancelWo_AndCycleRollover()
    {
        using var h = new TestDbContextHandle();
        var audit = new AuditLogHelper(h.Db);
        var khSvc = new KeHoachService(h.Db, audit);
        var lenhSvc = new LenhBaoTriService(h.Db, audit);
        var khoSvc = new KhoService(h.Db, audit);

        // Tạo thêm 1 mẫu checklist khác để test chặn gửi nhầm mẫu checklist
        h.Db.MAU_CHECKLIST.Add(new MAU_CHECKLIST
        {
            MauChecklistId = "CK-OTHER", TenMau = "Mẫu khác", NhomThietBiId = "NHOM-PHAY-CNC", TrangThaiHieuLuc = "DangApDung",
            HangMucs = [new HANG_MUC_CHECKLIST { NhomBoPhan = "X", NoiDungCongViec = "Y", KieuKetQua = "Dat_KhongDat", TieuChuanYeuCau = "OK", ThuTuThucHien = 1 }]
        });
        await h.Db.SaveChangesAsync();

        // Tạo kế hoạch & sinh lệnh PM
        await khSvc.CreateAsync(new KeHoachUpsertReq
        {
            KeHoachId = "KH-CYCLE-01", TenKeHoach = "Bảo dưỡng định kỳ 30 ngày", ThietBiId = "TB-CNC-01",
            MauChecklistId = "CK-CNC-01", DonViChuKy = "Ngay", GiaTriChuKy = 30, SoNgaySinhLenhTruoc = 3,
            ThoiGianDuKienPhut = 60, NgayBatDauApDung = DateOnly.FromDateTime(DateTime.Today)
        });
        var woId = (await khSvc.SyncWorkOrdersAsync(3)).Data!.DanhSachMaLenhMoi[0];

        // Phân công KTV NV003 đúng Ca 1 (08:00 - 10:00)
        var baseDate = DateTime.Today.AddDays(1);
        var pc1 = await lenhSvc.PhanCongKtvAsync(woId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = baseDate.AddHours(8), KhungGioKetThuc = baseDate.AddHours(10)
        }, "NV002");
        Assert.True(pc1.Success);

        // Tạo lệnh sự cố ưu tiên Khẩn cấp (1) -> kiểm tra chặn chồng chéo & cho phép chen ngang -> kiểm tra hủy lệnh trả dự trù!
        var ycSvc = new YeuCauService(h.Db, new FileStorageHelper(new DummyEnv(Path.GetTempPath())));
        var yc = await ycSvc.TaoYeuCauAsync(
            new TaoSuaYeuCauReq { ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Hỏng khẩn", MayDangDung = true, MucUuTienDeXuatId = 1 }, "NV013");
        await ycSvc.DuyetHoacTuChoiAsync(
            yc.Data!.YeuCauId, new DuyetYeuCauReq { LaPheDuyet = true, MaLoiId = "LOI-QUA-NHIET", MucUuTienChinhThucId = 1, HuongXuLy = CmmsConstants.HuongXuLy.SuaChuaKhanCap }, "NV002");
        var woUrgent = (await lenhSvc.TaoLenhTuYeuCauAsync(new TaoLenhTuYeuCauReq
        {
            YeuCauId = yc.Data.YeuCauId, NoiDungCongViec = "Xử lý khẩn cấp",
            ThoiGianDuKienBatDau = baseDate.AddHours(8), ThoiGianDuKienKetThuc = baseDate.AddHours(9)
        }, "NV002", actorTaiKhoanId: 3)).Data!;

        Assert.False((await lenhSvc.PhanCongKtvAsync(woUrgent.LenhBaoTriId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = baseDate.AddHours(8), KhungGioKetThuc = baseDate.AddHours(9),
            ChoPhepChenNgang = false
        }, "NV002")).Success);

        Assert.True((await lenhSvc.PhanCongKtvAsync(woUrgent.LenhBaoTriId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = baseDate.AddHours(8), KhungGioKetThuc = baseDate.AddHours(9),
            ChoPhepChenNgang = true, LyDoTamDung = "Nhường ưu tiên sự cố khẩn cấp"
        }, "NV002")).Success);

        // Hủy lệnh woUrgent kèm 1 phiếu dự trù dầu VG68 -> tự hủy phiếu dự trù, giải phóng giữ chỗ, đưa YC về DaPheDuyet
        var phieuDau = await khoSvc.TaoPhieuDuTruAsync(new TaoPhieuDuTruReq
        {
            LenhBaoTriId = woUrgent.LenhBaoTriId, ChiTiet = [new() { PhuTungId = "PT-DAU-VG68", SoLuongDuTru = 5 }]
        }, "NV002");
        Assert.Equal(5, h.Db.PHU_TUNG.Find("PT-DAU-VG68")!.SoLuongDangDuTru);
        Assert.True((await lenhSvc.HuyLenhAsync(woUrgent.LenhBaoTriId, new HuyLenhReq { LyDoHuy = "Báo nhầm thiết bị" }, 3)).Success);
        Assert.Equal(0, h.Db.PHU_TUNG.Find("PT-DAU-VG68")!.SoLuongDangDuTru);
        Assert.Equal(CmmsConstants.TrangThaiYeuCau.DaPheDuyet, h.Db.YEU_CAU_BAO_TRI.Find(yc.Data.YeuCauId)!.TrangThaiYeuCau);

        // Tiếp tục vòng đời lệnh PM woId: Dự trù 2 vòng bi
        var phieu = await khoSvc.TaoPhieuDuTruAsync(new TaoPhieuDuTruReq
        {
            LenhBaoTriId = woId, ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongDuTru = 2 }]
        }, "NV002");
        Assert.True(phieu.Success);

        // Phân công lại NV003 vào woId (vì lúc nãy đã bị chen ngang -> HuyDieuChuyen) và kiểm tra KTV Readiness
        Assert.True((await lenhSvc.PhanCongKtvAsync(woId, new PhanCongKtvReq
        {
            KyThuatVienId = "NV003", KhungGioBatDau = baseDate.AddHours(10), KhungGioKetThuc = baseDate.AddHours(12)
        }, "NV002")).Success);

        Assert.True((await lenhSvc.BatDauLenhAsync(woId, "NV003")).Success);
        Assert.Equal(CmmsConstants.TrangThaiSanSang.DangBan, h.Db.NHAN_VIEN.Find("NV003")!.TrangThaiSanSang);

        Assert.True((await lenhSvc.TamDungLenhAsync(woId, new TamDungLenhReq { LyDoTamDung = "Chờ mở nắp máy" })).Success);
        Assert.Equal(CmmsConstants.TrangThaiSanSang.SanSang, h.Db.NHAN_VIEN.Find("NV003")!.TrangThaiSanSang);

        Assert.True((await lenhSvc.TiepTucLenhAsync(woId)).Success);
        Assert.Equal(CmmsConstants.TrangThaiSanSang.DangBan, h.Db.NHAN_VIEN.Find("NV003")!.TrangThaiSanSang);

        // Chặn gửi nhầm hạng mục của mẫu CK-OTHER
        var wrongHmId = h.Db.HANG_MUC_CHECKLIST.First(x => x.MauChecklistId == "CK-OTHER").HangMucId;
        Assert.False((await lenhSvc.LuuKetQuaChecklistAsync(woId, [new() { HangMucId = wrongHmId, KetLuan = "Dat" }], "NV003")).Success);

        // Chỉ gửi 1/2 hạng mục -> kết thúc -> thử nghiệm thu PHẢI BỊ CHẶN vì chưa đủ tất cả hạng mục checklist!
        var hmIds = h.Db.HANG_MUC_CHECKLIST.Where(x => x.MauChecklistId == "CK-CNC-01").OrderBy(x => x.ThuTuThucHien).Select(x => x.HangMucId).ToList();
        Assert.True((await lenhSvc.LuuKetQuaChecklistAsync(woId, [new() { HangMucId = hmIds[0], GiaTriDoThucTe = 95 }], "NV003")).Success);
        Assert.True((await lenhSvc.KetThucLenhAsync(woId, new KetThucLenhReq())).Success);
        Assert.Equal(CmmsConstants.TrangThaiSanSang.SanSang, h.Db.NHAN_VIEN.Find("NV003")!.TrangThaiSanSang);

        var partialAcceptance = await lenhSvc.NghiemThuLenhAsync(woId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        });
        Assert.False(partialAcceptance.Success);

        // Lưu đủ cả 2 hạng mục checklist (hạng mục 0 vượt ngưỡng Max=80 -> KhongDat + warning) -> Nghiệm thu OK!
        var ckRes = await lenhSvc.LuuKetQuaChecklistAsync(woId,
        [
            new() { HangMucId = hmIds[0], GiaTriDoThucTe = 95 },
            new() { HangMucId = hmIds[1], KetLuan = "Dat" }
        ], "NV003");
        Assert.True(ckRes.Success);
        Assert.Single(ckRes.Warnings);
        Assert.Equal("KhongDat", ckRes.Data![0].KetLuan);

        Assert.True((await lenhSvc.NghiemThuLenhAsync(woId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);

        // Chưa chốt phiếu phụ tùng -> PHẢI CHẶN ĐÓNG LỆNH!
        Assert.False((await lenhSvc.DongLenhAsync(woId, "NV002", 3)).Success);

        // Hoàn tất quy trình vật tư -> đóng lệnh
        Assert.True((await khoSvc.KhaiBaoThucDungAsync(phieu.Data!.PhieuXuatId, new KhaiBaoThucDungReq
        {
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThucDung = 2 }]
        }, "NV003")).Success);
        Assert.True((await khoSvc.XacNhanXuatDungAsync(phieu.Data.PhieuXuatId, "NV002")).Success);

        Assert.True((await lenhSvc.DongLenhAsync(woId, "NV002", 3)).Success);
        Assert.Contains(h.Db.NHAT_KY_HE_THONG, l => l.LoaiThaoTac == CmmsConstants.LoaiThaoTacLog.DuyetDongLenh);

        // Vì vẫn còn yêu cầu sự cố chưa hoàn thành (yc đã revert về DaPheDuyet ở trên), test máy quay về hoặc kiểm tra chu kỳ xoay vòng +30 ngày!
        var kh = h.Db.KE_HOACH_BAO_TRI.Find("KH-CYCLE-01")!;
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today).AddDays(30), kh.NgayDenHanTiepTheo);
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
