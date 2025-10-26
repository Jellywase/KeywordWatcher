using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal class CollectedData : IReadOnlyCollectedData
    {
        [JsonInclude]
        public DateTime time { get; }
        [JsonInclude]
        public string name { get; }
        [JsonInclude]
        public string description { get; }
        [JsonInclude]
        public int N { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyKeywordData> keywords => keywordsInternal;
        Dictionary<string, IReadOnlyKeywordData> keywordsInternal;

        public CollectedData(DateTime time, string name, string description)
        {
            this.time = time;
            this.name = name;
            this.description = description;
            keywordsInternal = new Dictionary<string, IReadOnlyKeywordData>();
        }

        public IReadOnlyCollectedData DeepCopy()
        {
            var newData = new CollectedData(time, name, description);
            newData.N = N;
            foreach (var kvp in keywords)
            {
                var keywordData = kvp.Value;
                newData.keywordsInternal.Add(kvp.Key, new KeywordData(keywordData.keyword, keywordData.frequency));
            }
            return newData;
        }

        public void AddKeywordCount(string keyword, int count)
        {
            KeywordData keywordData;
            if (!keywordsInternal.ContainsKey(keyword))
            {
                keywordsInternal[keyword] = new KeywordData(keyword, 0);
            }
            keywordData = (KeywordData)keywordsInternal[keyword];
            keywordData.frequency += count;
        }

        public float GetRatio(string keyword)
        {
            if (N == 0)
            { return 0f; }
            if (keywordsInternal.TryGetValue(keyword, out var kd))
            { return (float)kd.frequency / N; }
            return 0f;
        }

        //public void Compare(IReadOnlyCollectedData compare)
        //{
        //    this.compared = compare;
        //    foreach (var kvp in keywords)
        //    {
        //        var keyword = kvp.Key;
        //        var keywordData = (KeywordData)kvp.Value;
        //        if (compare.keywords.ContainsKey(keyword))
        //        {
        //            var compareData = compare.keywords[keyword];
        //            keywordData.divergence = keywordData.frequency - compareData.frequency;
        //        }
        //        else
        //        {
        //            keywordData.divergence = keywordData.frequency;
        //        }
        //    }

        //    foreach (var kvp in compare.keywords)
        //    {
        //        var keyword = kvp.Key;
        //        var keywordData = (KeywordData)kvp.Value;
        //        if (keywordData.frequency == 0)
        //        { continue; }
        //        if (!keywordsInternal.ContainsKey(keyword))
        //        {
        //            var compareData = kvp.Value;
        //            keywordsInternal[keyword] = new KeywordData(keyword, 0, -compareData.frequency);
        //        }
        //    }
        //}
    }

    public interface IReadOnlyCollectedData
    {
        public DateTime time { get; }
        public string name { get; }
        public string description { get; }
        public int N { get; }
        public IReadOnlyDictionary<string, IReadOnlyKeywordData> keywords { get; }
        public IReadOnlyCollectedData DeepCopy();
        public float GetRatio(string keyword);
    }
}
