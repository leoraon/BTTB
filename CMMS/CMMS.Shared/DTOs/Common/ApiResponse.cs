namespace CMMS.Shared.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Warnings { get; set; } = [];

    public static ApiResponse<T> Ok(T? data, string message = "Thành công", List<string>? warnings = null)
        => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Warnings = warnings ?? []
        };

    public static ApiResponse<T> Fail(string message)
        => new()
        {
            Success = false,
            Message = message
        };
}
