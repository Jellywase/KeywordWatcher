using KeywordWatcher.DC;
using KeywordWatcher.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public static class KeywordClientFactory
    {
        public enum Type { dcUSStock, rdStocks}
        public async  static Task<KeywordClient> Create(Type clientType, HttpClient httpClient)
        {
            switch (clientType)
            {
                case Type.dcUSStock:
                    return new KeywordClient(new DCUSStockKC(httpClient), new(144), 600000); // 10min

                case Type.rdStocks:
                    return new KeywordClient(new RDStocksKC(await RedditApp.GetInstance()), new(144), 3600000); // 1hr

                default:
                    return null;
            }
        }
    }
}
