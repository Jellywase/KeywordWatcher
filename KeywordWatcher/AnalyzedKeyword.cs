using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal class AnalyzedKeyword : IReadOnlyAnalyzedKeyword
    {
        public string keyword => throw new NotImplementedException();

        public float avgFreq => throw new NotImplementedException();

        public float stdDev => throw new NotImplementedException();
    }

    public interface IReadOnlyAnalyzedKeyword
    {
        public string keyword { get; }
        public float avgFreq { get; }
        public float stdDev { get; }
    }
}
