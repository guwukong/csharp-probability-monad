using System;

namespace ProbCSharp
{
    public class InverseGammaPrimitive : PrimitiveDist<double>
    {
        public double shape;
        public double rate;
        public MathNet.Numerics.Distributions.InverseGamma dist;
        public InverseGammaPrimitive(double shape, double rate, Random gen)
        {
            dist = new MathNet.Numerics.Distributions.InverseGamma(shape, rate, gen);
        }
        public Func<double> Sample
        {
            get { return () => dist.Sample(); }
        }
    }
}
