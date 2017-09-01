using System;

namespace ProbCSharp
{
    public class InverseGamma : PrimitiveDist<double>
    {
        public double shape;
        public double rate;
        public MathNet.Numerics.Distributions.InverseGamma dist;
        public InverseGamma(double shape, double rate, Random gen)
        {
            dist = new MathNet.Numerics.Distributions.InverseGamma(shape, rate, gen);
        }
        public Func<double> Sample
        {
            get { return () => dist.Sample(); }
        }
    }
}
