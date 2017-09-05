using System;
using System.Collections.Generic;
using System.Linq;

namespace ProbCSharp
{
    public class Hdi
    {
        public double Max { get; set; }
        public double Min { get; set; }
    }
    public class HighestDensityInterval
    {
        public static Hdi FindUnivariateHdi(IEnumerable<ItemProb<double>> samples, double credibleInterval = 0.95)
        {
            var hdi= new Hdi();
            var sorted = samples.OrderBy(x => x.Item).Select(x => x.Item).ToArray();
            int n = sorted.ToArray().Length;
            var intervalIdxInc = (int) Math.Ceiling(credibleInterval*n);
            var nIntervals = n - intervalIdxInc;
            var first = sorted.Skip(intervalIdxInc);
            var last = sorted.Take(nIntervals);
            var intervalWidth = first.Zip(last, (f, l) => f - l).ToArray();

            var minIdx = Array.IndexOf(intervalWidth, intervalWidth.Min());
            hdi.Min = sorted[minIdx];
            hdi.Max = sorted[minIdx + intervalIdxInc];
            return hdi;
        }
    }
}
