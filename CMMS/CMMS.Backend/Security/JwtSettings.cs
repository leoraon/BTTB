namespace CMMS.Backend.Security;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CMMS.Backend";
    public string Audience { get; set; } = "CMMS.WinForms";
    public int ExpireMinutes { get; set; } = 480;
}
