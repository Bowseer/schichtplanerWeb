using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Data;
using Schichtplaner.Models;
using Schichtplaner.Models.ViewModels;

namespace Schichtplaner.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Users()
    {
        var model = await BuildModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(AdminUserManagementViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CreateUser.Email) || string.IsNullOrWhiteSpace(model.CreateUser.InitialPassword))
        {
            TempData["Error"] = "E-Mail und Initialpasswort sind erforderlich.";
            return RedirectToAction(nameof(Users));
        }

        var existing = await _userManager.FindByEmailAsync(model.CreateUser.Email);
        if (existing != null)
        {
            TempData["Error"] = "Benutzer existiert bereits.";
            return RedirectToAction(nameof(Users));
        }

        var user = new ApplicationUser
        {
            UserName = model.CreateUser.Email,
            Email = model.CreateUser.Email,
            DisplayName = model.CreateUser.DisplayName,
            EmailConfirmed = true,
            MustChangePassword = true,
            DefaultStandortId = model.CreateUser.DefaultStandortId,
            IsDisabled = false
        };

        var result = await _userManager.CreateAsync(user, model.CreateUser.InitialPassword);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }

        if (model.CreateUser.IsAdmin)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Planer");
        }

        TempData["Success"] = "Benutzer wurde angelegt.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Users));
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!await _userManager.IsInRoleAsync(user, "Planer"))
            {
                await _userManager.AddToRoleAsync(user, "Planer");
            }
            TempData["Success"] = "Adminrechte entfernt.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["Success"] = "Benutzer zum Admin befördert.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultStandort(string id, int? defaultStandortId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Users));
        }

        user.DefaultStandortId = defaultStandortId;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Standardstandort gespeichert.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminUserManagementViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ResetPassword.UserId) || string.IsNullOrWhiteSpace(model.ResetPassword.NewPassword))
        {
            TempData["Error"] = "Benutzer und neues Passwort sind erforderlich.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(model.ResetPassword.UserId);
        if (user == null)
        {
            TempData["Error"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Users));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.ResetPassword.NewPassword);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Passwort wurde zurückgesetzt. Benutzer muss es beim nächsten Login ändern.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDisabled(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Users));
        }

        user.IsDisabled = !user.IsDisabled;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = user.IsDisabled
            ? "Benutzer wurde deaktiviert."
            : "Benutzer wurde wieder aktiviert.";

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ChangePasswordRequired()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordRequired(string currentPassword, string newPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            ViewBag.Error = string.Join(" | ", result.Errors.Select(e => e.Description));
            return View();
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return RedirectToAction("Index", "Monatsplanung");
    }

    private async Task<AdminUserManagementViewModel> BuildModelAsync()
    {
        var users = await _userManager.Users
            .Include(u => u.DefaultStandort)
            .OrderBy(u => u.Email)
            .ToListAsync();

        var standorte = await _db.Standorte
            .OrderBy(s => s.Name)
            .ToListAsync();

        var list = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            list.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
                MustChangePassword = user.MustChangePassword,
                IsDisabled = user.IsDisabled,
                DefaultStandortId = user.DefaultStandortId,
                DefaultStandortName = user.DefaultStandort?.Name
            });
        }

        return new AdminUserManagementViewModel
        {
            Users = list,
            Standorte = standorte.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList()
        };
    }
}