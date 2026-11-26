using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IYeuCauService
{
    Task<ApiResponse<List<MucUuTienDto>>> GetMucUuTienAsync();
    Task<ApiResponse<MucUuTienDto>> UpdateMucUuTienAsync(int id, MucUuTienDto req);

    Task<ApiResponse<List<YeuCauBaoTriDto>>> GetDanhSachAsync(string? trangThai, string? nguoiBaoId, bool chiQuaHan);
    Task<ApiResponse<YeuCauBaoTriDto>> GetByIdAsync(string yeuCauId);
    Task<ApiResponse<YeuCauBaoTriDto>> TaoYeuCauAsync(TaoSuaYeuCauReq req, string nguoiBaoId);
    Task<ApiResponse<YeuCauBaoTriDto>> SuaYeuCauAsync(string yeuCauId, TaoSuaYeuCauReq req, string nguoiSuaId, bool laTruongBp);
    Task<ApiResponse<bool>> HuyYeuCauAsync(string yeuCauId, string nguoiHuyId, bool laTruongBp);
    Task<ApiResponse<YeuCauBaoTriDto>> DuyetHoacTuChoiAsync(string yeuCauId, DuyetYeuCauReq req, string nguoiDuyetId);
    Task<ApiResponse<string>> UploadAnhSuCoAsync(IFormFile file);
    Task<(bool Found, byte[] Bytes, string ContentType, string FileName)> DownloadAnhSuCoAsync(string yeuCauId);
}
