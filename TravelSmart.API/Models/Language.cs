using System;
using System.Collections.Generic;

namespace TravelSmart.API.Models;

public partial class Language
{
    public string LanguageCode { get; set; } = null!;

    public string LanguageName { get; set; } = null!;

    public virtual ICollection<PoiTranslation> PoiTranslations { get; set; } = new List<PoiTranslation>();
}
