namespace CMMS.Backend.Entities;

public class VAI_TRO
{
    public int VaiTroId { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }

    public ICollection<VAI_TRO_QUYEN> VAI_TRO_QUYEN { get; set; } = [];
    public ICollection<TAI_KHOAN> TAI_KHOAN { get; set; } = [];
}
