using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class LogisticsJobFormViewModel
{
    public ulong? Id { get; set; }

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string JobType { get; set; } = "collection";

    public ulong? CustomerId { get; set; }
    public ulong? DriverId { get; set; }
    public ulong? FromLocationId { get; set; }
    public ulong? ToLocationId { get; set; }
    public ulong? ReaderWayId { get; set; }

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string JobStatus { get; set; } = "open";

    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

