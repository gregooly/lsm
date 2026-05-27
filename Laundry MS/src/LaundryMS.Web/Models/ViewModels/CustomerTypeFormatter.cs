namespace LaundryMS.Web.Models.ViewModels;

public static class CustomerTypeFormatter
{
    public static string ToDisplay(string? customerType)
    {
        if (string.IsNullOrWhiteSpace(customerType))
            return "Other";

        return customerType.Trim().ToLowerInvariant() switch
        {
            "fixed" => "Fixed",
            "rental" => "Rental",
            "other" => "Other",
            _ => FormatTitle(customerType.Trim())
        };
    }

    private static string FormatTitle(string t)
    {
        if (t.Length == 0)
            return "Other";
        if (t.Length == 1)
            return t.ToUpperInvariant();
        return char.ToUpperInvariant(t[0]) + t[1..].ToLowerInvariant();
    }
}
