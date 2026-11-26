using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IBieuMauService
{
    Task<ApiResponse<List<BieuMauDto>>> GetAllAsync();
    Task<(bool Found, byte[] Bytes, string ContentType, string FileName, string Error)> DownloadAsync(string bieuMauId);
}
