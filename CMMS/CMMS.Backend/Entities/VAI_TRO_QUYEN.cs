namespace CMMS.Backend.Entities;

public class VAI_TRO_QUYEN
{
    public int VaiTroId { get; set; }
    public string MaQuyen { get; set; } = string.Empty;

    public VAI_TRO VaiTro { get; set; } = null!;
}
