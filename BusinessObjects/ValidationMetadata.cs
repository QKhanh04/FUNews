using System.ComponentModel.DataAnnotations;

namespace DataAccessObjects;

[MetadataType(typeof(SystemAccountMetadata))]
public partial class SystemAccount
{
}

[MetadataType(typeof(CategoryMetadata))]
public partial class Category
{
}

[MetadataType(typeof(NewsArticleMetadata))]
public partial class NewsArticle
{
}

[MetadataType(typeof(TagMetadata))]
public partial class Tag
{
}

internal sealed class SystemAccountMetadata
{
    [Required(ErrorMessage = "Account name is required.")]
    [StringLength(100, ErrorMessage = "Account name cannot exceed 100 characters.")]
    public string? AccountName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(70, ErrorMessage = "Email cannot exceed 70 characters.")]
    public string? AccountEmail { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [Range(1, 2, ErrorMessage = "Role must be Staff or Lecturer.")]
    public int? AccountRole { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(70, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 70 characters.")]
    public string? AccountPassword { get; set; }
}

internal sealed class CategoryMetadata
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category description is required.")]
    [StringLength(250, ErrorMessage = "Category description cannot exceed 250 characters.")]
    public string CategoryDesciption { get; set; } = string.Empty;
}

internal sealed class NewsArticleMetadata
{
    [Required(ErrorMessage = "News title is required.")]
    [StringLength(400, ErrorMessage = "News title cannot exceed 400 characters.")]
    public string? NewsTitle { get; set; }

    [Required(ErrorMessage = "Headline is required.")]
    [StringLength(150, ErrorMessage = "Headline cannot exceed 150 characters.")]
    public string Headline { get; set; } = string.Empty;

    [Required(ErrorMessage = "News content is required.")]
    [StringLength(4000, ErrorMessage = "News content cannot exceed 4000 characters.")]
    public string? NewsContent { get; set; }

    [Required(ErrorMessage = "News source is required.")]
    [StringLength(400, ErrorMessage = "News source cannot exceed 400 characters.")]
    public string? NewsSource { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public short? CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "Thumbnail URL cannot exceed 500 characters.")]
    [Url(ErrorMessage = "Thumbnail URL must be a valid URL.")]
    public string? ThumbnailUrl { get; set; }
}

internal sealed class TagMetadata
{
    [Required(ErrorMessage = "Tag name is required.")]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters.")]
    public string? TagName { get; set; }

    [StringLength(400, ErrorMessage = "Tag note cannot exceed 400 characters.")]
    public string? Note { get; set; }
}
