using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProbCSharp;
using static ProbCSharp.ProbBase;

namespace ProbCSharp.Test.Models
{
    public static class ObservationExt
    {

        /// <summary>
        /// Update weight distribution based on an observations
        /// </summary>
        public static Func<double, Dist<double>, Dist<double>>
        ObservationUpdate = (obs, dist) => Condition(w => Pdf(NormalPrimitive(10.0, 10.0), obs), dist);

        /// <summary>
        /// Update weight distribution based on a series of observations
        /// </summary>
        public static Func<List<double>, Dist<double>, Dist<double>>
        ObservationsUpdate = (obs, dist) => obs.Aggregate(dist, (d, t) => ObservationUpdate(t, d));

      }

    public class Observation
    {
        public Observation(double obs)
        {
            CurrentData = obs;
        }

        public double CurrentData { get; set; }

        public static Dist<Param> UpdateWithObservation(Dist<Param> prior, double x)
        {
            return Condition(param => Pdf(NormalPrimitive(0.0, 0.5), x), prior);
        }

        /// <summary>
        /// Represents posterior distribution after updating on a list of points
        /// </summary>
        public static Dist<Param> UpdateWithObservations(Dist<Param> prior, List<double> xs)
        {
            return xs.Aggregate(prior, UpdateWithObservation);
        }
    }
}
