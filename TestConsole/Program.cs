
using KeywordWatcher;
using NetKiwi;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine(MathF.Pow(3, 3));
        while (true)
        { }
    }

    class LoopResult
    {
        [JsonInclude]
        public IReadOnlyCollectedData cd;
        [JsonInclude]
        public long loopID = 0;
        [JsonInclude]
        public bool isSuccessful;
        [JsonInclude]
        public List<string> exceptions = new List<string>();
    }
}