using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public class Tester
    {

        public async Task Test()
        {
            CollectedData cd0 = new CollectedData(DateTime.Now, "CD0", null);
            cd0.AddKeywordCount("A", 5);
            cd0.AddKeywordCount("B", 1);
            cd0.AddKeywordCount("C", 1);
            cd0.AddKeywordCount("D", 1);
            cd0.AddKeywordCount("E", 1);
            cd0.N+=5;
            cd0.N++;
            cd0.N++;
            cd0.N++;
            cd0.N++;

            CollectedData cd1 = cd0.DeepCopy() as CollectedData;
            cd1.AddKeywordCount("A", 25);
            cd1.N+=25;
            cd1.AddKeywordCount("F", 1);
            cd1.N++;

            KeywordAnalyzer ka = new KeywordAnalyzer(10);
            var result0 = await ka.AnalyzeData(cd0);
            var result1 = await ka.AnalyzeData(cd0);
            var result2 = await ka.AnalyzeData(cd0);
            var result3 = await ka.AnalyzeData(cd0);
            var result4 = await ka.AnalyzeData(cd0);
            var result5 = await ka.AnalyzeData(cd0);
            var result6 = await ka.AnalyzeData(cd0);
            var result7 = await ka.AnalyzeData(cd0);
            var result8 = await ka.AnalyzeData(cd0);
            var result9 = await ka.AnalyzeData(cd1);
        }
    }
}
