namespace LaundryMS.Web.Models.Entities;

/// <summary>Client account managed inside the app (hospital, hotel, etc.). <see cref="CustomerId"/> is the external tenant scope shared with JWT; <see cref="Id"/> is this row&apos;s local primary key.</summary>
public class Customer
{
    public ulong Id { get; set; }

    /// <summary>External tenant id — same value as on JWT and on operational tables&apos; <c>customer_id</c> for row scope.</summary>
    public ulong? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "other";
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? AddressText { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<LinenItem> OwnedLinenItems { get; set; } = new List<LinenItem>();
}
