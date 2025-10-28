using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public class Tester
    {
        Stopwatch sw = new();
        public async Task Test()
        {
            CollectedData cd0 = new CollectedData(DateTime.Now, "CD0", null);
            cd0.AddKeywordCount("A", 5);
            cd0.AddKeywordCount("B", 1);
            cd0.AddKeywordCount("C", 1);
            cd0.AddKeywordCount("D", 1);
            cd0.AddKeywordCount("E", 1);
            //cd0.N+=5;
            //cd0.N++;
            //cd0.N++;
            //cd0.N++;
            //cd0.N++;

            CollectedData cd1 = cd0.DeepCopy() as CollectedData;
            cd1.AddKeywordCount("A", 25);
            //cd1.N += 25;

            cd1.AddKeywordCount("B", 5);
            //cd1.N += 5;

            cd1.AddKeywordCount("C", 6);
            //cd1.N += 6;

            cd1.AddKeywordCount("D", 7);
            //cd1.N += 7;

            cd1.AddKeywordCount("F", 1);
            //cd1.N++;

            cd1.AddKeywordCount("G", 5);
            //cd1.N += 5;

            cd1.N = 1000; // 이게 맞아.

            KeywordAnalyzer ka = new KeywordAnalyzer(144);

            sw.Start();
            for (int i = 0; i < 144; i++)
            {
                await ka.AnalyzeData(cd0);
            }
            var result9 = await ka.AnalyzeData(cd1);

            sw.Stop();

            Console.WriteLine($"Elapsed Time: {sw.ElapsedMilliseconds / 1000f}s");
            Console.WriteLine();
            foreach (var kvp in result9.ad.analyzedKeywords)
            {
                var keyword = kvp.Key;
                var ak = kvp.Value;
                Console.WriteLine( $"{keyword} {ak.score}");
            }
        }
    }
}
