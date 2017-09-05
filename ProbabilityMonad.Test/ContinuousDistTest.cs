using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using static ProbCSharp.ProbBase;
using static ProbCSharp.Test.Models.IndianGpaModel;
using ProbCSharp.Test.Models;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using static ProbCSharp.Test.Models.ObservationExt;

namespace ProbCSharp.Test
{
    [TestClass]
    public class ContinuousDistTest
    {
        [TestMethod]
        public void NormalTest()
        {

            var hist =
                from m in Normal(0.0, 1.0)
                select m;
            var samples = hist.SampleN(1000);
            Debug.WriteLine(Histogram.Unweighted(samples));
            //Debug.WriteLine(Histogram.Unweighted(samples));

        }

        [TestMethod]
        public void LearnGaussian()
        {
            double[] data = new double[100];
            var r = new Normal(0.0, 1.0);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = r.Sample();
            }

            var priors = from mean in Normal(0.0, 100.0)
                from variance in InverseGamma(1.0, 1.0)
                select new Param(mean, variance);

            IEnumerable<ItemProb<double>> posteriorA = null;
            IEnumerable<ItemProb<double>> posteriorB = null;

            foreach (var d in data)
            {
                var p = Observation.UpdateWithObservation(priors, d);

                var ss = p.WeightedPrior().SampleN(500);
                posteriorA = ss.Select(sample => ItemProb(sample.Item.a, sample.Prob));
                posteriorB = ss.Select(sample => ItemProb(sample.Item.b, sample.Prob));


            }

            var hdiPA = HighestDensityInterval.FindUnivariateHdi(posteriorA, 0.5);
            var hdiPB = HighestDensityInterval.FindUnivariateHdi(posteriorB, 0.5);


            Debug.WriteLine("Posterior distribution of a:");
            Debug.WriteLine(Histogram.Weighted(posteriorA));
            Debug.WriteLine("Posterior distribution of b:");
            Debug.WriteLine(Histogram.Weighted(posteriorB));

            Debug.WriteLine($"PA HDI Min: {hdiPA.Min} Max: {hdiPA.Max}");
            Debug.WriteLine($"PB HDI Min: {hdiPB.Min} Max: {hdiPB.Max}");
        }

        [TestMethod]
        public void UniformTest()
        {

            var hist =
                from m in Uniform(0.0, 10.0)
                select m;
            var samples = hist.SampleN(100);
            Debug.WriteLine(Histogram.Unweighted(samples));
        }

        [TestMethod]
        public void StudentTTest()
        {
            var y = new Normal(10.0, 1.0).Samples().Take(1000).ToArray();

            var muM = y.Mean();
            var muP = 0.000001*1/(Math.Pow(y.StandardDeviation(), 2.0));
            var muv = (y.Variance())/0.00001;
            var sigmaLow = y.Variance()/1000.0;
            var sigmaHigh = y.Variance()*1000.0;


            var hist = from nu in Exponential(1.0/29)
                       from mu in Normal(muM, muv)
                       from tau in (from m in Uniform(sigmaLow, sigmaHigh)
                                    select 1 / Math.Pow(m, 2.0))
                       from n in StudentT(mu, tau, nu + 1)
                       select n;
            var smcPosterior = hist.SmcStandard(10000);
            var samples = smcPosterior.Sample();


            Debug.WriteLine(Histogram.Weighted(samples));
        }

        [TestMethod]
        public void ETest()
        {
            var hist = new Exponential(1.0/29).Samples().Take(1000);

            Debug.WriteLine(Histogram.Unweighted(hist));
        }
        [TestMethod]
        public void LinReg()
        {
            // Define a prior with heavy tails
            var prior = from a in Normal(5, 10)
                        from b in Normal(1, 10)
                        select new Param(a, b);

            // Create the linear regression, but don't do any inference yet
            var linReg = LinearRegression.CreateLinearRegression(prior, LinearRegression.LinearData);

            // Basically do importance sampling using the prior
            var samples = linReg.WeightedPrior().SampleNParallel(100);

            var posteriorA = samples.Select(sample => ItemProb(sample.Item.a, sample.Prob));
            var posteriorB = samples.Select(sample => ItemProb(sample.Item.b, sample.Prob));

            // Graph the results

            Debug.WriteLine("Posterior distribution of a:");
            Debug.WriteLine(Histogram.Weighted(posteriorA, 20, 2));

            Debug.WriteLine("Posterior distribution of b:");
            Debug.WriteLine(Histogram.Weighted(posteriorB, 20, 2));
        }

        [TestMethod]
        public void ThreeRolls()
        {
            var die = UniformF(1, 2, 3, 4, 5, 6);
            var threeRolls = from roll1 in die
                             from roll2 in die
                             from roll3 in die
                             select new List<int>() { roll1, roll2, roll3 };
            var samp3Roll = threeRolls.ToSampleDist();
            var sample = samp3Roll.Sample();
        }


        [TestMethod]
        public void IndianGpaTest()
        {

            var indiaSamples = IndiaGpa.SampleN(10000);
            Debug.WriteLine(Histogram.Unweighted(indiaSamples.Select(g => g.GPA), numBuckets: 30, scale: 400));

            var combined = from isAmerican in Bernoulli(0.25)
                           from gpa in isAmerican ? UsaGpa : IndiaGpa
                           select gpa;
            var combinedSamples = combined.SampleN(10000);

            Func<Grade, Prob> PrIs4 = gpa => Pdf(NormalPrimitive(4, 0.00001), gpa.GPA);

            Debug.WriteLine(Histogram.Unweighted(combinedSamples.Select(g => g.GPA), numBuckets: 30, scale: 400));

            var countryGiven4Gpa = from gpa in Condition(PrIs4, combined)
                                   select gpa.Country;

            var whichCountry = countryGiven4Gpa.SmcMultiple(1000, 100).Sample();

            Debug.WriteLine(Histogram.Finite(whichCountry));

            var hardConditioned = from grade in combined.Condition(g => g.GPA == 4.0 ? Prob(1.0) : Prob(0.0))
                                  select grade.Country;

            Debug.WriteLine(Histogram.Finite(hardConditioned.SmcMultiple(1000, 100).Sample()));

        }
    }
}
