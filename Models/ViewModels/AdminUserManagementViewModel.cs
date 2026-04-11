using Microsoft.AspNetCore.Mvc.Rendering;

namespace Schichtplaner.Models.ViewModels;

public class AdminUserManagementViewModel
{
    public CreateUserViewModel CreateUser { get; set; } = new();
    public ResetPasswordViewModel ResetPassword { get; set; } = new();
    public List<UserListItemViewModel> Users { get; set; } = new();
    public List<SelectListItem> Standorte { get; set; } = new();
}

public class CreateUserViewModel
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string InitialPassword { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int? DefaultStandortId { get; set; }
}

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsAdmin { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsDisabled { get; set; }
    public int? DefaultStandortId { get; set; }
    public string? DefaultStandortName { get; set; }
}