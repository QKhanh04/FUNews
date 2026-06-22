using DataAccessObjects;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace FUNewsManagement_API
{
    public static class ODataModelBuilder
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            
            builder.EntitySet<NewsArticle>("News").EntityType.HasKey(n => n.NewsArticleId);
            builder.EntitySet<Category>("Categories").EntityType.HasKey(c => c.CategoryId);
            builder.EntitySet<SystemAccount>("Accounts").EntityType.HasKey(a => a.AccountId);
            builder.EntitySet<Tag>("Tags").EntityType.HasKey(t => t.TagId);

            return builder.GetEdmModel();
        }
    }
}
