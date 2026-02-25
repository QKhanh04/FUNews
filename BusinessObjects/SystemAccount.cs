using System;
using System.Collections.Generic;

namespace DataAccessObjects;

public partial class SystemAccount
{
    public short AccountId { get; set; }

    public string? AccountName { get; set; }

    public string? AccountEmail { get; set; }

    public int? AccountRole { get; set; }

    public string? AccountPassword { get; set; }

    public bool? IsGoogleAccount { get; set; }

    public string? GoogleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
}
