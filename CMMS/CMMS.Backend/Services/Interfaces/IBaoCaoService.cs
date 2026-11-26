using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IBaoCaoService
{
    Task<ApiResponse<Bc1TongHopDto>> GetBc1TongHopAsync(ReportFilterReq filter);
    Task<ApiResponse<List<ThietBiListDto>>> GetBc2DanhSachThietBiAsync(ReportFilterReq filter);
    Task<ApiResponse<List<Bc3TienDoPmItemDto>>> GetBc3TienDoPmAsync(ReportFilterReq filter);
    Task<ApiResponse<List<Bc4SuCoMaLoiDto>>> GetBc4SuCoTheoMaLoiAsync(ReportFilterReq filter);
    Task<ApiResponse<Bc5KhoiLuongKtvDto>> GetBc5KhoiLuongKtvAsync(ReportFilterReq filter);
    Task<ApiResponse<List<PhuTungDto>>> GetBc6TonKhoToiThieuAsync(ReportFilterReq filter);
    Task<ApiResponse<Bc7XuatTonDoiChieuDto>> GetBc7XuatTonDoiChieuAsync(ReportFilterReq filter);
    Task<(bool Ok, byte[] Bytes, string ContentType, string FileName, string Error)> ExportAsync(string reportCode, string format, ReportFilterReq filter);
}
