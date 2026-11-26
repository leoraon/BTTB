using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IKhoService
{
    Task<ApiResponse<List<PhuTungDto>>> GetPhuTungAsync(string? keyword, string? loai, bool chiDuoiToiThieu);
    Task<ApiResponse<PhuTungDto>> CreatePhuTungAsync(PhuTungUpsertReq req);
    Task<ApiResponse<PhuTungDto>> UpdatePhuTungAsync(string phuTungId, PhuTungUpsertReq req);
    Task<ApiResponse<bool>> DeletePhuTungAsync(string phuTungId);

    Task<ApiResponse<List<DinhMucLinhKienDto>>> GetDinhMucByThietBiAsync(string thietBiId, bool chiThayTheDinhKy);
    Task<ApiResponse<DinhMucLinhKienDto>> UpsertDinhMucAsync(DinhMucUpsertReq req);
    Task<ApiResponse<bool>> DeleteDinhMucAsync(long dinhMucId);

    Task<ApiResponse<List<PhieuCapNhatTonKhoDto>>> GetPhieuCapNhatTonKhoAsync(string? trangThai);
    Task<ApiResponse<PhieuCapNhatTonKhoDto>> TaoPhieuCapNhatTonKhoAsync(TaoPhieuCapNhatTonKhoReq req, string nguoiLapId);
    Task<ApiResponse<bool>> XacNhanCapNhatTonKhoAsync(string phieuId, bool laXacNhan, string nguoiXacNhanId, int actorTaiKhoanId);

    Task<ApiResponse<List<PhieuDuTruXuatDto>>> GetPhieuXuatByLenhAsync(string? lenhBaoTriId, string? trangThai);
    Task<ApiResponse<PhieuDuTruXuatDto>> TaoPhieuDuTruAsync(TaoPhieuDuTruReq req, string nguoiLapId);
    Task<ApiResponse<bool>> HuyPhieuDuTruAsync(string phieuXuatId);
    Task<ApiResponse<PhieuDuTruXuatDto>> KhaiBaoThucDungAsync(string phieuXuatId, KhaiBaoThucDungReq req, string ktvId);
    Task<ApiResponse<PhieuDuTruXuatDto>> DuyetVuotDuTruAsync(string phieuXuatId, DuyetVuotDuTruReq req, string nguoiDuyetId);
    Task<ApiResponse<PhieuDuTruXuatDto>> XacNhanXuatDungAsync(string phieuXuatId, string nguoiXacNhanId, int actorTaiKhoanId = 0);
}
