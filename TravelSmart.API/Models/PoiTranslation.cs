using System;
using System.Collections.Generic;

namespace TravelSmart.API.Models;

public partial class PoiTranslation
{
    public Guid TranslationId { get; set; }

    public Guid PoiId { get; set; }

    public string LanguageCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? AudioUrl { get; set; }

    public virtual Language LanguageCodeNavigation { get; set; } = null!;

    public virtual Poi Poi { get; set; } = null!;
}
