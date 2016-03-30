﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProbabilityMonad;
using System.Diagnostics;
using static ProbabilityMonad.Distributions;
using static ProbabilityMonad.Base;
using static ProbabilityMonad.ProbabilityFunctions;
using System.Linq;
using System.Collections.Generic;
using CSharpProbabilityMonad;

namespace ProbabilityMonad.Test
{
    [TestClass]
    public class BernoulliTest
    {
        [TestMethod]
        public void CoinFlip_SumsToOne()
        {
            var coin = Bernoulli(Prob(0.5));
            Assert.AreEqual(1, coin.Distribution.Select(p => p.Prob.Value).Sum());
        }

        [TestMethod]
        public void SingleUniformDie_ProbIsOne()
        {
            var die = UniformD(1, 2, 3, 4, 5, 6);
            Assert.AreEqual("16.6%", die.ProbOf(roll => roll == 1).ToString());
        }

        [TestMethod]
        public void ThreeDice_AtLeast2Odd()
        {
            var weight = from isFair in Bernoulli(Prob(0.8))
                         select isFair ? 0.5 : Beta(5, 1).Sample();

            Func<bool, FiniteDist<double>, FiniteDist<double>> toss = (t, dist) => SoftConditional(w => Prob(t ? w : 1-w), dist);

            Func<List<bool>, FiniteDist<double>, FiniteDist<double>> tosses = (ts, dist) => ts.Aggregate(dist, (d, t) => toss(t, d));

            var observations = new List<bool> { true, false, true, true, false, true, true, false };
            var posteriorWeight = tosses(observations, weight);
            foreach (var something in posteriorWeight.Distribution)
            {
                Debug.WriteLine($"{something.Item}: {something.Prob}");
            }
        }

        [TestMethod]
        public void ChanceAddOneToRoll()
        {
            Func<int, FiniteDist<int>> flipCoinAddOne = x => UniformD(x, x + 1);

            var die = UniformD(1, 2, 3, 4, 5, 6);
            var rollAndFlip = from roll in die
                              from maybeAdded in flipCoinAddOne(roll)
                              select maybeAdded;

            Assert.AreEqual("8.3%", rollAndFlip.ProbOf(a => a == 7).ToString());
            Assert.AreEqual("16.6%", rollAndFlip.ProbOf(a => a == 6).ToString());
            Assert.AreEqual("8.3%", rollAndFlip.ProbOf(a => a == 1).ToString());
        }

        [TestMethod]
        public void SprinklerBayes()
        {
            Func<bool, bool, Prob> wetProb = (rain, sprinkler) =>
            {
                if (rain && sprinkler) return Prob(0.98);
                if (rain && !sprinkler) return Prob(0.8);
                if (!rain && sprinkler) return Prob(0.9);
                return Prob(0);
            };

            var sprinklerModel =
                from rain in Bernoulli(Prob(0.3))
                from sprinkler in Bernoulli(Prob(rain ? 0.1 : 0.4))
                from wet in Bernoulli(wetProb(rain, sprinkler))
                select wet;

        }

    }
}

