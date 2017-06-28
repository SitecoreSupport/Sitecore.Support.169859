using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Security;

namespace Sitecore.Support.ContentSearch.LuceneProvider
{
  public class LuceneIndex : Sitecore.ContentSearch.LuceneProvider.LuceneIndex
  {
    public LuceneIndex(string name, string folder, IIndexPropertyStore propertyStore)
      : base(name, folder, propertyStore)
    {
    }

    public override IProviderSearchContext CreateSearchContext(SearchSecurityOptions securityOptions = SearchSecurityOptions.EnableSecurityCheck)
    {
      this.EnsureInitialized();
      return new Sitecore.Support.ContentSearch.LuceneProvider.LuceneSearchContext(this, securityOptions);
    }
  }
}