using System;
using MathNet.Numerics.Distributions;

namespace ProbCSharp
{
    /// <summary>
    /// Primitive Normal distribution
    /// </summary>
    public class NormalPrimitive : PrimitiveDist<double>
    {
        public double Mean { get; }
        public double Variance { get; }
        public Random Gen {get;}
        public Normal normal { get; }
        public NormalPrimitive(double mean, double variance, Random gen)
        {
            Mean = mean;
            Variance = variance;
            Gen = gen;
        }

        public Func<double> Sample
        {
            get
            {
                return () => MathNet.Numerics.Distributions.Normal
                        .WithMeanVariance(Mean, Variance, Gen)
                        .Sample();
            }
        }
    }

    public class NormalPrimitiveWithPrecision : PrimitiveDist<double>
    {
        public double Mean { get; }
        public double Precision { get; }
        public Random Gen { get; }
        public Normal normal { get; }
        public NormalPrimitiveWithPrecision(double mean, double precision, Random gen)
        {
            Mean = mean;
            Precision = precision;
            Gen = gen;
        }

        public Func<double> Sample
        {
            get
            {
                return () => Normal
                        .WithMeanPrecision(Mean, Precision, Gen)
                        .Sample();
            }
        }
    }
}
