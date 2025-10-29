using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal static class Utility
    {
        public class APIUsage
        {
            List<DateTime> history = new();
            object lockObj = new();
            readonly int limitPerMin;
            public APIUsage(int limitPerMin)
            {
                this.limitPerMin = limitPerMin;
            }
            public int WhatMSShouldIWait()
            {
                lock (lockObj)
                {
                    var now = DateTime.UtcNow;
                    TrimHistory(now);
                    if (history.Count < limitPerMin)
                    { return 0; }
                    else
                    {
                        // boundary index부터 끝까지 요소 = limitPerMin
                        int boundaryIndex = history.Count - limitPerMin;
                        DateTime boundaryDate = history[boundaryIndex];
                        DateTime oneMinuteAgo = now.AddMinutes(-1);
                        return (int)(boundaryDate - oneMinuteAgo).TotalMilliseconds + 1;
                    }
                }
            }
            public void RecordUse()
            {
                lock (lockObj)
                {
                    var now = DateTime.Now;
                    TrimHistory(now);
                    history.Add(now);
                }
            }
            /// <summary>
            /// 현재기준 1분 안의 Date만 남김.
            /// </summary>
            void TrimHistory(DateTime now)
            {
                var oneMinuteAgo = now.AddMinutes(-1);
                while (history.Count > 0 && history[0] < oneMinuteAgo)
                {
                    history.RemoveAt(0);
                }
            }
        }
    }
}
