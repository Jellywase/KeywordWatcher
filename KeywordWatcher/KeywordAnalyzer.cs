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
        IReadOnlyAnalyzedData lastAD;
        const int leastCumulative = 2;
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
                    _cumulative = int.Clamp(value, leastCumulative, int.MaxValue);
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

                var sw = Tester.CreateStopwatch();
                sw.Start();
                result.ad = await AnalyzeCumulativeData();
                sw.Stop((ms) => Console.WriteLine($"Analyze Cumulative - {ms}ms"));
                sw.Reset();

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
                if (cdSequence is null || cdSequence.Length < leastCumulative)
                { throw new InvalidOperationException("CD sequence is invalid"); }

                for (int i = cdSequence.Length - 1; i > 0; i--)
                {
                    cdSequence[i] = cdSequence[i - 1];
                }
                cdSequence[0] = cd;
            }
        }
        async Task<IReadOnlyAnalyzedData> AnalyzeCumulativeData()
        {
            lock (lockObj)
            {
                if (cdSequence is null || cdSequence.Length < leastCumulative)
                { throw new InvalidOperationException("CD sequence is invalid"); }

                // 첫번째 CD 캐싱.
                var frontCD = cdSequence[0];
                if (frontCD == null)
                { throw new InvalidOperationException("Sequence need at least 1 CollectedData"); }

                // 두번째 CD 캐싱.
                var secondCD = cdSequence[1];
                if (secondCD == null)
                    // 두번째 CD가 없는 경우 첫번째 CD의 복사본 할당
                { 
                    cdSequence[1] = secondCD = frontCD.DeepCopy();
                }

                // 목표 cumulative보다 축적된 cumulative가 적은 경우 대응하여 actualCumulative 로 대체.
                int actualCumulative = leastCumulative;
                while (actualCumulative < cumulative && cdSequence[actualCumulative] != null)
                {
                    actualCumulative++;
                }

                AnalyzedData ad = new(frontCD.name, frontCD, actualCumulative);

                // AnalyzedKeyword 인스턴스를 만들고 키워드 빈도를 누적 집계
                for (int i = 0; i < actualCumulative; i++)
                {
                    var cd = cdSequence[i];

                    foreach (var kvp in cd.keywords)
                    {
                        string keyword = kvp.Key;
                        var kd = kvp.Value;

                        AnalyzedKeyword ak;
                        if (!ad.analyzedKeywords.TryGetValue(keyword, out var rak))
                        {
                            rak = ak = new AnalyzedKeyword(keyword);
                            ad.AddAnalyzedKeyword(ak);
                        }
                        ak = (AnalyzedKeyword)rak;
                        ak.totalF += kd.frequency;
                        ak.totalSqrF += (int)Sqr(kd.frequency);
                        var r = cd.GetRatio(keyword);
                        ak.totalR += r;
                        ak.totalSqrR += Sqr(r);

                        // 선두 빈도 저장
                        if (i == 0)
                        {
                            ak.frontF = kd.frequency;
                            ak.frontR = cd.GetRatio(keyword);
                        }
                    }
                }

                // 키워드 집계로부터 통계산출
                foreach (var kvp in ad.analyzedKeywords)
                {
                    var keyword = kvp.Key;
                    var ak = (AnalyzedKeyword)kvp.Value;

                    // 누계 마킹
                    ak.cumulative = actualCumulative;

                    // 키워드 평균빈도
                    ak.avgF = (float)ak.totalF / actualCumulative;
                    ak.avgR = (float)ak.totalR / actualCumulative;

                    // 키워드 분산
                    ak.varF = ((float)ak.totalSqrF / actualCumulative) - Sqr(ak.avgF);
                    ak.varR = (ak.totalSqrR / actualCumulative) - Sqr(ak.avgR);

                    // 키워드 표준편차
                    ak.stdDevF = MathF.Sqrt(ak.varF);
                    ak.stdDevR = MathF.Sqrt(ak.varR);

                    // 빈도 비율
                    float r = frontCD.GetRatio(keyword);
                    float r_1 = secondCD.GetRatio(keyword);

                    // EMAR, EMVR
                    float lastEMAR, lastEMVR;
                    if (lastAD == null || !lastAD.analyzedKeywords.TryGetValue(keyword, out var lrak) && lrak is not AnalyzedKeyword)
                    {
                        lastEMAR = ak.avgR;
                        lastEMVR = ak.varR; 
                    }
                    else
                    {
                        var lak = (AnalyzedKeyword)lrak;
                        lastEMAR = lak.emaR;
                        lastEMVR = lak.emvR;
                    }
                    float lambda = 2f / (cumulative + 1f);
                    ak.emaR = GetEMAR(lambda, lastEMAR, r);
                    ak.emvR = GetEMVR(lambda, lastEMVR, lastEMAR, r);

                    // 점수 산출
                    //ak.score = Scoring1(0.5f, 0.5f, r, r_1, ak.avgR, ak.stdDevR);
                    ak.score = Scoring2(0.5f, 0.5f, r, r_1, ak.emaR, ak.emvR);

                    ad.AddAnalyzedKeyword(ak);
                }
                lastAD = ad;
                return ad;
            }
        }
        float Scoring1(float alpha, float beta, float r, float r_1, float avgR, float stdDevR)
        {

            float x = (r - avgR) / MathF.Sqrt(stdDevR);
            float y = (r - r_1) / MathF.Sqrt((r + r_1) / 2);
            float result = alpha * x + beta * y;
            return result;
        }
        float Scoring2(float alpha, float beta, float r, float r_1, float emaR, float emvR)
        {
            float emsdR = MathF.Sqrt(emvR);
            float x = emsdR > 0f ? (r - emaR) / MathF.Sqrt(emsdR) : 0;
            float y = (r - r_1) / MathF.Sqrt((r + r_1) / 2);
            float result = alpha * x + beta * y;
            return result;
        }

        float Scoring4(float alpha, float beta, float r, float r_1, float avgR, float stdDevR)
        {
            float epsilon = 1e-6f;
            float result = (1 - MathF.Pow(r - 1, 4)) * (alpha * (r - avgR) / stdDevR + beta * MathF.Log(1 + (r - r_1) / (r_1 + epsilon)));
            return result;
        }

        float Sqr(float x)
        { return x * x; }

        float GetEMAR(float lambda, float lastEMAR, float r)
        {
            return lambda * r + (1 - lambda) * lastEMAR;
        }
        float GetEMVR(float lambda, float lastEMVR, float lastEMAR, float r)
        {
            return (1 - lambda) * (lastEMVR + lambda * MathF.Pow(r - lastEMAR, 2));
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
