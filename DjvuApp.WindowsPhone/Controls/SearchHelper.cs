using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public static class SearchHelper
    {
        private static IEnumerable<TextLayerZone> GetWordsLinear(IEnumerable<TextLayerZone> zones)
        {
            foreach (var zone in zones)
            {
                if (zone.Type == ZoneType.Word)
                {
                    yield return zone;
                }
                else
                {
                    foreach (var childZone in GetWordsLinear(zone.Children))
                    {
                        yield return childZone;
                    }
                }
            }
        }

        private static int[] IndicesOfSubstring(string source, string pattern)
        {
            var result = new List<int>();

            var wordIndex = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == ' ')
                {
                    wordIndex++;
                    continue;
                }

                if (source.Length < i + pattern.Length)
                {
                    break;
                }

                if (string.Compare(source.Substring(i, pattern.Length), pattern, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    result.Add(wordIndex);
                }
            }

            return result.ToArray();
        }

        public static IReadOnlyCollection<IReadOnlyCollection<TextLayerZone>> Search(IEnumerable<TextLayerZone> zones, string query)
        {
            var queryWords = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var wordsZones = GetWordsLinear(zones).ToArray();

            var pageText = string.Join(" ", wordsZones.Select(zone => zone.Text));
            var indices = IndicesOfSubstring(pageText, query);

            return indices.Select(index => wordsZones.Skip(index).Take(queryWords.Length).ToArray()).ToArray();
        }
    }
}
