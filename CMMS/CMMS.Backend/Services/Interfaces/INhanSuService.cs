using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface INhanSuService
{
    Task<ApiResponse<List<NhanVienDto>>> GetNhanViensAsync(string? keyword, string? donViId, string? trangThai);
    Task<ApiResponse<List<NhanVienDto>>> GetKyThuatViensAsync();
    Task<ApiResponse<NhanVienDto>> CreateNhanVienAsync(NhanVienUpsertReq req);
    Task<ApiResponse<NhanVienDto>> UpdateNhanVienAsync(string nhanVienId, NhanVienUpsertReq req, int actorTaiKhoanId);
    Task<ApiResponse<bool>> NgungCongTacNhanVienAsync(string nhanVienId, int actorTaiKhoanId);

    Task<ApiResponse<List<TaiKhoanDto>>> GetTaiKhoansAsync(string? trangThai);
    Task<ApiResponse<TaiKhoanDto>> DeXuatTaiKhoanAsync(DeXuatTaiKhoanReq req, int actorTaiKhoanId);
    Task<ApiResponse<bool>> PheDuyetTaiKhoanAsync(int taiKhoanId, PheDuyetTaiKhoanReq req, string nguoiDuyetId, int actorTaiKhoanId);
    Task<ApiResponse<bool>> KhoaMoTaiKhoanAsync(int taiKhoanId, KhoaMoTaiKhoanReq req, int actorTaiKhoanId);
}
