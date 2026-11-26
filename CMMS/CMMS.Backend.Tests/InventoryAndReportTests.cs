using System.Text;
using CMMS.Backend.Helpers;
using CMMS.Backend.Helpers.Exporters;
using CMMS.Backend.Services.Implementations;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.Extensions.Options;
using Xunit;

namespace CMMS.Backend.Tests;

public class InventoryAndReportTests
{
    [Fact]
    public async Task StockUpdateCrossControl_MorningWindow_FourStepVoucher_AndLowStockWarning()
    {
        using var h = new TestDbContextHandle();
        var audit = new AuditLogHelper(h.Db);
        var invOpts = Options.Create(new InventorySettings { ReplenishmentStartHour = 7, ReplenishmentEndHour = 10, EnforceReplenishmentWindow = true });

        // 0. Kiểm tra khung giờ bổ sung kho ngoài: 14:00 chiều BỊ CHẶN, trong khi kiểm kê vẫn được phép
        var afternoonKho = new KhoService(h.Db, audit, new FixedTimeProvider(DateTime.Today.AddHours(14)), invOpts);
        var blockedAfternoon = await afternoonKho.TaoPhieuCapNhatTonKhoAsync(new TaoPhieuCapNhatTonKhoReq
        {
            LoaiCapNhat = CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai,
            MaPhieuKhoNgoai = "NK-CHIEU-001",
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThayDoi = 2 }]
        }, "NV002");
        Assert.False(blockedAfternoon.Success);

        var okKiemKe = await afternoonKho.TaoPhieuCapNhatTonKhoAsync(new TaoPhieuCapNhatTonKhoReq
        {
            LoaiCapNhat = CmmsConstants.LoaiCapNhatKho.DieuChinhKiemKe,
            GhiChu = "Kiểm kê chiều đối chiếu kệ",
            ChiTiet = [new() { PhuTungId = "PT-DAU-VG68", SoLuongThayDoi = 1 }]
        }, "NV002");
        Assert.True(okKiemKe.Success);

        // 1. Khung giờ chuẩn 08:30 sáng -> Trưởng BP lập phiếu bổ sung kho ngoài (+5 vòng bi) -> tồn chưa tăng
        var kho = new KhoService(h.Db, audit, new FixedTimeProvider(DateTime.Today.AddHours(8).AddMinutes(30)), invOpts);
        var lenhSvc = new LenhBaoTriService(h.Db, audit);

        var propose = await kho.TaoPhieuCapNhatTonKhoAsync(new TaoPhieuCapNhatTonKhoReq
        {
            LoaiCapNhat = CmmsConstants.LoaiCapNhatKho.BoSungTuKhoNgoai,
            MaPhieuKhoNgoai = "NK-EXT-001",
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThayDoi = 5 }]
        }, "NV002");
        Assert.True(propose.Success);
        Assert.Equal(10, h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongTonHienCo);

        // Kiểm soát chéo: Chính người lập phiếu (NV002) tự xác nhận -> PHẢI BỊ CHẶN!
        var selfConfirm = await kho.XacNhanCapNhatTonKhoAsync(propose.Data!.PhieuCapNhatId, true, "NV002", 3);
        Assert.False(selfConfirm.Success);

        // Quản lý nhà máy (NV001) xác nhận -> tồn tăng lên 15 & ghi Nhật ký hệ thống
        var confirmAdd = await kho.XacNhanCapNhatTonKhoAsync(propose.Data.PhieuCapNhatId, true, "NV001", 2);
        Assert.True(confirmAdd.Success);
        Assert.Equal(15, h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongTonHienCo);
        Assert.Contains(h.Db.NHAT_KY_HE_THONG, log => log.LoaiThaoTac == CmmsConstants.LoaiThaoTacLog.XacNhanBoSungTonKho);

        // 2. Quy trình 4 bước xuất dùng trên lệnh sự cố: Dự trù 2 -> Thực dùng 9 -> Từ chối vượt -> Khai báo lại 9 -> Duyệt vượt -> Xuất trừ tồn
        var yc = new YeuCauService(h.Db, null!);
        var ycRes = await yc.TaoYeuCauAsync(new TaoSuaYeuCauReq { ThietBiId = "TB-CNC-01", MoTaTrieuChung = "Hư ổ bi", MayDangDung = false, MucUuTienDeXuatId = 2 }, "NV013");
        await yc.DuyetHoacTuChoiAsync(ycRes.Data!.YeuCauId, new DuyetYeuCauReq { LaPheDuyet = true, MaLoiId = "LOI-QUA-NHIET", MucUuTienChinhThucId = 2, HuongXuLy = CmmsConstants.HuongXuLy.SuaChuaTheoKeHoach }, "NV002");
        var wo = (await lenhSvc.TaoLenhTuYeuCauAsync(new TaoLenhTuYeuCauReq { YeuCauId = ycRes.Data.YeuCauId, NoiDungCongViec = "Thay bi", ThoiGianDuKienBatDau = DateTime.Now, ThoiGianDuKienKetThuc = DateTime.Now.AddHours(2) }, "NV002", 3)).Data!;

        var phieu = await kho.TaoPhieuDuTruAsync(new TaoPhieuDuTruReq
        {
            LenhBaoTriId = wo.LenhBaoTriId,
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongDuTru = 2 }]
        }, "NV002");
        Assert.True(phieu.Success);
        Assert.Equal(2, h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongDangDuTru);

        // Khai báo vượt dự trù thiếu giải trình -> bị chặn
        Assert.False((await kho.KhaiBaoThucDungAsync(phieu.Data!.PhieuXuatId, new KhaiBaoThucDungReq
        {
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThucDung = 9 }]
        }, "NV003")).Success);

        // Khai báo vượt có giải trình -> ChoDuyetVuotDuTru
        var overWithReason = await kho.KhaiBaoThucDungAsync(phieu.Data.PhieuXuatId, new KhaiBaoThucDungReq
        {
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThucDung = 9, GhiChuGiaiTrinh = "Hư luôn bộ vòng bi sau" }]
        }, "NV003");
        Assert.True(overWithReason.Success);
        Assert.Equal(CmmsConstants.TrangThaiPhieuXuat.ChoDuyetVuotDuTru, overWithReason.Data!.TrangThaiPhieu);

        // Thử từ chối duyệt vượt -> trả về DangDuTru để KTV khai báo lại
        var rejectOver = await kho.DuyetVuotDuTruAsync(phieu.Data.PhieuXuatId, new DuyetVuotDuTruReq { LaPheDuyet = false, GhiChu = "Kiểm tra lại số lượng tháo cũ" }, "NV002");
        Assert.True(rejectOver.Success);
        Assert.Equal(CmmsConstants.TrangThaiPhieuXuat.DangDuTru, rejectOver.Data!.TrangThaiPhieu);

        // KTV khai báo lại -> Trưởng BP duyệt vượt -> Xác nhận xuất trừ tồn (15 - 9 = 6 <= Min 7 -> cảnh báo tồn thấp!)
        Assert.True((await kho.KhaiBaoThucDungAsync(phieu.Data.PhieuXuatId, new KhaiBaoThucDungReq
        {
            ChiTiet = [new() { PhuTungId = "PT-VB-6205", SoLuongThucDung = 9, GhiChuGiaiTrinh = "Đã kiểm đếm xác nhận hỏng cả cụm sau" }]
        }, "NV003")).Success);
        Assert.True((await kho.DuyetVuotDuTruAsync(phieu.Data.PhieuXuatId, new DuyetVuotDuTruReq { LaPheDuyet = true }, "NV002")).Success);

        var issueOk = await kho.XacNhanXuatDungAsync(phieu.Data.PhieuXuatId, "NV002");
        Assert.True(issueOk.Success);
        Assert.Single(issueOk.Warnings);
        Assert.Equal(6, h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongTonHienCo);
        Assert.Equal(0, h.Db.PHU_TUNG.Find("PT-VB-6205")!.SoLuongDangDuTru);

        // Kiểm tra nghiệm thu lệnh sự cố bị chặn khi chưa lập Báo cáo sự cố, sau khi lập Báo cáo sự cố -> cho nghiệm thu & đóng lệnh
        Assert.True((await lenhSvc.BatDauLenhAsync(wo.LenhBaoTriId, "NV003")).Success);
        Assert.True((await lenhSvc.KetThucLenhAsync(wo.LenhBaoTriId, new KetThucLenhReq())).Success);
        Assert.False((await lenhSvc.NghiemThuLenhAsync(wo.LenhBaoTriId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);

        Assert.True((await lenhSvc.LuuBaoCaoSuCoAsync(wo.LenhBaoTriId, new BaoCaoSuCoUpsertReq
        {
            MaLoiChinhThucId = "LOI-QUA-NHIET", CoDungMay = true,
            ThoiDiemXayRa = DateTime.Now.AddHours(-2), ThoiDiemKhoiPhuc = DateTime.Now,
            MoTaHuHong = "Vỡ vòng bi", NguyenNhanGocRe = "Khô mỡ bôi trơn",
            MucDoAnhHuongPhanTram = 100, BienPhapKhacPhuc = "Thay vòng bi mới & bơm mỡ"
        }, "NV003")).Success);

        Assert.True((await lenhSvc.NghiemThuLenhAsync(wo.LenhBaoTriId, new NghiemThuLenhReq
        {
            NguoiBanGiaoId = "NV003", NguoiKyNhanId = "NV013", TinhTrangSauBaoTri = "DatChuan"
        })).Success);
        Assert.True((await lenhSvc.DongLenhAsync(wo.LenhBaoTriId, "NV002", 3)).Success);

        // 3. Kiểm tra Dashboard + BC1..BC7 + Export Excel & PDF chuẩn quốc hiệu/chữ ký Việt Nam
        var dash = await new DashboardService(h.Db).GetSummaryAsync();
        Assert.True(dash.Success);
        Assert.Equal(1, dash.Data!.SoPhuTungDuoiToiThieu);

        var bcSvc = new BaoCaoService(h.Db, [new ExcelReportExporter(), new PdfReportExporter()]);
        var filter = new ReportFilterReq { TuNgay = DateTime.Today.AddDays(-7), DenNgay = DateTime.Today.AddDays(1) };

        var bc7 = await bcSvc.GetBc7XuatTonDoiChieuAsync(filter);
        Assert.True(bc7.Success);
        Assert.Contains(bc7.Data!.DoiChieuDuTruThucDung, r => r.PhuTungId == "PT-VB-6205" && r.ChenhLech == 7);

        foreach (var code in new[] { "BC1", "BC2", "BC3", "BC4", "BC5", "BC6", "BC7" })
        {
            var xlsx = await bcSvc.ExportAsync(code, "excel", filter);
            Assert.True(xlsx.Ok);
            Assert.Equal("PK", Encoding.ASCII.GetString(xlsx.Bytes, 0, 2));

            var pdf = await bcSvc.ExportAsync(code, "pdf", filter);
            Assert.True(pdf.Ok);
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf.Bytes, 0, 5));
        }
    }
}
