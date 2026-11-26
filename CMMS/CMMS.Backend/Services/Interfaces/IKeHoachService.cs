using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IKeHoachService
{
    Task<ApiResponse<List<KeHoachBaoTriDto>>> GetAllAsync(string? thietBiId, string? trangThai, bool chiQuaHan);
    Task<ApiResponse<KeHoachBaoTriDto>> CreateAsync(KeHoachUpsertReq req);
    Task<ApiResponse<KeHoachBaoTriDto>> UpdateAsync(string keHoachId, KeHoachUpsertReq req);
    Task<ApiResponse<bool>> UpdateTrangThaiAsync(string keHoachId, string trangThaiMoi);
    Task<ApiResponse<List<LichSuBaoTriThietBiDto>>> GetLichSuKyAsync(string keHoachId);
    Task<ApiResponse<List<LichBaoTriDuKienItemDto>>> GetLichDuKienAsync(DateOnly tuNgay, DateOnly denNgay, string? thietBiId = null, string? nhomThietBiId = null);
    Task<ApiResponse<SyncWorkOrdersResultDto>> SyncWorkOrdersAsync(int? actorTaiKhoanId = null);
}
