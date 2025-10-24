using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal class AnalyzedKeyword : IReadOnlyAnalyzedKeyword
    {
        [JsonInclude]
        public string keyword { get; }
        [JsonInclude]
        public float score { get; set; }
        [JsonInclude]
        public int cumulative { get; set; }
        [JsonInclude]
        public int totalF { get; set; }
        [JsonInclude]
        public float totalR { get; set; }
        [JsonInclude]
        public int totalSqrF { get; set; }
        [JsonInclude]
        public float totalSqrR { get; set; }
        [JsonInclude]
        public float avgF { get; set; }
        [JsonInclude]
        public float avgR { get; set; }
        [JsonInclude]
        public float varF { get; set; }
        [JsonInclude]
        public float varR { get; set; }
        [JsonInclude]
        public float stdDevF { get; set; }
        [JsonInclude]
        public float stdDevR { get; set; }

        public AnalyzedKeyword(string keyword)
        {
            this.keyword = keyword;
        }
    }

    public interface IReadOnlyAnalyzedKeyword
    {
        public string keyword { get; }
        public float score { get; }
        public int cumulative { get; }
        public int totalF { get; }
        public float avgF { get; }
        public float avgR { get; }
        public float stdDevF { get; }
    }
}
