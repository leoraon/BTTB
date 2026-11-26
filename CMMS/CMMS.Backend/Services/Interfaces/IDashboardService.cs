using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync();
}
