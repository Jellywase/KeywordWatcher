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
        public IReadOnlyCollectedData frontCD { get; }
        public int cumulative { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyAnalyzedKeyword> analyzedKeywords => analyzedKeywords_Internal;
        Dictionary<string, IReadOnlyAnalyzedKeyword> analyzedKeywords_Internal { get; }

        public AnalyzedData(string name, IReadOnlyCollectedData frontCD, int cumulated)
        {
            this.name = name;
            this.frontCD = frontCD;
            this.cumulative = cumulated;
            analyzedKeywords_Internal = new();
        }

        public void AddAnalyzedKeyword(IReadOnlyAnalyzedKeyword ak)
        {
            analyzedKeywords_Internal[ak.keyword] = ak;
        }
    }

    public interface IReadOnlyAnalyzedData
    {
        public string name { get; }
        public IReadOnlyCollectedData frontCD { get; }
        public int cumulative { get; }
        public IReadOnlyDictionary<string, IReadOnlyAnalyzedKeyword> analyzedKeywords { get; }
    }
}
