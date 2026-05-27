using LaundryMS.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Data;

public class LaundryMsDbContext : DbContext
{
    public LaundryMsDbContext(DbContextOptions<LaundryMsDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<ReaderEvent> ReaderEvents => Set<ReaderEvent>();
    public DbSet<ReaderWay> ReaderWays => Set<ReaderWay>();
    public DbSet<ReaderWayEvent> ReaderWayEvents => Set<ReaderWayEvent>();
    public DbSet<LinenItem> LinenItems => Set<LinenItem>();
    public DbSet<LinenMovementEvent> LinenMovementEvents => Set<LinenMovementEvent>();
    public DbSet<LogisticsJob> LogisticsJobs => Set<LogisticsJob>();
    public DbSet<JobExpectedItem> JobExpectedItems => Set<JobExpectedItem>();
    public DbSet<LinenAssignmentEvent> LinenAssignmentEvents => Set<LinenAssignmentEvent>();
    public DbSet<LinenQualityEvent> LinenQualityEvents => Set<LinenQualityEvent>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.CustomerName).HasMaxLength(150).HasColumnName("customer_name");
            entity.Property(x => x.CustomerType).HasMaxLength(32).HasColumnName("customer_type");
            entity.Property(x => x.PrimaryEmail).HasMaxLength(150).HasColumnName("primary_email");
            entity.Property(x => x.PrimaryPhone).HasMaxLength(30).HasColumnName("primary_phone");
            entity.Property(x => x.AddressText).HasColumnName("address_text");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(150).HasColumnName("name");
            entity.Property(x => x.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(x => x.PasswordHash).HasMaxLength(255).HasColumnName("password_hash");
            entity.Property(x => x.Role).HasMaxLength(32).HasColumnName("role");
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.OwnerCustomerId).HasColumnName("owner_customer_id");
            entity.Property(x => x.EmployeeName).HasMaxLength(150).HasColumnName("employee_name");
            entity.Property(x => x.EmployeeCode).HasMaxLength(60).HasColumnName("employee_code");
            entity.Property(x => x.SizeProfileText).HasMaxLength(120).HasColumnName("size_profile_text");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.OwnerCustomerId);
            entity.HasOne(x => x.OwnerCustomer)
                .WithMany()
                .HasForeignKey(x => x.OwnerCustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("drivers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.DriverName).HasMaxLength(150).HasColumnName("driver_name");
            entity.Property(x => x.MobilePhone).HasMaxLength(30).HasColumnName("mobile_phone");
            entity.Property(x => x.VehicleRegistrationNo).HasMaxLength(30).HasColumnName("vehicle_registration_no");
            entity.Property(x => x.HandheldDeviceId).HasMaxLength(120).HasColumnName("handheld_device_id");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.LocationName).HasMaxLength(150).HasColumnName("location_name");
            entity.Property(x => x.LocationType).HasMaxLength(32).HasColumnName("location_type");
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.LocationAddressText).HasColumnName("location_address_text");
            entity.Property(x => x.ContactPerson).HasMaxLength(120).HasColumnName("contact_person");
            entity.Property(x => x.ContactPhone).HasMaxLength(30).HasColumnName("contact_phone");
            entity.Property(x => x.GeoLat).HasPrecision(10, 7).HasColumnName("geo_lat");
            entity.Property(x => x.GeoLng).HasPrecision(10, 7).HasColumnName("geo_lng");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<Reader>(entity =>
        {
            entity.ToTable("readers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.ReaderName).HasMaxLength(160).HasColumnName("reader_name");
            entity.Property(x => x.DeviceIdentifier).HasMaxLength(120).HasColumnName("device_identifier");
            entity.Property(x => x.DeviceModel).HasMaxLength(100).HasColumnName("device_model");
            entity.Property(x => x.ReaderCategory).HasMaxLength(24).HasColumnName("reader_category");
            entity.Property(x => x.InstalledAt).HasColumnName("installed_at");
            entity.Property(x => x.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
            entity.Property(x => x.MaintenanceNote).HasMaxLength(300).HasColumnName("maintenance_note");
            entity.Property(x => x.MqttUsername).HasMaxLength(64).HasColumnName("mqtt_username");
            entity.Property(x => x.MqttPasswordHash).HasMaxLength(255).HasColumnName("mqtt_password_hash");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.MqttUsername).IsUnique();
        });

        modelBuilder.Entity<ReaderEvent>(entity =>
        {
            entity.ToTable("reader_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.ReaderId).HasColumnName("reader_id");
            entity.Property(x => x.EventType).HasMaxLength(40).HasColumnName("event_type");
            entity.Property(x => x.Note).HasMaxLength(400).HasColumnName("note");
            entity.Property(x => x.ChangedBy).HasMaxLength(120).HasColumnName("changed_by");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId);
            entity.HasIndex(x => new { x.ReaderId, x.CreatedAt });
        });

        modelBuilder.Entity<ReaderWay>(entity =>
        {
            entity.ToTable("reader_ways");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.ReaderId).HasColumnName("reader_id");
            entity.Property(x => x.WayName).HasMaxLength(180).HasColumnName("way_name");
            entity.Property(x => x.MovementDirection).HasMaxLength(16).HasColumnName("movement_direction");
            entity.Property(x => x.BusinessPurposeKey).HasMaxLength(60).HasColumnName("business_purpose_key");
            entity.Property(x => x.FromLocationId).HasColumnName("from_location_id");
            entity.Property(x => x.ToLocationId).HasColumnName("to_location_id");
            entity.Property(x => x.TargetProcessStatus).HasMaxLength(40).HasColumnName("target_process_status");
            entity.Property(x => x.AntennaIndex).HasColumnName("antenna_index");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => new { x.ReaderId, x.AntennaIndex }).IsUnique().HasDatabaseName("uq_reader_ways_antenna_per_reader");
            entity.HasOne(x => x.Reader).WithMany(x => x.ReaderWays).HasForeignKey(x => x.ReaderId);
            entity.HasOne(x => x.FromLocation)
                .WithMany()
                .HasForeignKey(x => x.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.HasOne(x => x.ToLocation)
                .WithMany()
                .HasForeignKey(x => x.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        modelBuilder.Entity<ReaderWayEvent>(entity =>
        {
            entity.ToTable("reader_way_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.ReaderWayId).HasColumnName("reader_way_id");
            entity.Property(x => x.EventType).HasMaxLength(40).HasColumnName("event_type");
            entity.Property(x => x.Note).HasMaxLength(400).HasColumnName("note");
            entity.Property(x => x.ChangedBy).HasMaxLength(120).HasColumnName("changed_by");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.ReaderWay).WithMany().HasForeignKey(x => x.ReaderWayId);
            entity.HasIndex(x => new { x.ReaderWayId, x.CreatedAt });
        });

        modelBuilder.Entity<LinenItem>(entity =>
        {
            entity.ToTable("linen_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.RfidTag).HasMaxLength(120).HasColumnName("rfid_tag");
            entity.Property(x => x.ItemType).HasMaxLength(100).HasColumnName("item_type");
            entity.Property(x => x.SizeLabel).HasMaxLength(20).HasColumnName("size_label");
            entity.Property(x => x.DefaultAssignmentType).HasMaxLength(24).HasColumnName("default_assignment_type");
            entity.Property(x => x.OwnerCustomerId).HasColumnName("owner_customer_id");
            entity.Property(x => x.AssignedEmployeeId).HasColumnName("assigned_employee_id");
            entity.Property(x => x.CurrentLocationId).HasColumnName("current_location_id");
            entity.Property(x => x.CurrentProcessStatus).HasMaxLength(40).HasColumnName("current_process_status");
            entity.Property(x => x.PhysicalCondition).HasMaxLength(24).HasColumnName("physical_condition");
            entity.Property(x => x.LastScannedAt).HasColumnName("last_scanned_at");
            entity.Property(x => x.LifecycleState).HasMaxLength(24).HasColumnName("lifecycle_state");
            entity.Property(x => x.DeactivationReason).HasMaxLength(200).HasColumnName("deactivation_reason");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.OwnerCustomer).WithMany(x => x.OwnedLinenItems).HasForeignKey(x => x.OwnerCustomerId).IsRequired(false);
            entity.HasOne(x => x.AssignedEmployee)
                .WithMany()
                .HasForeignKey(x => x.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(x => x.CurrentLocation)
                .WithMany()
                .HasForeignKey(x => x.CurrentLocationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        modelBuilder.Entity<LinenMovementEvent>(entity =>
        {
            entity.ToTable("linen_movement_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.LinenItemId).HasColumnName("linen_item_id");
            entity.Property(x => x.ReaderId).HasColumnName("reader_id");
            entity.Property(x => x.ReaderWayId).HasColumnName("reader_way_id");
            entity.Property(x => x.LogisticsJobId).HasColumnName("logistics_job_id");
            entity.Property(x => x.DriverId).HasColumnName("driver_id");
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            entity.Property(x => x.ReceivedAtServer).HasColumnName("received_at_server");
            entity.Property(x => x.IdempotencyKey).HasMaxLength(64).HasColumnName("idempotency_key");
            entity.Property(x => x.ProcessingResult).HasMaxLength(24).HasColumnName("processing_result");
            entity.Property(x => x.RejectionReason).HasMaxLength(200).HasColumnName("rejection_reason");
            entity.Property(x => x.ConditionAfterEvent).HasMaxLength(24).HasColumnName("condition_after_event");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasOne(x => x.LinenItem).WithMany().HasForeignKey(x => x.LinenItemId);
            entity.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId);
            entity.HasOne(x => x.ReaderWay).WithMany().HasForeignKey(x => x.ReaderWayId);
            entity.HasOne(x => x.LogisticsJob)
                .WithMany()
                .HasForeignKey(x => x.LogisticsJobId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).IsRequired(false);
            entity.HasIndex(x => x.LogisticsJobId);
            entity.HasIndex(x => new { x.ReaderWayId, x.OccurredAt });
        });

        modelBuilder.Entity<LogisticsJob>(entity =>
        {
            entity.ToTable("logistics_jobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.JobType).HasMaxLength(32).HasColumnName("job_type");
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.DriverId).HasColumnName("driver_id");
            entity.Property(x => x.FromLocationId).HasColumnName("from_location_id");
            entity.Property(x => x.ToLocationId).HasColumnName("to_location_id");
            entity.Property(x => x.ReaderWayId).HasColumnName("reader_way_id");
            entity.Property(x => x.JobStatus).HasMaxLength(24).HasColumnName("job_status");
            entity.Property(x => x.PlannedStartAt).HasColumnName("planned_start_at");
            entity.Property(x => x.PlannedEndAt).HasColumnName("planned_end_at");
            entity.Property(x => x.ActualStartAt).HasColumnName("actual_start_at");
            entity.Property(x => x.ActualEndAt).HasColumnName("actual_end_at");
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).IsRequired(false);
            entity.HasOne(x => x.FromLocation)
                .WithMany()
                .HasForeignKey(x => x.FromLocationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(x => x.ToLocation)
                .WithMany()
                .HasForeignKey(x => x.ToLocationId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(x => x.ReaderWay)
                .WithMany()
                .HasForeignKey(x => x.ReaderWayId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        modelBuilder.Entity<JobExpectedItem>(entity =>
        {
            entity.ToTable("job_expected_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.LogisticsJobId).HasColumnName("logistics_job_id");
            entity.Property(x => x.LinenItemId).HasColumnName("linen_item_id");
            entity.Property(x => x.ExpectedProcessStatus).HasMaxLength(40).HasColumnName("expected_process_status");
            entity.Property(x => x.ReachedExpectedStatus).HasColumnName("reached_expected_status");
            entity.Property(x => x.ReachedAt).HasColumnName("reached_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.LogisticsJob).WithMany(x => x.ExpectedItems).HasForeignKey(x => x.LogisticsJobId);
            entity.HasOne(x => x.LinenItem).WithMany().HasForeignKey(x => x.LinenItemId);
        });

        modelBuilder.Entity<LinenAssignmentEvent>(entity =>
        {
            entity.ToTable("linen_assignment_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.LinenItemId).HasColumnName("linen_item_id");
            entity.Property(x => x.ChangedAt).HasColumnName("changed_at");
            entity.Property(x => x.ChangedBy).HasMaxLength(120).HasColumnName("changed_by");
            entity.Property(x => x.ChangeSource).HasMaxLength(60).HasColumnName("change_source");
            entity.Property(x => x.FromJson).HasColumnName("from_json");
            entity.Property(x => x.ToJson).HasColumnName("to_json");
            entity.Property(x => x.Note).HasMaxLength(300).HasColumnName("note");
            entity.HasOne(x => x.LinenItem).WithMany().HasForeignKey(x => x.LinenItemId);
            entity.HasIndex(x => new { x.LinenItemId, x.ChangedAt });
            entity.HasIndex(x => x.ChangedAt);
        });

        modelBuilder.Entity<LinenQualityEvent>(entity =>
        {
            entity.ToTable("linen_quality_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.LinenItemId).HasColumnName("linen_item_id");
            entity.Property(x => x.EventType).HasMaxLength(40).HasColumnName("event_type");
            entity.Property(x => x.FromCondition).HasMaxLength(24).HasColumnName("from_condition");
            entity.Property(x => x.ToCondition).HasMaxLength(24).HasColumnName("to_condition");
            entity.Property(x => x.Note).HasMaxLength(500).HasColumnName("note");
            entity.Property(x => x.ReportedBy).HasMaxLength(120).HasColumnName("reported_by");
            entity.Property(x => x.ResolvedBy).HasMaxLength(120).HasColumnName("resolved_by");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.LinenItem).WithMany().HasForeignKey(x => x.LinenItemId);
            entity.HasIndex(x => new { x.LinenItemId, x.CreatedAt });
            entity.HasIndex(x => new { x.EventType, x.CreatedAt });
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.SettingKey).HasMaxLength(120).HasColumnName("setting_key");
            entity.Property(x => x.SettingValue).HasColumnName("setting_value");
            entity.Property(x => x.IsSecret).HasColumnName("is_secret");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(x => x.SettingKey).IsUnique();
        });

    }
}
