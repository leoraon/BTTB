using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;
using CMMS.Shared.Enums;

namespace CMMS.Backend.Services.Interfaces;

public interface IPhanQuyenService
{
    Task<ApiResponse<List<VaiTroDto>>> GetAllVaiTrosAsync();
    Task<ApiResponse<VaiTroDto>> CreateVaiTroAsync(VaiTroUpsertReq req);
    Task<ApiResponse<VaiTroDto>> UpdateVaiTroAsync(int vaiTroId, VaiTroUpsertReq req);
    IReadOnlyList<MaQuyen.QuyenInfo> GetDanhMucMaQuyen();
    Task<ApiResponse<bool>> CapNhatQuyenVaiTroAsync(int vaiTroId, CapNhatQuyenVaiTroReq req, int actorTaiKhoanId);
    Task<ApiResponse<bool>> GanVaiTroTaiKhoanAsync(int taiKhoanId, int vaiTroId, int actorTaiKhoanId);
}
