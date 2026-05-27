namespace LaundryMS.Web.Models.Auth;

public class PulsePointUser
{
    public int Id { get; set; }
    public string? Company { get; set; }
    public string? HotelName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Contact { get; set; }
    public int Status { get; set; }
    public int Role { get; set; }
    public int IsVerify { get; set; }
}
