using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Lucene;
using Sitecore.ContentSearch.Linq.Parsing;

namespace Sitecore.Support.ContentSearch.LuceneProvider
{
  public class LinqToLuceneIndex<TItem>: Sitecore.ContentSearch.LuceneProvider.LinqToLuceneIndex<TItem>
  {
    private readonly QueryMapper<LuceneQuery> queryMapper;


    public LinqToLuceneIndex(LuceneSearchContext context) : this(context,null)
    {
    }

    public LinqToLuceneIndex(LuceneSearchContext context, IExecutionContext executionContext) : base(context, executionContext)
    {
      this.queryMapper = new Sitecore.Support.ContentSearch.Linq.Lucene.LuceneQueryMapper(((Sitecore.ContentSearch.Linq.Lucene.LuceneQueryMapper)base.QueryMapper).Parameters);
    }

    protected override QueryMapper<LuceneQuery> QueryMapper
    {
      get { return this.queryMapper; }
    }
  }
}