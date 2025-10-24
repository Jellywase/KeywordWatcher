using NetKiwi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher.DC
{
    internal class DCUSStockKeywordCollector : DCKeywordCollector
    {
        protected override string boardCode => "stockus";
        public DCUSStockKeywordCollector(HttpClient httpClient, SharpKiwi kiwi) : base(httpClient, kiwi)
        { }
    }
}
