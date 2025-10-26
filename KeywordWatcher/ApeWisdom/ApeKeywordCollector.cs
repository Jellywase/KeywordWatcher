using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher.ApeWisdom
{
    internal class ApeKeywordCollector : KeywordCollector
    {
        public override Task<CollectResult> CollectData(CancellationToken ct, int collectPeriod)
        {
            throw new NotImplementedException();
        }
    }
}
