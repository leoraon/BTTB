using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface INhatKyService
{
    Task<ApiResponse<List<NhatKyDto>>> SearchAsync(NhatKyFilterReq filter);
}
