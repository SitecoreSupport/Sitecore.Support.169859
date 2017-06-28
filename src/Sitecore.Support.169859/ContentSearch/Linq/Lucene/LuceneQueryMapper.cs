using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Helpers;
using Sitecore.ContentSearch.Linq.Lucene;
using Sitecore.ContentSearch.Linq.Lucene.Queries;
using Sitecore.ContentSearch.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.ContentSearch.Linq.Lucene
{
  public class LuceneQueryMapper : Sitecore.ContentSearch.Linq.Lucene.LuceneQueryMapper
  {
    public LuceneQueryMapper(LuceneIndexParameters parameters) : base(parameters)
    {
    }

    private SpanQuery BuildSpanQuery(SpanSubQuery[] subqueries)
    {
      SpanQuery query = null;
      List<SpanQuery> list = new List<SpanQuery>();
      SpanSubQuery[] queryArray = subqueries;
      for (int i = 0; i < queryArray.Length; i++)
      {
        int slop = (i > 0) ? ((queryArray[i].Position - queryArray[i - 1].Position) - 1) : 0;
        SpanQuery item = queryArray[i].CreatorMethod();
        if (slop > 0)
        {
          SpanNearQuery query3 = null;
          if (list.Count > 0)
          {
            query3 = new SpanNearQuery(list.ToArray(), 0, true);
          }
          if (query == null)
          {
            SpanQuery[] clauses = new SpanQuery[] { query3, item };
            query = new SpanNearQuery(clauses, slop, true);
          }
          else
          {
            query = (query3 != null) ? new SpanNearQuery(new SpanQuery[] { query, query3, item }, slop, true) : new SpanNearQuery(new SpanQuery[] { query, item }, slop, true);
          }
          list = new List<SpanQuery>();
        }
        else
        {
          list.Add(item);
        }
      }
      if ((query == null) && (list.Count > 0))
      {
        query = new SpanNearQuery(list.ToArray(), 0, true);
      }
      //patch start Sitecore.Support.169859
      else if ((query != null) && (list.Count > 0))
      {
        list.Insert(0, query);
        query = new SpanNearQuery(list.ToArray(), 0, true);
      }
      //patch end Sitecore.Support.169859
      return query;
    }

    private SpanQuery GetSpanQuery(string fieldName, IEnumerable<string> terms, bool isWildcard)
    {
      if (!isWildcard && (terms.Count<string>() <= 1))
      {
        return new SpanTermQuery(new Term(fieldName, terms.First<string>()));
      }
      return this.GetSpanWildcardQuery(fieldName, terms);
    }
    private SpanWildcardQuery GetSpanWildcardQuery(string fieldName, IEnumerable<string> terms) =>
            new SpanWildcardQuery(from z in terms select new Term(fieldName, z));

    protected override Query VisitContains(ContainsNode node, LuceneQueryMapperState mappingState)
    {
      Query query;
      FieldNode fieldNode = QueryHelper.GetFieldNode(node);
      ConstantNode valueNode = QueryHelper.GetValueNode<string>(node);
      string queryText = base.ValueFormatter.FormatValueForIndexStorage(valueNode.Value, fieldNode.FieldKey).ToString();
      Analyzer analyzer = this.GetAnalyzer(fieldNode.FieldKey);
      if ((queryText.Length > 0) && (queryText != "*"))
      {
        List<KeyValuePair<string, int>> terms = this.GetTermsWithPositions(fieldNode.FieldKey, queryText);
        mappingState.UsedAnalyzers.Add(new Tuple<string, ComparisonType, Analyzer>(fieldNode.FieldKey, ComparisonType.Contains, analyzer));
        if (terms.Count > 1)
        {
          IEnumerable<SpanSubQuery> source = from term in terms
                                             group term by term.Value into g
                                             select new SpanSubQuery
                                             {
                                               IsWildcard = (g.Key == terms.Last<KeyValuePair<string, int>>().Value) || (g.Key == terms.First<KeyValuePair<string, int>>().Value),
                                               Position = g.Key,
                                               CreatorMethod = delegate {
                                                 if (g.Key == terms.First<KeyValuePair<string, int>>().Value)
                                                 {
                                                   return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select "*" + pair.Key, true);
                                                 }
                                                 if (g.Key == terms.Last<KeyValuePair<string, int>>().Value)
                                                 {
                                                   return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select pair.Key + "*", true);
                                                 }
                                                 return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select pair.Key, false);
                                               }
                                             };
          query = this.BuildSpanQuery(source.ToArray<SpanSubQuery>());
        }
        else if (terms.Count == 1)
        {
          KeyValuePair<string, int> pair = terms[0];
          query = new SpanWildcardQuery(new Term(fieldNode.FieldKey, "*" + pair.Key + "*"));
        }
        else
        {
          query = new WildcardQuery(new Term(fieldNode.FieldKey, "*" + queryText.ToLowerInvariant() + "*"));
        }
      }
      else
      {
        query = new WildcardQuery(new Term(fieldNode.FieldKey, "*"));
      }
      query.Boost = node.Boost;
      return query;
    }

    protected override Query VisitEndsWith(EndsWithNode node, LuceneQueryMapperState mappingState)
    {
      Query query;
      FieldNode fieldNode = QueryHelper.GetFieldNode(node);
      ConstantNode valueNode = QueryHelper.GetValueNode<string>(node);
      string queryText = base.ValueFormatter.FormatValueForIndexStorage(valueNode.Value, fieldNode.FieldKey).ToString();
      Analyzer analyzer = this.GetAnalyzer(fieldNode.FieldKey);
      List<KeyValuePair<string, int>> terms = this.GetTermsWithPositions(fieldNode.FieldKey, queryText);
      mappingState.UsedAnalyzers.Add(new Tuple<string, ComparisonType, Analyzer>(fieldNode.FieldKey, ComparisonType.EndsWith, analyzer));
      if (terms.Count > 1)
      {
        IEnumerable<SpanSubQuery> source = from term in terms
                                           group term by term.Value into g
                                           select new SpanSubQuery
                                           {
                                             IsWildcard = g.Key == terms.First<KeyValuePair<string, int>>().Value,
                                             Position = g.Key,
                                             CreatorMethod = delegate {
                                               if (g.Key == terms.First<KeyValuePair<string, int>>().Value)
                                               {
                                                 return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select "*" + pair.Key, true);
                                               }
                                               if (g.Key == terms.Last<KeyValuePair<string, int>>().Value)
                                               {
                                                 return new SpanLastQuery(new SpanTermQuery(new Term(fieldNode.FieldKey, (from pair in g select pair.Key).First<string>())), analyzer);
                                               }
                                               return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select pair.Key, true);
                                             }
                                           };
        query = this.BuildSpanQuery(source.ToArray<SpanSubQuery>());
      }
      else if (terms.Count == 1)
      {
        KeyValuePair<string, int> pair = terms[0];
        query = new SpanLastQuery(new SpanWildcardQuery(new Term(fieldNode.FieldKey, "*" + pair.Key)), analyzer);
      }
      else
      {
        query = new MatchNoDocsQuery();
      }
      query.Boost = node.Boost;
      return query;
    }

    protected virtual Query VisitStartsWith(StartsWithNode node, LuceneQueryMapperState mappingState)
    {
      Query query;
      FieldNode fieldNode = QueryHelper.GetFieldNode(node);
      ConstantNode valueNode = QueryHelper.GetValueNode<string>(node);
      string queryText = base.ValueFormatter.FormatValueForIndexStorage(valueNode.Value, fieldNode.FieldKey).ToString();
      Analyzer analyzer = this.GetAnalyzer(fieldNode.FieldKey);
      List<KeyValuePair<string, int>> terms = this.GetTermsWithPositions(fieldNode.FieldKey, queryText);
      mappingState.UsedAnalyzers.Add(new Tuple<string, ComparisonType, Analyzer>(fieldNode.FieldKey, ComparisonType.StartsWith, analyzer));
      if (terms.Count > 1)
      {
        IEnumerable<SpanSubQuery> source = from term in terms
                                           group term by term.Value into g
                                           select new SpanSubQuery
                                           {
                                             IsWildcard = g.Key == terms.Last<KeyValuePair<string, int>>().Value,
                                             Position = g.Key,
                                             CreatorMethod = delegate {
                                               if (g.Key == terms.First<KeyValuePair<string, int>>().Value)
                                               {
                                                 return new SpanFirstQuery(new SpanTermQuery(new Term(fieldNode.FieldKey, (from pair in g select pair.Key).First<string>())), g.Key + 1);
                                               }
                                               if (g.Key == terms.Last<KeyValuePair<string, int>>().Value)
                                               {
                                                 return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select pair.Key + "*", true);
                                               }
                                               return this.GetSpanQuery(fieldNode.FieldKey, from pair in g select pair.Key, true);
                                             }
                                           };
        query = this.BuildSpanQuery(source.ToArray<SpanSubQuery>());
      }
      else if (terms.Count == 1)
      {
        KeyValuePair<string, int> pair = terms[0];
        pair = terms[0];
        query = new SpanFirstQuery(new SpanWildcardQuery(new Term(fieldNode.FieldKey, pair.Key + "*")), pair.Value + 1);
      }
      else
      {
        query = new SpanFirstQuery(new SpanWildcardQuery(new Term(fieldNode.FieldKey, queryText.ToLowerInvariant() + "*")), 1);
      }
      query.Boost = node.Boost;
      return query;
    }
  }
}
