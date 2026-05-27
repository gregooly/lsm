using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.Auth;

public class SignInRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>Route hint: <c>admin</c> (PulsePoint) or <c>users</c> (local DB). Not the JWT role.</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}
