using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface ILenhBaoTriService
{
    Task<ApiResponse<List<LenhBaoTriDto>>> GetDanhSachAsync(string? loaiLenh, string? trangThai, string? thietBiId, string? ktvId, DateTime? tuNgay, DateTime? denNgay);
    Task<ApiResponse<LenhBaoTriDetailDto>> GetDetailAsync(string lenhBaoTriId);
    Task<ApiResponse<LenhBaoTriDto>> TaoLenhTuYeuCauAsync(TaoLenhTuYeuCauReq req, string nguoiTaoId, int actorTaiKhoanId = 0);
    Task<ApiResponse<LenhBaoTriDto>> DoiLichAsync(string lenhBaoTriId, DoiLichLenhReq req);

    Task<ApiResponse<List<ChuyenMonKtvDto>>> GetHoSoKtvAsync();
    Task<ApiResponse<bool>> CapNhatChuyenMonKtvAsync(string nhanVienId, CapNhatChuyenMonKtvReq req);
    Task<ApiResponse<PhanCongKtvDto>> PhanCongKtvAsync(string lenhBaoTriId, PhanCongKtvReq req, string nguoiPhanCongId);
    Task<ApiResponse<bool>> HuyPhanCongKtvAsync(long phanCongId);

    Task<ApiResponse<bool>> BatDauLenhAsync(string lenhBaoTriId, string actorNhanVienId);
    Task<ApiResponse<bool>> TamDungLenhAsync(string lenhBaoTriId, TamDungLenhReq req);
    Task<ApiResponse<bool>> TiepTucLenhAsync(string lenhBaoTriId);
    Task<ApiResponse<bool>> KetThucLenhAsync(string lenhBaoTriId, KetThucLenhReq req);

    Task<ApiResponse<List<KetQuaChecklistDto>>> LuuKetQuaChecklistAsync(string lenhBaoTriId, List<KetQuaChecklistItemReq> items, string ktvId);
    Task<ApiResponse<BaoCaoSuCoDto>> LuuBaoCaoSuCoAsync(string lenhBaoTriId, BaoCaoSuCoUpsertReq req, string ktvId);
    Task<ApiResponse<bool>> NghiemThuLenhAsync(string lenhBaoTriId, NghiemThuLenhReq req);
    Task<ApiResponse<bool>> DongLenhAsync(string lenhBaoTriId, string nguoiDuyetDongId, int actorTaiKhoanId = 0);
    Task<ApiResponse<bool>> HuyLenhAsync(string lenhBaoTriId, HuyLenhReq req, int actorTaiKhoanId = 0);
}
