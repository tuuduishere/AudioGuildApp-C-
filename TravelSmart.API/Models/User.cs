using System;
using System.Collections.Generic;

namespace TravelSmart.API.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Email { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public string? MerchantRequestStatus { get; set; }

    public virtual ICollection<Poi> Pois { get; set; } = new List<Poi>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<VisitLog> VisitLogs { get; set; } = new List<VisitLog>();
}
