namespace CMMS.Backend.Helpers;

public class InventorySettings
{
    public int ReplenishmentStartHour { get; set; } = 7;
    public int ReplenishmentEndHour { get; set; } = 10;
    public bool EnforceReplenishmentWindow { get; set; } = true;
}
