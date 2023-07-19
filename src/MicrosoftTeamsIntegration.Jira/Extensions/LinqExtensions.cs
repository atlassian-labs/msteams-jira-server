using System;
using System.Collections.Generic;

namespace MicrosoftTeamsIntegration.Jira.Extensions
{
    public static class LinqExtensions
    {
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(selector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
