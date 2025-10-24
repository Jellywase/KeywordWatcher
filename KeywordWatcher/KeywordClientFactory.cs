using KeywordWatcher.DC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public static class KeywordClientFactory
    {
        public enum Type { dcUSStockGallery, }
        public static KeywordClient Create(Type clientType, HttpClient httpClient)
        {
            switch (clientType)
            {
                case Type.dcUSStockGallery:
                    return new KeywordClient(new DCUSStockKeywordCollector(httpClient), new(6), 60000);

                default:
                    return null;
            }
        }
    }
}
