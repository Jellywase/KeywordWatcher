using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal class AnalyzedData : IReadOnlyAnalyzedData
    {
        public string name { get; }
        public DateTime frontCDTime { get; }
        public int cumulative { get; set; }
        public IEnumerable<IReadOnlyAnalyzedKeyword> hotKeywords => analyzedKeywords.OrderByDescending((kvp) => kvp.Value.score).Select((kvp) => kvp.Value).ToArray();
        Dictionary<string, IReadOnlyAnalyzedKeyword> analyzedKeywords { get; }

        public AnalyzedData(string name, DateTime frontCDTime, int cumulated)
        {
            this.name = name;
            this.frontCDTime = frontCDTime;
            this.cumulative = cumulated;
            analyzedKeywords = new();
        }

        public void AddAnalyzedKeyword(IReadOnlyAnalyzedKeyword ak)
        {
            analyzedKeywords[ak.keyword] = ak;
        }
    }

    public interface IReadOnlyAnalyzedData
    {
        public string name { get; }
        public DateTime frontCDTime { get; }
        public int cumulative { get; }
        public IEnumerable<IReadOnlyAnalyzedKeyword> hotKeywords { get; }
    }
}
