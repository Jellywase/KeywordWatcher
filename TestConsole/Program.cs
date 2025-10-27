
using KeywordWatcher;
using NetKiwi;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using KeywordWatcher.DC;

public class Program
{
    public static async Task Main(string[] args)
    {

        //Tester tester = new();
        //await tester.Test();
        //return;


        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "E");

        var kc = KeywordClientFactory.Create(KeywordClientFactory.Type.dcUSStockGallery, httpClient);


        CancellationTokenSource cts = new();
        IProgress<KeywordClient.LoopResult> handler = new Progress<KeywordClient.LoopResult>((lr) =>
        {
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine(DateTime.Now.ToString());
            if (lr == null || !lr.isSuccessful)
            {
                Console.WriteLine("Client loop failed");
                if (lr != null)
                {
                    foreach (var ex in lr.exceptions)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                return;
            }

            var ad = lr.analyzedData;
            int cnt = 0;
            var hot = ad.analyzedKeywords.OrderByDescending((kvp) => kvp.Value.score).Select((kvp) => kvp.Value).ToArray();
            foreach (var ak in hot)
            {
                Console.WriteLine($"{ak.keyword} : score - {ak.score} , avgR - {ak.avgR} , avgF - {ak.avgF} , F - {ak.frontF}");
                cnt++;
                if (cnt == 10)
                { break; }
            }
            Console.WriteLine("--------------------------------------------------------------");
        });


        _ = Task.Run(() => kc.WatchLoop(cts.Token, handler));


        while (true)
        { }
    }
}