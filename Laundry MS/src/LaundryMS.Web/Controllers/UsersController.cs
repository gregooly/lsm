using LaundryMS.Web.Data;
using LaundryMS.Web.Auth;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class UsersController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public UsersController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        bool includeInactive = false,
        string sortBy = "name",
        string sortDir = "asc",
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var claimCustomerId = GetCurrentCustomerIdFromClaim();
        if (!claimCustomerId.HasValue)
        {
            TempData["Error"] = "Missing customer scope in token.";
            return RedirectToAction("Login", "Account");
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 200 ? 50 : pageSize;
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        var query = _dbContext.AppUsers.AsNoTracking().Where(x => x.CustomerId == claimCustomerId.Value);
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(term)
                || x.Email.Contains(term)
                || x.Role.Contains(term));
        }

        query = sortBy switch
        {
            "email" => desc ? query.OrderByDescending(x => x.Email).ThenBy(x => x.Name) : query.OrderBy(x => x.Email).ThenBy(x => x.Name),
            "role" => desc ? query.OrderByDescending(x => x.Role).ThenBy(x => x.Name) : query.OrderBy(x => x.Role).ThenBy(x => x.Name),
            "customer" => desc ? query.OrderByDescending(x => x.CustomerId).ThenBy(x => x.Name) : query.OrderBy(x => x.CustomerId).ThenBy(x => x.Name),
            "status" => desc ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name) : query.OrderBy(x => x.IsActive).ThenBy(x => x.Name),
            "updated" => desc ? query.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Name) : query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Name),
            _ => desc ? query.OrderByDescending(x => x.Name).ThenBy(x => x.Email) : query.OrderBy(x => x.Name).ThenBy(x => x.Email)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Role = x.Role,
                CustomerId = x.CustomerId,
                CustomerName = $"Customer {x.CustomerId}",
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var customerOptions = new List<CustomerOptionViewModel>
        {
            new() { Id = claimCustomerId.Value, CustomerName = $"Customer {claimCustomerId.Value}" }
        };

        var vm = new UsersIndexViewModel
        {
            Items = items,
            CustomerOptions = customerOptions,
            Query = new UsersQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                IncludeInactive = includeInactive,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        ViewData["Saved"] = TempData["Saved"];
        ViewData["Error"] = TempData["Error"];
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UserCreateFormViewModel model, CancellationToken cancellationToken)
    {
        var claimCustomerId = GetCurrentCustomerIdFromClaim();
        if (!claimCustomerId.HasValue)
        {
            TempData["Error"] = "Missing customer scope in token.";
            return RedirectToAction(nameof(Index));
        }

        NormalizeCreate(model);
        TryValidateModel(model);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetFirstValidationError() ?? "Invalid user form.";
            return RedirectToAction(nameof(Index));
        }

        var emailExists = await _dbContext.AppUsers.AsNoTracking()
            .AnyAsync(x => x.Email.ToLower() == model.Email.ToLower(), cancellationToken);
        if (emailExists)
        {
            TempData["Error"] = "Email already exists.";
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        _dbContext.AppUsers.Add(new AppUser
        {
            Name = model.Name,
            Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = "MANAGER",
            CustomerId = claimCustomerId.Value,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Saved"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] UserEditFormViewModel model, CancellationToken cancellationToken)
    {
        var claimCustomerId = GetCurrentCustomerIdFromClaim();
        if (!claimCustomerId.HasValue)
        {
            TempData["Error"] = "Missing customer scope in token.";
            return RedirectToAction(nameof(Index));
        }

        NormalizeEdit(model);
        TryValidateModel(model);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetFirstValidationError() ?? "Invalid user form.";
            return RedirectToAction(nameof(Index));
        }

        var entity = await _dbContext.AppUsers
            .FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == claimCustomerId.Value, cancellationToken);
        if (entity is null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        var duplicateEmail = await _dbContext.AppUsers.AsNoTracking()
            .AnyAsync(x => x.Id != model.Id && x.Email.ToLower() == model.Email.ToLower(), cancellationToken);
        if (duplicateEmail)
        {
            TempData["Error"] = "Email already exists.";
            return RedirectToAction(nameof(Index));
        }

        entity.Name = model.Name;
        entity.Email = model.Email;
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Saved"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(ulong id, CancellationToken cancellationToken)
    {
        var claimCustomerId = GetCurrentCustomerIdFromClaim();
        if (!claimCustomerId.HasValue)
            return Forbid();

        var entity = await _dbContext.AppUsers
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == claimCustomerId.Value, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Saved"] = "User deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ulong id, CancellationToken cancellationToken)
    {
        var claimCustomerId = GetCurrentCustomerIdFromClaim();
        if (!claimCustomerId.HasValue)
            return Forbid();

        var entity = await _dbContext.AppUsers
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == claimCustomerId.Value, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Saved"] = "User activated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id, CancellationToken cancellationToken)
    {
        var claimCustomerId = GetCurrentCustomerIdFromClaim();
        if (!claimCustomerId.HasValue)
            return Forbid();

        var entity = await _dbContext.AppUsers
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == claimCustomerId.Value, cancellationToken);
        if (entity is null)
            return NotFound();

        _dbContext.AppUsers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Saved"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeCreate(UserCreateFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
    }

    private static void NormalizeEdit(UserEditFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.NewPassword = string.IsNullOrWhiteSpace(model.NewPassword) ? null : model.NewPassword.Trim();
        model.ConfirmNewPassword = string.IsNullOrWhiteSpace(model.ConfirmNewPassword) ? null : model.ConfirmNewPassword.Trim();
    }

    private string? GetFirstValidationError()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? null : e.ErrorMessage)
            .FirstOrDefault(e => e != null);
    }

    private ulong? GetCurrentCustomerIdFromClaim()
    {
        var claimValue = User.FindFirst(AuthConstants.CustomerIdClaimType)?.Value;
        return ulong.TryParse(claimValue, out var claimCustomerId) ? claimCustomerId : null;
    }
}
