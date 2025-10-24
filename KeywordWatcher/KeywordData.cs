using System.Text.Json.Serialization;

namespace KeywordWatcher
{
    internal class KeywordData : IReadOnlyKeywordData
    {
        [JsonInclude]
        public string keyword { get; set; }
        [JsonInclude]
        public int frequency { get; set; }
        public KeywordData(string keyword, int frequency)
        {
            this.keyword = keyword;
            this.frequency = frequency;
        }
    }
    public interface IReadOnlyKeywordData
    {
        string keyword { get; }
        int frequency { get; }
    }
}
