using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.LuceneProvider;
using Sitecore.ContentSearch.Security;
using System.Linq;

namespace Sitecore.Support.ContentSearch.LuceneProvider
{
  public class LuceneSearchContext: Sitecore.ContentSearch.LuceneProvider.LuceneSearchContext, IProviderSearchContext
  {
    public LuceneSearchContext(ILuceneProviderIndex index, SearchSecurityOptions securityOptions = SearchSecurityOptions.EnableSecurityCheck) : base(index, securityOptions)
    {
    }

    public override IQueryable<TItem> GetQueryable<TItem>()
    {
      return this.GetQueryable<TItem>(default(TItem) as IExecutionContext);
    }

    public override IQueryable<TItem> GetQueryable<TItem>(IExecutionContext executionContext)
    {
      LinqToLuceneIndex<TItem> index = new Sitecore.Support.ContentSearch.LuceneProvider.LinqToLuceneIndex<TItem>(this, executionContext);
      //This part will not work, because TraceWriter is internal.
      /*if (ContentSearchConfigurationSettings.EnableSearchDebug)
      {
        index.TraceWriter = new LoggingTraceWriter(SearchLog.Log);
      }*/
      return index.GetQueryable();
    }
  }
}