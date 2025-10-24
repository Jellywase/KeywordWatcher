using KeywordWatcher.DC;
using NetKiwi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public sealed class KeywordClient
    {
        readonly int interval;
        KeywordCollector collector { get; }
        KeywordAnalyzer analyzer { get; }
        int loopID = 0;

        internal KeywordClient(KeywordCollector collector, KeywordAnalyzer analyzer, int watchInterval)
        {
            this.collector = collector;
            this.analyzer = analyzer;
            this.interval = watchInterval;
        }


        public async Task WatchLoop(CancellationToken ct, IProgress<LoopResult> loopHandler)
        {
            loopID = 0;
            while (!ct.IsCancellationRequested)
            {
                LoopResult result = new();
                List<Exception> exceptions = new();
                result.exceptions = exceptions;
                try
                { 
                    var collectResult = await collector.CollectData();
                    exceptions.AddRange(collectResult.exceptions);
                    if (!collectResult.isSuccessful || collectResult.cd == null)
                    { throw new Exception("Keyword Collecting process failed."); }

                    var analyzeResult = await analyzer.AnalyzeData(collectResult.cd);
                    exceptions.AddRange(analyzeResult.exceptions);
                    if (!analyzeResult.isSuccessful || analyzeResult.ad == null)
                    { throw new Exception("Keyword Analyzing process failed."); }

                    result.analyzedData = analyzeResult.ad;
                    result.isSuccessful = true;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    result.isSuccessful = false;
                }
                finally
                {
                    result.loopID = ++loopID;
                    loopHandler?.Report(result);
                    await Task.Delay(interval, ct);
                }
            }
        }

        public class LoopResult
        {
            public IReadOnlyAnalyzedData analyzedData;
            public IReadOnlyList<Exception> exceptions;
            public bool isSuccessful;
            public int loopID;
        }
    }
}
