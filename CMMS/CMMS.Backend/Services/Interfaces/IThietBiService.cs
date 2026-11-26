using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IThietBiService
{
    Task<ApiResponse<List<ThietBiListDto>>> SearchAsync(string? keyword, string? viTriId, string? boPhanId, string? nhomId, string? trangThai);
    Task<ApiResponse<List<ThietBiListDto>>> GetTreeAsync(string? keyword, string? viTriId, string? boPhanId, string? nhomId, string? trangThai);
    Task<ApiResponse<PhieuLyLichThietBiDto>> GetPhieuLyLichAsync(string thietBiId);
    Task<ApiResponse<ThietBiListDto>> CreateAsync(ThietBiUpsertReq req);
    Task<ApiResponse<ThietBiListDto>> UpdateAsync(string thietBiId, ThietBiUpsertReq req);
    Task<ApiResponse<bool>> DeleteAsync(string thietBiId);
    Task<ApiResponse<bool>> UpdateTrangThaiAsync(string thietBiId, string trangThaiMoi);
    Task<ApiResponse<List<LichSuBaoTriThietBiDto>>> GetLichSuBaoTriAsync(string thietBiId);

    Task<ApiResponse<List<ThongSoThietBiDto>>> GetThongSoAsync(string thietBiId);
    Task<ApiResponse<ThongSoThietBiDto>> AddThongSoAsync(string thietBiId, ThongSoUpsertReq req);
    Task<ApiResponse<ThongSoThietBiDto>> UpdateThongSoAsync(long thongSoId, ThongSoUpsertReq req);
    Task<ApiResponse<bool>> DeleteThongSoAsync(long thongSoId);

    Task<ApiResponse<string>> UploadAnhAsync(string thietBiId, IFormFile file);
    Task<ApiResponse<string>> UploadTaiLieuPdfAsync(string thietBiId, IFormFile file);
    Task<(bool Found, byte[] Bytes, string ContentType, string FileName)> DownloadTaiLieuAsync(string thietBiId, bool isImage);
    Task<(bool Ok, byte[] Bytes, string ContentType, string FileName, string Error)> ExportPhieuLyLichAsync(string thietBiId, string format);
}
