using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher.Reddit
{
    internal class RDStocksKC : RedditKeywordCollector
    {
        protected override string subreddit => "stocks";
        public RDStocksKC(RedditApp redditApp) : base(redditApp)
        {
        }
    }
}
