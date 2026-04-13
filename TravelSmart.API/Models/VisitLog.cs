using System;
using System.Collections.Generic;

namespace TravelSmart.API.Models;

public partial class VisitLog
{
    public Guid LogId { get; set; }

    public Guid PoiId { get; set; }

    public Guid? UserId { get; set; }

    public string? VisitType { get; set; }

    public DateTime? VisitTime { get; set; }

    public virtual Poi Poi { get; set; } = null!;

    public virtual User? User { get; set; }
}
