using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AgroUnion.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(180)] public string FullNameOrCompany { get; set; } = string.Empty;
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
