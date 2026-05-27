using System.Text.Json;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class LinenAssignmentController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public LinenAssignmentController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        ulong? customerId,
        ulong? locationId,
        string? assignmentType,
        string? processStatus,
        string? physicalCondition,
        bool includeInactive = false,
        bool exceptionOnly = false,
        string sortBy = "rfid",
        string sortDir = "asc",
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        customerId = tenantCustomerId;

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 200 ? 50 : pageSize;

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new CustomerOptionViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName
            })
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.LocationName)
            .Select(x => new LocationOptionViewModel
            {
                Id = x.Id,
                LocationName = x.LocationName
            })
            .ToListAsync(cancellationToken);

        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null)
            .ThenBy(x => x.EmployeeName)
            .Select(x => new EmployeeOptionViewModel
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                OwnerCustomerId = x.OwnerCustomerId,
                EmployeeName = x.EmployeeName,
                DisplayLabel = x.OwnerCustomer != null
                    ? x.OwnerCustomer.CustomerName + " — " + x.EmployeeName
                    : x.EmployeeName
            })
            .ToListAsync(cancellationToken);

        var itemsQuery = _dbContext.LinenItems.AsNoTracking().Where(x => x.CustomerId == tenantCustomerId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            itemsQuery = itemsQuery.Where(x =>
                x.RfidTag.Contains(term)
                || x.ItemType.Contains(term)
                || (x.OwnerCustomer != null && x.OwnerCustomer.CustomerName.Contains(term))
                || (x.AssignedEmployee != null && x.AssignedEmployee.EmployeeName.Contains(term))
                || (x.CurrentLocation != null && x.CurrentLocation.LocationName.Contains(term)));
        }

        if (customerId.HasValue)
            itemsQuery = itemsQuery.Where(x => x.CustomerId == customerId.Value);

        if (locationId.HasValue)
            itemsQuery = itemsQuery.Where(x => x.CurrentLocationId == locationId.Value);

        if (!string.IsNullOrWhiteSpace(assignmentType))
        {
            var t = assignmentType.Trim().ToLowerInvariant();
            itemsQuery = itemsQuery.Where(x => x.DefaultAssignmentType == t);
        }

        if (!string.IsNullOrWhiteSpace(processStatus))
        {
            var s = processStatus.Trim().ToLowerInvariant();
            itemsQuery = itemsQuery.Where(x => x.CurrentProcessStatus == s);
        }

        if (!string.IsNullOrWhiteSpace(physicalCondition))
        {
            var c = physicalCondition.Trim().ToLowerInvariant();
            itemsQuery = itemsQuery.Where(x => x.PhysicalCondition == c);
        }

        if (!includeInactive)
            itemsQuery = itemsQuery.Where(x => x.IsActive);

        if (exceptionOnly)
        {
            var clearStatuses = await GetTemporaryClearStatusesAsync(cancellationToken);
            itemsQuery = itemsQuery.Where(x =>
                x.CurrentLocationId == null
                || (x.AssignedEmployeeId != null && x.OwnerCustomerId == null)
                || (x.AssignedEmployeeId != null && x.DefaultAssignmentType == "temporary" && clearStatuses.Contains(x.CurrentProcessStatus)));
        }

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "rfid" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        itemsQuery = sortBy switch
        {
            "owner" => desc
                ? itemsQuery.OrderByDescending(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null).ThenBy(x => x.RfidTag)
                : itemsQuery.OrderBy(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null).ThenBy(x => x.RfidTag),
            "assignment" => desc
                ? itemsQuery.OrderByDescending(x => x.DefaultAssignmentType).ThenBy(x => x.RfidTag)
                : itemsQuery.OrderBy(x => x.DefaultAssignmentType).ThenBy(x => x.RfidTag),
            "status" => desc
                ? itemsQuery.OrderByDescending(x => x.CurrentProcessStatus).ThenBy(x => x.RfidTag)
                : itemsQuery.OrderBy(x => x.CurrentProcessStatus).ThenBy(x => x.RfidTag),
            "location" => desc
                ? itemsQuery.OrderByDescending(x => x.CurrentLocation != null ? x.CurrentLocation.LocationName : null).ThenBy(x => x.RfidTag)
                : itemsQuery.OrderBy(x => x.CurrentLocation != null ? x.CurrentLocation.LocationName : null).ThenBy(x => x.RfidTag),
            "updated" => desc
                ? itemsQuery.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.RfidTag)
                : itemsQuery.OrderBy(x => x.UpdatedAt).ThenBy(x => x.RfidTag),
            _ => desc
                ? itemsQuery.OrderByDescending(x => x.RfidTag)
                : itemsQuery.OrderBy(x => x.RfidTag)
        };

        var total = await itemsQuery.CountAsync(cancellationToken);

        var items = await itemsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LinenItemListItemViewModel
            {
                Id = x.Id,
                RfidTag = x.RfidTag,
                ItemType = x.ItemType,
                SizeLabel = x.SizeLabel,
                DefaultAssignmentType = x.DefaultAssignmentType,
                OwnerCustomerId = x.OwnerCustomerId,
                AssignedEmployeeId = x.AssignedEmployeeId,
                CurrentLocationId = x.CurrentLocationId,
                CurrentProcessStatus = x.CurrentProcessStatus,
                PhysicalCondition = x.PhysicalCondition,
                OwnerCustomerName = x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null,
                AssignedEmployeeName = x.AssignedEmployee != null ? x.AssignedEmployee.EmployeeName : null,
                CurrentLocationName = x.CurrentLocation != null ? x.CurrentLocation.LocationName : null,
                IsActive = x.IsActive,
                LifecycleState = x.LifecycleState,
                LastScannedAt = x.LastScannedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var processStatuses = await _dbContext.LinenItems
            .AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.CurrentProcessStatus)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
        if (processStatuses.Count == 0)
        {
            processStatuses =
            [
                "at_customer",
                "arriving_at_laundry",
                "arrived_at_laundry",
                "waiting_for_cleaning",
                "being_cleaned",
                "cleaned",
                "ready_for_dispatch",
                "in_transit"
            ];
        }

        var physicalConditions = await _dbContext.LinenItems
            .AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.PhysicalCondition)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
        if (physicalConditions.Count == 0)
            physicalConditions = ["good", "damaged", "lost"];

        return View(new LinenAssignmentIndexViewModel
        {
            Customers = customers,
            Locations = locations,
            Employees = employees,
            Items = items,
            ProcessStatuses = processStatuses,
            PhysicalConditions = physicalConditions,
            Query = new LinenAssignmentQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                CustomerId = customerId,
                LocationId = locationId,
                AssignmentType = string.IsNullOrWhiteSpace(assignmentType) ? null : assignmentType.Trim().ToLowerInvariant(),
                ProcessStatus = string.IsNullOrWhiteSpace(processStatus) ? null : processStatus.Trim().ToLowerInvariant(),
                PhysicalCondition = string.IsNullOrWhiteSpace(physicalCondition) ? null : physicalCondition.Trim().ToLowerInvariant(),
                IncludeInactive = includeInactive,
                ExceptionOnly = exceptionOnly,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var item = await _dbContext.LinenItems
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .Select(x => new LinenItemFormViewModel
            {
                Id = x.Id,
                RfidTag = x.RfidTag,
                ItemType = x.ItemType,
                SizeLabel = x.SizeLabel,
                DefaultAssignmentType = x.DefaultAssignmentType,
                OwnerCustomerId = x.OwnerCustomerId,
                AssignedEmployeeId = x.AssignedEmployeeId,
                CurrentLocationId = x.CurrentLocationId,
                CurrentProcessStatus = x.CurrentProcessStatus,
                PhysicalCondition = x.PhysicalCondition,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
            return Json(new { ok = false, message = "Linen item not found." });

        return Json(new { ok = true, item });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LinenItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        model = NormalizeForm(model);
        ModelState.Remove(nameof(LinenItemFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        var transitionErr = ValidateProcessTransition(previousStatus: null, nextStatus: model.CurrentProcessStatus);
        if (transitionErr != null)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LinenItemFormViewModel.CurrentProcessStatus)] = [transitionErr] } });

        var empErr = await ValidateEmployeeAssignmentAsync(customerId, model.AssignedEmployeeId, model.OwnerCustomerId, cancellationToken);
        if (empErr != null)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LinenItemFormViewModel.AssignedEmployeeId)] = [empErr] } });

        var rfidExists = await _dbContext.LinenItems.AnyAsync(x => x.CustomerId == customerId && x.RfidTag == model.RfidTag, cancellationToken);
        if (rfidExists)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LinenItemFormViewModel.RfidTag)] = ["RFID tag already exists."] } });

        var entity = new LinenItem
        {
            CustomerId = customerId,
            RfidTag = model.RfidTag,
            ItemType = model.ItemType,
            SizeLabel = model.SizeLabel,
            DefaultAssignmentType = model.DefaultAssignmentType,
            OwnerCustomerId = model.OwnerCustomerId,
            AssignedEmployeeId = model.AssignedEmployeeId,
            CurrentLocationId = model.CurrentLocationId,
            CurrentProcessStatus = model.CurrentProcessStatus,
            PhysicalCondition = model.PhysicalCondition,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.LinenItems.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAssignmentEventAsync(entity.Id, "manual_ui_create", null, Snapshot(entity), null, cancellationToken);

        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LinenItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        model = NormalizeForm(model);
        if (model.Id == null || model.Id == 0)
            return Json(new { ok = false, message = "Missing id." });

        ModelState.Remove(nameof(LinenItemFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        var entity = await _dbContext.LinenItems.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Linen item not found." });

        var transitionErr = ValidateProcessTransition(entity.CurrentProcessStatus, model.CurrentProcessStatus);
        if (transitionErr != null)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LinenItemFormViewModel.CurrentProcessStatus)] = [transitionErr] } });

        var empErr = await ValidateEmployeeAssignmentAsync(customerId, model.AssignedEmployeeId, model.OwnerCustomerId, cancellationToken);
        if (empErr != null)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LinenItemFormViewModel.AssignedEmployeeId)] = [empErr] } });

        var rfidExists = await _dbContext.LinenItems.AnyAsync(x => x.CustomerId == customerId && x.Id != entity.Id && x.RfidTag == model.RfidTag, cancellationToken);
        if (rfidExists)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LinenItemFormViewModel.RfidTag)] = ["RFID tag already exists."] } });

        var previousStatus = entity.CurrentProcessStatus;
        var before = Snapshot(entity);

        entity.RfidTag = model.RfidTag;
        entity.ItemType = model.ItemType;
        entity.SizeLabel = model.SizeLabel;
        entity.DefaultAssignmentType = model.DefaultAssignmentType;
        entity.OwnerCustomerId = model.OwnerCustomerId;
        entity.AssignedEmployeeId = model.AssignedEmployeeId;
        entity.CurrentLocationId = model.CurrentLocationId;
        entity.CurrentProcessStatus = model.CurrentProcessStatus;
        entity.PhysicalCondition = model.PhysicalCondition;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        var clearStatuses = await GetTemporaryClearStatusesAsync(cancellationToken);
        LinenWorkflowHelper.ApplyTemporaryEmployeeClear(entity, previousStatus, clearStatuses);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddAssignmentEventAsync(entity.Id, "manual_ui_edit", before, Snapshot(entity), null, cancellationToken);

        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdate([FromForm] LinenAssignmentBulkUpdateViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        NormalizeBulkForm(model);
        if (model.SelectedIds.Count == 0)
            return Json(new { ok = false, message = "Select at least one item." });

        var items = await _dbContext.LinenItems
            .Where(x => x.CustomerId == customerId && model.SelectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
            return Json(new { ok = false, message = "No items found for selected IDs." });

        foreach (var entity in items)
        {
            var before = Snapshot(entity);

            var nextOwner = model.OwnerCustomerId.HasValue ? model.OwnerCustomerId : entity.OwnerCustomerId;
            var nextEmployee = model.AssignedEmployeeId.HasValue ? model.AssignedEmployeeId : entity.AssignedEmployeeId;
            var empErr = await ValidateEmployeeAssignmentAsync(customerId, nextEmployee, nextOwner, cancellationToken);
            if (empErr != null)
            {
                return Json(new
                {
                    ok = false,
                    message = $"Item {entity.RfidTag}: {empErr}"
                });
            }

            var nextStatus = model.CurrentProcessStatus ?? entity.CurrentProcessStatus;
            var transitionErr = ValidateProcessTransition(entity.CurrentProcessStatus, nextStatus);
            if (transitionErr != null)
            {
                return Json(new
                {
                    ok = false,
                    message = $"Item {entity.RfidTag}: {transitionErr}"
                });
            }

            var previousStatus = entity.CurrentProcessStatus;
            if (model.DefaultAssignmentType != null)
                entity.DefaultAssignmentType = model.DefaultAssignmentType;
            if (model.OwnerCustomerId.HasValue)
                entity.OwnerCustomerId = model.OwnerCustomerId;
            if (model.AssignedEmployeeId.HasValue)
                entity.AssignedEmployeeId = model.AssignedEmployeeId;
            if (model.CurrentLocationId.HasValue)
                entity.CurrentLocationId = model.CurrentLocationId;
            if (model.CurrentProcessStatus != null)
                entity.CurrentProcessStatus = model.CurrentProcessStatus;
            if (model.PhysicalCondition != null)
                entity.PhysicalCondition = model.PhysicalCondition;
            if (model.IsActive.HasValue)
                entity.IsActive = model.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow;

            var clearStatuses = await GetTemporaryClearStatusesAsync(cancellationToken);
            LinenWorkflowHelper.ApplyTemporaryEmployeeClear(entity, previousStatus, clearStatuses);

            _dbContext.LinenAssignmentEvents.Add(new LinenAssignmentEvent
            {
                CustomerId = customerId,
                LinenItemId = entity.Id,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = User.Identity?.Name,
                ChangeSource = "manual_ui_bulk",
                FromJson = before,
                ToJson = Snapshot(entity)
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true, message = $"Updated {items.Count} item(s)." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.LinenItems.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Linen item not found." });

        var linkedScans = await _dbContext.LinenMovementEvents.AnyAsync(x => x.CustomerId == customerId && x.LinenItemId == id, cancellationToken);
        if (linkedScans)
            return Json(new { ok = false, message = "Cannot delete: this linen item has scan history." });

        var linkedJobs = await _dbContext.JobExpectedItems.AnyAsync(x => x.CustomerId == customerId && x.LinenItemId == id, cancellationToken);
        if (linkedJobs)
            return Json(new { ok = false, message = "Cannot delete: this linen item is referenced by logistics jobs." });

        _dbContext.LinenItems.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Json(new { ok = true });
    }

    private async Task<IReadOnlyList<string>> GetTemporaryClearStatusesAsync(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return [];

        var row = await _dbContext.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.SettingKey == SettingsKeys.WorkflowClearTemporaryEmployeeOnStatus, cancellationToken);
        return LinenWorkflowHelper.ParseStatusList(row?.SettingValue);
    }

    private async Task<string?> ValidateEmployeeAssignmentAsync(
        ulong tenantCustomerId,
        ulong? assignedEmployeeId,
        ulong? ownerCustomerId,
        CancellationToken cancellationToken)
    {
        if (assignedEmployeeId == null)
            return null;

        if (ownerCustomerId == null)
            return "Select an owner customer before assigning an employee.";

        var emp = await _dbContext.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == assignedEmployeeId.Value && x.CustomerId == tenantCustomerId, cancellationToken);
        if (emp == null)
            return "Employee not found.";

        if (emp.OwnerCustomerId != ownerCustomerId.Value)
            return "Employee must belong to the selected owner customer.";

        return null;
    }

    private static string? ValidateProcessTransition(string? previousStatus, string nextStatus)
    {
        var next = NormalizeStatus(nextStatus);
        if (next == null)
            return "Current status is required.";

        var prev = NormalizeStatus(previousStatus);
        if (prev == null)
            return null;
        if (prev == next)
            return null;

        return AllowedTransitions.TryGetValue(prev, out var allowed) && allowed.Contains(next)
            ? null
            : $"Invalid status transition: {prev} -> {next}.";
    }

    private static string? NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

    private static LinenItemFormViewModel NormalizeForm(LinenItemFormViewModel model)
    {
        model.RfidTag = (model.RfidTag ?? string.Empty).Trim();
        model.ItemType = (model.ItemType ?? string.Empty).Trim();
        model.SizeLabel = string.IsNullOrWhiteSpace(model.SizeLabel) ? null : model.SizeLabel.Trim();

        model.DefaultAssignmentType = string.IsNullOrWhiteSpace(model.DefaultAssignmentType)
            ? "fixed"
            : model.DefaultAssignmentType.Trim().ToLowerInvariant();
        if (model.DefaultAssignmentType is not ("fixed" or "temporary"))
            model.DefaultAssignmentType = "fixed";

        model.CurrentProcessStatus = string.IsNullOrWhiteSpace(model.CurrentProcessStatus)
            ? "at_customer"
            : model.CurrentProcessStatus.Trim().ToLowerInvariant();

        model.PhysicalCondition = string.IsNullOrWhiteSpace(model.PhysicalCondition)
            ? "good"
            : model.PhysicalCondition.Trim().ToLowerInvariant();

        if (model.OwnerCustomerId == 0)
            model.OwnerCustomerId = null;
        if (model.AssignedEmployeeId == 0)
            model.AssignedEmployeeId = null;
        if (model.CurrentLocationId == 0)
            model.CurrentLocationId = null;

        return model;
    }

    private static void NormalizeBulkForm(LinenAssignmentBulkUpdateViewModel model)
    {
        model.DefaultAssignmentType = string.IsNullOrWhiteSpace(model.DefaultAssignmentType)
            ? null
            : model.DefaultAssignmentType.Trim().ToLowerInvariant();
        if (model.DefaultAssignmentType is not (null or "fixed" or "temporary"))
            model.DefaultAssignmentType = null;

        model.CurrentProcessStatus = string.IsNullOrWhiteSpace(model.CurrentProcessStatus)
            ? null
            : model.CurrentProcessStatus.Trim().ToLowerInvariant();
        model.PhysicalCondition = string.IsNullOrWhiteSpace(model.PhysicalCondition)
            ? null
            : model.PhysicalCondition.Trim().ToLowerInvariant();
    }

    private static string Snapshot(LinenItem entity)
    {
        return JsonSerializer.Serialize(new
        {
            entity.Id,
            entity.RfidTag,
            entity.DefaultAssignmentType,
            entity.OwnerCustomerId,
            entity.AssignedEmployeeId,
            entity.CurrentLocationId,
            entity.CurrentProcessStatus,
            entity.PhysicalCondition,
            entity.IsActive,
            entity.UpdatedAt
        });
    }

    private async Task AddAssignmentEventAsync(
        ulong linenItemId,
        string source,
        string? fromJson,
        string? toJson,
        string? note,
        CancellationToken cancellationToken)
    {
        _dbContext.LinenAssignmentEvents.Add(new LinenAssignmentEvent
        {
            LinenItemId = linenItemId,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = User.Identity?.Name,
            ChangeSource = source,
            FromJson = fromJson,
            ToJson = toJson,
            Note = note
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["at_customer"] = ["arriving_at_laundry", "in_transit"],
        ["arriving_at_laundry"] = ["arrived_at_laundry"],
        ["arrived_at_laundry"] = ["waiting_for_cleaning", "being_cleaned"],
        ["waiting_for_cleaning"] = ["being_cleaned"],
        ["being_cleaned"] = ["cleaned"],
        ["cleaned"] = ["ready_for_dispatch", "at_customer"],
        ["ready_for_dispatch"] = ["in_transit", "at_customer"],
        ["in_transit"] = ["at_customer", "arrived_at_laundry"]
    };
}
