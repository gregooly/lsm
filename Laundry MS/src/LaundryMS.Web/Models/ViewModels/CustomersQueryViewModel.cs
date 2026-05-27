namespace LaundryMS.Web.Models.ViewModels;

public class CustomersQueryViewModel
{
    public string? Q { get; init; }
    /// <summary>fixed, rental, other, or null for all.</summary>
    public string? CustomerType { get; init; }
    /// <summary>When true, inactive customers are included.</summary>
    public bool IncludeInactive { get; init; }
    /// <summary>name, type, status</summary>
    public string SortBy { get; init; } = "name";
    public string SortDir { get; init; } = "asc";
}
