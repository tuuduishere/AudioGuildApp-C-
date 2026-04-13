using System;
using System.Collections.Generic;

namespace TravelSmart.API.Models;

public partial class Poi
{
    public Guid PoiId { get; set; }

    public Guid? OwnerId { get; set; } // ĐÂY CHÍNH LÀ SỔ ĐỎ CHỦ QUÁN NHÉ!

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int? RadiusMeter { get; set; }

    public string? QrCodeKey { get; set; }

    public bool? IsActive { get; set; }

    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Owner { get; set; }

    public virtual ICollection<PoiTranslation> PoiTranslations { get; set; } = new List<PoiTranslation>();

    public virtual ICollection<VisitLog> VisitLogs { get; set; } = new List<VisitLog>();
}