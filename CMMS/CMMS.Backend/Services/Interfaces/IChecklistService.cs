using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IChecklistService
{
    Task<ApiResponse<List<MauChecklistDto>>> GetAllAsync(string? nhomThietBiId, string? trangThaiHieuLuc);
    Task<ApiResponse<MauChecklistDto>> GetByIdAsync(string mauChecklistId);
    Task<ApiResponse<MauChecklistDto>> CreateAsync(MauChecklistUpsertReq req);
    Task<ApiResponse<MauChecklistDto>> UpdateAsync(string mauChecklistId, MauChecklistUpsertReq req);
    Task<ApiResponse<bool>> NgungApDungAsync(string mauChecklistId);
    Task<ApiResponse<bool>> DeleteAsync(string mauChecklistId);
}
