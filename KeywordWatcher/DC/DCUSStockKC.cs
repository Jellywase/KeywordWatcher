using NetKiwi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher.DC
{
    internal class DCUSStockKC : DCKeywordCollector
    {
        protected override string boardCode => "stockus";
        public DCUSStockKC(HttpClient httpClient) : base(httpClient)
        { }
    }
}
