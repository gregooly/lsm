namespace LaundryMS.Web.Models.Entities;

public class SystemSetting
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public DateTime UpdatedAt { get; set; }
}

