using CMMS.Shared.DTOs;
using CMMS.Shared.DTOs.Common;

namespace CMMS.Backend.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req);
    Task<ApiResponse<bool>> LogoutAsync(int taiKhoanId, string tenDangNhap);
    Task<ApiResponse<bool>> ChangePasswordAsync(int taiKhoanId, ChangePasswordRequest req);
    Task<ApiResponse<bool>> UpdateProfileAsync(string nhanVienId, UpdateProfileRequest req);
    Task<ApiResponse<CurrentUserDto>> GetCurrentUserAsync(int taiKhoanId);
}
