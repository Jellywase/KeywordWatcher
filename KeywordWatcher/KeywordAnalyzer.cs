using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal class KeywordAnalyzer
    {
        IReadOnlyCollectedData[] cdSequence = null;
        object lockObj = new object();
        public int cumulative
        {
            get 
            {
                lock (lockObj)
                {
                    return _cumulative;
                }
            }
            set 
            {
                lock (lockObj)
                {
                    _cumulative = value;
                    ResizeCDSequence();
                }
            }
        }
        int _cumulative;

        public KeywordAnalyzer(int cumulative)
        {
            this.cumulative = cumulative;
        }
        public async Task<AnalyzeResult> AnalyzeData(IReadOnlyCollectedData cd)
        {
            AnalyzeResult result = new AnalyzeResult();
            List<Exception> exceptions = new();
            result.exceptions = exceptions;

            try
            {
                UpdateCDSequence(cd);
                result.ad = await AnalyzeCumulativeData();
                result.isSuccessful = true;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                result.isSuccessful = false;
            }
            return result;
        }

        void ResizeCDSequence()
        {
            lock (lockObj)
            {
                if (cdSequence == null)
                {
                    cdSequence = new IReadOnlyCollectedData[cumulative];
                    return;
                }
                if (cdSequence.Length != cumulative)
                {
                    var newSequence = new IReadOnlyCollectedData[cumulative];
                    for (int i = 0; i < Math.Min(cdSequence.Length, newSequence.Length); i++)
                    {
                        newSequence[i] = cdSequence[i];
                    }
                    cdSequence = newSequence;
                }
            }
        }

        void UpdateCDSequence(IReadOnlyCollectedData cd)
        {
            lock (lockObj)
            {
                for (int i = cdSequence.Length - 1; i > 0; i--)
                {
                    cdSequence[i] = cdSequence[i - 1];
                }
                cdSequence[0] = cd;
            }
        }
        Task<IReadOnlyAnalyzedData> AnalyzeCumulativeData()
        {
            return null;
        }

        public class AnalyzeResult
        {
            [JsonInclude]
            public IReadOnlyAnalyzedData ad;
            [JsonInclude]
            public IReadOnlyList<Exception> exceptions;
            [JsonInclude]
            public long loopID = 0;
            [JsonInclude]
            public bool isSuccessful;
        }
    }
}
