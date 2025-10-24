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

        public int cumulative { get; }

        public IEnumerable<IReadOnlyAnalyzedKeyword> hotKeywords => _hotKeywords;
        public List<IReadOnlyAnalyzedKeyword> _hotKeywords;

        public AnalyzedData(string name, DateTime frontCDTime, int cumulative)
        {
            this.name = name;
            this.frontCDTime = frontCDTime;
            this.cumulative = cumulative;
            _hotKeywords = new List<IReadOnlyAnalyzedKeyword>();
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
