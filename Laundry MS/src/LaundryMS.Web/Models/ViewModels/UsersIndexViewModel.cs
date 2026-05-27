using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class UsersIndexViewModel
{
    public IReadOnlyList<UserListItemViewModel> Items { get; init; } = [];
    public IReadOnlyList<CustomerOptionViewModel> CustomerOptions { get; init; } = [];
    public UsersQueryViewModel Query { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class UsersQueryViewModel
{
    public string? Q { get; init; }
    public bool IncludeInactive { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDir { get; init; } = "asc";
}

public class UserListItemViewModel
{
    public ulong Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "MANAGER";
    public ulong CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class UserCreateFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Password and confirm password do not match.",
                [nameof(ConfirmPassword)]);
        }
    }
}

public class UserEditFormViewModel : IValidatableObject
{
    [Required]
    public ulong Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(200, MinimumLength = 8, ErrorMessage = "New password must be at least 8 characters.")]
    public string? NewPassword { get; set; }

    [StringLength(200, MinimumLength = 8)]
    public string? ConfirmNewPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var newPasswordProvided = !string.IsNullOrWhiteSpace(NewPassword);
        var confirmProvided = !string.IsNullOrWhiteSpace(ConfirmNewPassword);
        if (!newPasswordProvided && !confirmProvided)
        {
            yield break;
        }

        if (!newPasswordProvided || !confirmProvided)
        {
            yield return new ValidationResult(
                "Provide both new password and confirm password, or leave both empty.",
                [nameof(NewPassword), nameof(ConfirmNewPassword)]);
            yield break;
        }

        if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "New password and confirm password do not match.",
                [nameof(ConfirmNewPassword)]);
        }
    }
}
