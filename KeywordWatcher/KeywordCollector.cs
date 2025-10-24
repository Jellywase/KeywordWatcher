using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal abstract class KeywordCollector
    {
        public abstract Task<CollectResult> CollectData(CancellationToken ct, int collectPeriod);


        internal class CollectResult
        {
            public IReadOnlyCollectedData cd;
            public IReadOnlyList<Exception> exceptions;
            public bool isSuccessful;
        }
    }
}
