using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IDanhMucService
{
    Task<ApiResponse<List<DonViDto>>> GetDonViAsync(string? loaiDonVi, string? keyword);
    Task<ApiResponse<List<DonViDto>>> GetDonViTreeAsync();
    Task<ApiResponse<DonViDto>> CreateDonViAsync(DonViUpsertReq req);
    Task<ApiResponse<DonViDto>> UpdateDonViAsync(string donViId, DonViUpsertReq req);
    Task<ApiResponse<bool>> DeleteDonViAsync(string donViId);

    Task<ApiResponse<List<NhomThietBiDto>>> GetNhomThietBiAsync(string? keyword);
    Task<ApiResponse<NhomThietBiDto>> CreateNhomThietBiAsync(NhomThietBiDto req);
    Task<ApiResponse<NhomThietBiDto>> UpdateNhomThietBiAsync(string id, NhomThietBiDto req);
    Task<ApiResponse<bool>> DeleteNhomThietBiAsync(string id);

    Task<ApiResponse<List<MaLoiDto>>> GetMaLoiAsync(string? keyword);
    Task<ApiResponse<MaLoiDto>> CreateMaLoiAsync(MaLoiDto req);
    Task<ApiResponse<MaLoiDto>> UpdateMaLoiAsync(string id, MaLoiDto req);
    Task<ApiResponse<bool>> DeleteMaLoiAsync(string id);
}
