using System.ComponentModel.DataAnnotations;
using LaundryMS.Web.Auth;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class CustomersController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public CustomersController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        string? customerType,
        bool includeInactive = false,
        string sortBy = "name",
        string sortDir = "asc",
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 200 ? 50 : pageSize;

        var baseQuery = _dbContext.Customers.AsNoTracking().Where(x => x.CustomerId == customerId);
        if (!includeInactive)
            baseQuery = baseQuery.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            baseQuery = baseQuery.Where(x =>
                x.CustomerName.Contains(term)
                || (x.PrimaryEmail != null && x.PrimaryEmail.Contains(term))
                || (x.PrimaryPhone != null && x.PrimaryPhone.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(customerType))
        {
            var ct = customerType.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(x => x.CustomerType == ct);
        }

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        baseQuery = sortBy switch
        {
            "type" => desc
                ? baseQuery.OrderByDescending(x => x.CustomerType).ThenBy(x => x.CustomerName)
                : baseQuery.OrderBy(x => x.CustomerType).ThenBy(x => x.CustomerName),
            "status" => desc
                ? baseQuery.OrderByDescending(x => x.IsActive).ThenBy(x => x.CustomerName)
                : baseQuery.OrderBy(x => x.IsActive).ThenBy(x => x.CustomerName),
            _ => desc
                ? baseQuery.OrderByDescending(x => x.CustomerName)
                : baseQuery.OrderBy(x => x.CustomerName)
        };

        var total = await baseQuery.CountAsync(cancellationToken);

        var pageRows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.CustomerName,
                x.CustomerType,
                x.PrimaryEmail,
                x.PrimaryPhone,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        var pageIds = pageRows.Select(x => x.Id).ToList();
        var metrics = await LoadCustomerMetricsAsync(pageIds, customerId, cancellationToken);

        var items = pageRows.Select(x => new CustomerListItemViewModel
        {
            Id = x.Id,
            CustomerName = x.CustomerName,
            CustomerType = x.CustomerType,
            CustomerTypeDisplay = CustomerTypeFormatter.ToDisplay(x.CustomerType),
            PrimaryEmail = x.PrimaryEmail,
            PrimaryPhone = x.PrimaryPhone,
            IsActive = x.IsActive,
            EmployeeCount = metrics.Employees.GetValueOrDefault(x.Id),
            OwnedLinenActiveCount = metrics.LinenActive.GetValueOrDefault(x.Id),
            OwnedLinenTotalCount = metrics.LinenTotal.GetValueOrDefault(x.Id),
            OpenLogisticsJobsCount = metrics.OpenJobs.GetValueOrDefault(x.Id),
            DamagedLinenCount = metrics.DamagedLinen.GetValueOrDefault(x.Id)
        }).ToList();

        return View(new CustomersIndexViewModel
        {
            Items = items,
            Query = new CustomersQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                CustomerType = string.IsNullOrWhiteSpace(customerType) ? null : customerType.Trim().ToLowerInvariant(),
                IncludeInactive = includeInactive,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var c = await _dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

        if (c is null)
            return NotFound();

        var metrics = await LoadCustomerMetricsAsync([id], customerId, cancellationToken);

        var model = new CustomerDetailViewModel
        {
            Id = c.Id,
            CustomerName = c.CustomerName,
            CustomerType = c.CustomerType,
            CustomerTypeDisplay = CustomerTypeFormatter.ToDisplay(c.CustomerType),
            PrimaryEmail = c.PrimaryEmail,
            PrimaryPhone = c.PrimaryPhone,
            AddressText = c.AddressText,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            EmployeeCount = metrics.Employees.GetValueOrDefault(id),
            OwnedLinenActiveCount = metrics.LinenActive.GetValueOrDefault(id),
            OwnedLinenTotalCount = metrics.LinenTotal.GetValueOrDefault(id),
            OpenLogisticsJobsCount = metrics.OpenJobs.GetValueOrDefault(id),
            DamagedLinenCount = metrics.DamagedLinen.GetValueOrDefault(id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var c = await _dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

        if (c is null)
            return NotFound();

        return Json(new
        {
            c.Id,
            c.CustomerName,
            c.CustomerType,
            c.PrimaryEmail,
            c.PrimaryPhone,
            c.AddressText,
            c.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CustomerFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        MergeFormIntoModel(Request.Form, model, includeId: false);
        ModelState.Remove(nameof(CustomerFormViewModel.CustomerType));
        ModelState.Remove(nameof(CustomerFormViewModel.IsActive));

        NormalizeForm(model);
        if (!TryValidateEmail(model))
            return ModalValidationResult();

        TryValidateModel(model);
        if (!ModelState.IsValid)
            return ModalValidationResult();

        var now = DateTime.UtcNow;
        _dbContext.Customers.Add(new Customer
        {
            CustomerId = customerId,
            CustomerName = model.CustomerName.Trim(),
            CustomerType = NormalizeCustomerTypeForStorage(model.CustomerType),
            PrimaryEmail = model.PrimaryEmail,
            PrimaryPhone = model.PrimaryPhone,
            AddressText = model.AddressText,
            IsActive = model.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] CustomerFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        MergeFormIntoModel(Request.Form, model, includeId: true);
        ModelState.Remove(nameof(CustomerFormViewModel.Id));
        ModelState.Remove(nameof(CustomerFormViewModel.CustomerType));
        ModelState.Remove(nameof(CustomerFormViewModel.IsActive));

        if (model.Id is null or 0)
        {
            ModelState.AddModelError(nameof(model.Id), "Missing customer id.");
            return ModalValidationResult();
        }

        NormalizeForm(model);
        if (!TryValidateEmail(model))
            return ModalValidationResult();

        TryValidateModel(model);
        if (!ModelState.IsValid)
            return ModalValidationResult();

        var entity = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.CustomerName = model.CustomerName.Trim();
        var newType = NormalizeCustomerTypeForStorage(model.CustomerType);
        entity.CustomerType = newType;
        entity.PrimaryEmail = model.PrimaryEmail;
        entity.PrimaryPhone = model.PrimaryPhone;
        entity.AddressText = model.AddressText;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        var linenCount = await _dbContext.LinenItems.CountAsync(x => x.CustomerId == customerId && x.OwnerCustomerId == id, cancellationToken);
        var employeeCount = await _dbContext.Employees.CountAsync(x => x.OwnerCustomerId == id, cancellationToken);
        var jobCount = await _dbContext.LogisticsJobs.CountAsync(x => x.CustomerId == customerId, cancellationToken);

        if (linenCount > 0 || employeeCount > 0 || jobCount > 0)
        {
            return BadRequest(new
            {
                ok = false,
                message = "Cannot delete this customer while linen, employees, or logistics jobs are still linked. Remove those links first, or use Deactivate."
            });
        }

        _dbContext.Customers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    private sealed class CustomerMetricsBundle
    {
        public Dictionary<ulong, int> Employees { get; init; } = new();
        public Dictionary<ulong, int> LinenActive { get; init; } = new();
        public Dictionary<ulong, int> LinenTotal { get; init; } = new();
        public Dictionary<ulong, int> DamagedLinen { get; init; } = new();
        public Dictionary<ulong, int> OpenJobs { get; init; } = new();
    }

    private async Task<CustomerMetricsBundle> LoadCustomerMetricsAsync(
        IReadOnlyList<ulong> customerIds,
        ulong customerId,
        CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
            return new CustomerMetricsBundle();

        var employees = await _dbContext.Employees.AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.OwnerCustomerId != null && customerIds.Contains(e.OwnerCustomerId.Value))
            .GroupBy(e => e.OwnerCustomerId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var linenActive = await _dbContext.LinenItems.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.OwnerCustomerId != null && customerIds.Contains(i.OwnerCustomerId.Value) && i.IsActive)
            .GroupBy(i => i.OwnerCustomerId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var linenTotal = await _dbContext.LinenItems.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.OwnerCustomerId != null && customerIds.Contains(i.OwnerCustomerId.Value))
            .GroupBy(i => i.OwnerCustomerId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var damaged = await _dbContext.LinenItems.AsNoTracking()
            .Where(i =>
                i.OwnerCustomerId != null
                && i.CustomerId == customerId
                && customerIds.Contains(i.OwnerCustomerId.Value)
                && i.IsActive
                && i.PhysicalCondition != "good")
            .GroupBy(i => i.OwnerCustomerId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        // Jobs are tenant-scoped only; there is no direct job→client link in the schema.
        var openJobs = new Dictionary<ulong, int>();

        return new CustomerMetricsBundle
        {
            Employees = employees,
            LinenActive = linenActive,
            LinenTotal = linenTotal,
            DamagedLinen = damaged,
            OpenJobs = openJobs
        };
    }

    private IActionResult ModalValidationResult()
    {
        var errors = ModelState
            .Where(x => x.Value is { Errors.Count: > 0 })
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray());

        return BadRequest(new { ok = false, errors });
    }

    private static void MergeFormIntoModel(IFormCollection form, CustomerFormViewModel model, bool includeId)
    {
        if (TryGetFormValue(form, "CustomerType", out var type) && !string.IsNullOrWhiteSpace(type))
            model.CustomerType = type.Trim();

        if (TryGetFormValue(form, "IsActive", out var activeStr))
        {
            model.IsActive = activeStr.Equals("true", StringComparison.OrdinalIgnoreCase)
                || activeStr.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        if (includeId && TryGetFormValue(form, "Id", out var idStr) && ulong.TryParse(idStr, out var parsedId))
            model.Id = parsedId;
    }

    private static bool TryGetFormValue(IFormCollection form, string key, out string value)
    {
        value = string.Empty;
        if (!form.TryGetValue(key, out var sv))
            return false;
        value = sv.ToString();
        return !string.IsNullOrEmpty(value);
    }

    private static string NormalizeCustomerTypeForStorage(string customerType)
    {
        var t = customerType.Trim();
        if (t.Length == 0)
            return "other";
        return t.ToLowerInvariant() switch
        {
            "fixed" => "fixed",
            "rental" => "rental",
            "other" => "other",
            _ => t.Length <= 32 ? t : t[..32]
        };
    }

    private static void NormalizeForm(CustomerFormViewModel model)
    {
        model.PrimaryEmail = string.IsNullOrWhiteSpace(model.PrimaryEmail) ? null : model.PrimaryEmail.Trim();
        model.PrimaryPhone = string.IsNullOrWhiteSpace(model.PrimaryPhone) ? null : model.PrimaryPhone.Trim();
        model.AddressText = string.IsNullOrWhiteSpace(model.AddressText) ? null : model.AddressText.Trim();
        if (string.IsNullOrWhiteSpace(model.CustomerType))
            model.CustomerType = "other";
        else
            model.CustomerType = model.CustomerType.Trim();
    }

    private bool TryValidateEmail(CustomerFormViewModel model)
    {
        if (model.PrimaryEmail is null)
            return true;

        var attr = new EmailAddressAttribute();
        if (attr.IsValid(model.PrimaryEmail))
            return true;

        ModelState.AddModelError(nameof(model.PrimaryEmail), "Enter a valid email address.");
        return false;
    }
}
