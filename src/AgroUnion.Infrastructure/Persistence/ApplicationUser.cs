using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AgroUnion.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(180)] public string FullNameOrCompany { get; set; } = string.Empty;
    [MaxLength(120)] public string Region { get; set; } = string.Empty;
    [MaxLength(80)] public string? MembershipCode { get; set; }
    [MaxLength(3000)] public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}
