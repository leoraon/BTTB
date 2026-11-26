using System.Text;
using CMMS.Backend.Entities;
using CMMS.Backend.Helpers;
using CMMS.Backend.Helpers.Exporters;
using CMMS.Backend.Services.Implementations;
using CMMS.Backend.Tests.Fixtures;
using CMMS.Shared.DTOs;
using CMMS.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace CMMS.Backend.Tests;

public class EquipmentAndIncidentTests
{
    [Fact]
    public async Task DeviceTree_MaxTwoLevels_LaCumChiTiet_CycleValidation_AndExport()
    {
        using var h = new TestDbContextHandle();
        var env = new DummyEnv(Path.GetTempPath());
        var tbSvc = new ThietBiService(h.Db, new FileStorageHelper(env), [new PdfReportExporter(), new ExcelReportExporter()]);

        // Chặn chu kỳ <= 0
        var badCycle = await tbSvc.CreateAsync(new ThietBiUpsertReq
        {
            ThietBiId = "TB-BAD-CYC", TenThietBi = "Máy test", NhomThietBiId = "NHOM-TIEN-CNC",
            ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "M", Serial = "SN-CYC",
            NhaSanXuat = "NSK", NgayDuaVaoSuDung = new DateOnly(2024, 1, 1), ChuKyBaoTriKhuyenNghiNgay = 0,
            CongDungMoTa = "Test"
        });
        Assert.False(badCycle.Success);

        // Chặn máy cấp 1 gán vào nhóm cụm chi tiết con (LaCumChiTiet = true)
        var wrongRootGroup = await tbSvc.CreateAsync(new ThietBiUpsertReq
        {
            ThietBiId = "TB-WRONG-GRP", TenThietBi = "Máy gán nhầm nhóm cụm", NhomThietBiId = "CUM-SPINDLE",
            ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "M", Serial = "SN-WG1",
            NhaSanXuat = "NSK", NgayDuaVaoSuDung = new DateOnly(2024, 1, 1), ChuKyBaoTriKhuyenNghiNgay = 30,
            CongDungMoTa = "Test"
        });
        Assert.False(wrongRootGroup.Success);

        // Chặn cụm con cấp 2 gán vào nhóm máy chính (LaCumChiTiet = false)
        var wrongChildGroup = await tbSvc.CreateAsync(new ThietBiUpsertReq
        {
            ThietBiId = "CUM-WRONG-GRP", TenThietBi = "Cụm gán nhầm nhóm máy", ThietBiChaId = "TB-CNC-01",
            NhomThietBiId = "NHOM-TIEN-CNC", ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01",
            Model = "M", Serial = "SN-WG2", NhaSanXuat = "NSK", NgayDuaVaoSuDung = new DateOnly(2024, 1, 1),
            ChuKyBaoTriKhuyenNghiNgay = 30, CongDungMoTa = "Test"
        });
        Assert.False(wrongChildGroup.Success);

        // Chặn tạo cấp 3 (CUM-SP-01 đã là con của TB-CNC-01)
        var level3 = await tbSvc.CreateAsync(new ThietBiUpsertReq
        {
            ThietBiId = "CAP3-ERR", TenThietBi = "Vòng bi con của cụm", ThietBiChaId = "CUM-SP-01",
            NhomThietBiId = "CUM-SPINDLE", ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01",
            Model = "M", Serial = "SN-CAP3", NhaSanXuat = "NSK", NgayDuaVaoSuDung = new DateOnly(2024, 1, 1),
            CongDungMoTa = "Test"
        });
        Assert.False(level3.Success);

        // Chặn xóa thiết bị cha khi còn cụm con
        var delParent = await tbSvc.DeleteAsync("TB-CNC-01");
        Assert.False(delParent.Success);

        // Xuất Phiếu lý lịch thiết bị PDF & Excel
        var expPdf = await tbSvc.ExportPhieuLyLichAsync("TB-CNC-01", "pdf");
        Assert.True(expPdf.Ok);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(expPdf.Bytes, 0, 5));

        var expXlsx = await tbSvc.ExportPhieuLyLichAsync("TB-CNC-01", "excel");
        Assert.True(expXlsx.Ok);
        Assert.Equal("PK", Encoding.ASCII.GetString(expXlsx.Bytes, 0, 2));

        // Chặn tạo vòng lặp đơn vị cha - con
        var dmSvc = new DanhMucService(h.Db);
        var cycle = await dmSvc.UpdateDonViAsync("PX-COKHI", new DonViUpsertReq
        {
            DonViId = "PX-COKHI", TenDonVi = "Phân xưởng Cơ khí", DonViChaId = "DC-CNC-01", LoaiDonVi = "PhanXuong"
        });
        Assert.False(cycle.Success);
    }

    [Fact]
    public async Task IncidentRequest_AutoStopsMachine_EditDeviceRevertsOldMachine_SLA_AndPriority()
    {
        using var h = new TestDbContextHandle();
        h.Db.THIET_BI.Add(new THIET_BI
        {
            ThietBiId = "TB-CNC-02", TenThietBi = "Máy Phay CNC Mazak", NhomThietBiId = "NHOM-PHAY-CNC",
            ViTriLapDatId = "DC-CNC-01", BoPhanQuanLyId = "DC-CNC-01", Model = "VCN-530", Serial = "SN-002",
            NhaSanXuat = "Mazak", NgayDuaVaoSuDung = new DateOnly(2023, 5, 1), ChuKyBaoTriKhuyenNghiNgay = 30,
            CongDungMoTa = "Phay", TrangThaiVanHanh = "DangHoatDong"
        });
        await h.Db.SaveChangesAsync();

        var wwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CMMS.Backend", "wwwroot"));
        var ycSvc = new YeuCauService(h.Db, new FileStorageHelper(new DummyEnv(wwwroot)));

        // Người vận hành báo hỏng máy TB-CNC-01 đang dừng
        var create = await ycSvc.TaoYeuCauAsync(new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-01",
            MoTaTrieuChung = "Trục chính nóng và kêu lớn",
            DuongDanAnhSuCo = "/anh-su-co/YC-2026-CM001.jpg",
            MayDangDung = true,
            MucUuTienDeXuatId = 4
        }, "NV013");

        Assert.True(create.Success);
        Assert.Equal("DungDoSuCo", h.Db.THIET_BI.Find("TB-CNC-01")!.TrangThaiVanHanh);
        Assert.Equal(1440, (int)Math.Round((create.Data!.HanPhanHoi - create.Data.ThoiDiemGui).TotalMinutes));

        // Tải ảnh sự cố gắn vào phiếu phải trả về JPEG thật
        var imgDl = await ycSvc.DownloadAnhSuCoAsync(create.Data.YeuCauId);
        Assert.True(imgDl.Found);
        Assert.Equal(0xFF, imgDl.Bytes[0]);
        Assert.Equal(0xD8, imgDl.Bytes[1]);

        // Sửa lại chọn nhầm máy -> đổi sang TB-CNC-02 khi còn ChoTiepNhan -> trả TB-CNC-01 về DangHoatDong và dừng TB-CNC-02!
        var edited = await ycSvc.SuaYeuCauAsync(create.Data.YeuCauId, new TaoSuaYeuCauReq
        {
            ThietBiId = "TB-CNC-02",
            MoTaTrieuChung = "Thực ra máy Mazak bị rò dầu",
            DuongDanAnhSuCo = "/anh-su-co/YC-2026-CM001.jpg",
            MayDangDung = true,
            MucUuTienDeXuatId = 1
        }, "NV013", laTruongBp: false);
        Assert.True(edited.Success);
        Assert.Equal("TB-CNC-02", edited.Data!.ThietBiId);
        Assert.Equal("DangHoatDong", h.Db.THIET_BI.Find("TB-CNC-01")!.TrangThaiVanHanh);
        Assert.Equal("DungDoSuCo", h.Db.THIET_BI.Find("TB-CNC-02")!.TrangThaiVanHanh);

        // Trưởng BP đánh giá phê duyệt
        var approve = await ycSvc.DuyetHoacTuChoiAsync(create.Data.YeuCauId, new DuyetYeuCauReq
        {
            LaPheDuyet = true,
            MaLoiId = "LOI-RO-RI-DAU",
            NhanDinhNguyenNhanSoBo = "Hở phớt dầu",
            MucUuTienChinhThucId = 1,
            HuongXuLy = CmmsConstants.HuongXuLy.SuaChuaKhanCap
        }, "NV002");

        Assert.True(approve.Success);
        Assert.Equal(1, approve.Data!.MucUuTienDeXuatId);
        Assert.Equal(1, approve.Data.MucUuTienChinhThucId);
        Assert.Equal("DaPheDuyet", approve.Data.TrangThaiYeuCau);
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
