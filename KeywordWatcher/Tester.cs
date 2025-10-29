using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public static class Tester
    {
        public static async Task Test()
        {
            CollectedData cd0 = new CollectedData(DateTime.Now, "CD0", null);
            cd0.N = 150;
            for (int i = 0; i < 150; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    cd0.AddKeywordCount(j.ToString(), 1);
                }
            }
            KeywordAnalyzer ka = new KeywordAnalyzer(144);
            for (int i = 0; i < 144; i++)
            {
                await ka.AnalyzeData(cd0);
            }
        }

        internal static IStopwatch CreateStopwatch()
        { return new Tester.Stopwatch(); }

        internal interface IStopwatch
        {
            public void Start();
            public float Stop();
            public float Stop(Action<float> action);
            public void Reset();
        }

        internal class Stopwatch : IStopwatch
        {
            System.Diagnostics.Stopwatch sw = new();
            public void Start()
            {
                sw.Start();
            }

            public float Stop()
            {
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }

            public float Stop(Action<float> action)
            {
                sw.Stop();
                action?.Invoke(sw.ElapsedMilliseconds);
                return sw.ElapsedMilliseconds;
            }
            public void Reset()
            {  sw.Reset(); }
        }
        internal class VoidStopwatch : IStopwatch
        {
            public void Start()
            { }

            public float Stop()
            { return -1; }

            public float Stop(Action<float> action)
            { return -1; }
            public void Reset()
            { }
        }
    }
}
