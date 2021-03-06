Probabilistic C#
================

A monadic probabilistic programming framework for Bayesian modelling and inference in C#.

## Introduction
In general purpose languages (and even in many languages designed for statistical computing, like R), the description of a Bayesian model is often tightly coupled with the inference algorithm. This tight coupling can cripple iterative or exploratory approaches to Bayesian modelling.

#### Probabilistic programming languages
A probabilistic programming language (PPL) treats entire probabilistic models as primitives. This brings a number of advantages to modelling:

- Models can be specified in a declarative manner
- Models compose freely, allowing us to construct complex models from simpler ones
- Inference and modelling are completely separate; we can define models without caring how we're going to do inference

An inference strategy in a PPL is really just a compiler or an interpreter for a PPL model, which gives us a lot of flexibility:

- We can define an inference method once and easily run it against *any* model
- We can run multiple inference methods against the same model 
- Inference methods are composable; this library, for instance, composes a Metropolis-Hastings algorithm with a Sequential Monte Carlo algorithm to get a Particle Independent Metropolis-Hastings algorithm

#### This library

One of the most elegant approaches to probabilistic programming is described in the fantastic paper *[Practical Probabilistic Programming with Monads](http://mlg.eng.cam.ac.uk/pub/pdf/SciGhaGor15.pdf)* by Ścibior, Ghahramani, and Gordon (2015). The paper presents an excellent Haskell implementation that leans heavily on GADTs and free monad constructions. Unfortunately, the use of these abstractions often hinders translation into mainstream languages (particularly statically-typed languages with weaker type systems), slowing the dissemination of valuable ideas in the broader community.

This library is a C# implementation of the approach described in Ścibior, Ghahramani, and Gordon (2015). It also extends the paper's implementation to allow parallel sampling from independent distributions.

## Getting started
Install from NuGet: [https://www.nuget.org/packages/ProbCSharp/](https://www.nuget.org/packages/ProbCSharp/)

Once installed, you can reference the library by adding this to the top of your C# code:

```cs
using ProbCSharp;
using static ProbCSharp.ProbBase;
```

The namespace `ProbCSharp` contains all the types, and `ProbCSharp.ProbBase` contains the functions. If you're running a version of C# older than C# 6, you won't be able to use the `using static` feature, so all functions will need to be qualified with `ProbBase.Function`.

## Tutorial!
Another probabilistic programming language called Anglican has an interesting [example](http://www.robots.ox.ac.uk/~fwood/anglican/examples/viewer/?worksheet=indian-gpa) on their website called the *Indian GPA problem*. It's actually an example of how probabilistic programming techniques fail, but it's a great illustration of the flexibility of the modelling process, so we'll reimplement it here. Here's a description of the problem, quoted verbatim from Anglican's website:

> if you observe that a student GPA is exactly `4.0` in a model of transcripts of students from the USA (GPA's from `0.0` to `4.0` and India (GPA's from `0.0` to `10.0`) what is the probability that the student is from India? This problem gets at the heart of measure theoretic problems arising from combining distribution and density, problems not easy to automatically avoid in a probabilistic programming system. 

#### Setup
Let's start by defining some simple types to hold our data:

```cs
public enum Country { USA, India };

public class Grade
{
    public double GPA;
    public Country Country;
    public Grade(double gpa, Country country)
    {
        GPA = gpa;
        Country = country;
    }
}
```

#### Modelling American GPAs
We can model the distribution of American GPAs with a Beta distribution, skewed to account for grade inflation:

```cs
var intervalDist = Beta(2, 8);
```

There are also a small number of students that either score exactly `0.0` or exactly `4.0`; of those, around 85% score exactly `4.0`. We can model this with a Bernoulli distribution:

```cs
var boundaryDist = from isPerfectGpa in Bernoulli(0.85)
                   select isPerfectGpa ? 1.0 : 0.0;
```

If we assume that 5% of students score exactly `0.0` or `4.0`, we can compose the distributions we've just defined to form a partially continuous distribution for the American GPA:

```cs
var americanGrades =
    from isBoundary in Bernoulli(0.05)
    from gpa in isBoundary ? boundaryDist : intervalDist
    select new Grade(gpa * 4, Country.USA);
```

Let's sample from this distribution and plot a histogram of the results:

```cs
var americanSamples = americanGrades.SampleN(10000);

Histogram.Unweighted(americanSamples.Select(grade => grade.GPA));

// 0.00 0.13 ##
// 0.13 0.27 
// 0.27 0.40 
// 0.40 0.53 
// 0.53 0.67 
// 0.67 0.80 
// 0.80 0.93 
// 0.93 1.07 
// 1.07 1.20 
// 1.20 1.33 
// 1.33 1.47 
// 1.47 1.60 
// 1.60 1.73 
// 1.73 1.87 #
// 1.87 2.00 #
// 2.00 2.13 ###
// 2.13 2.27 ####
// 2.27 2.40 ######
// 2.40 2.53 ########
// 2.53 2.67 ############
// 2.67 2.80 ###############
// 2.80 2.93 ##################
// 2.93 3.07 #####################
// 3.07 3.20 ###########################
// 3.20 3.33 ###############################
// 3.33 3.47 ###################################
// 3.47 3.60 ##################################
// 3.60 3.73 ###############################
// 3.73 3.87 #####################
// 3.87 4.00 ######################
```

We can see our Beta distribution along with some small bumps at `0.0` and `4.0`.

#### Modelling Indian GPAs
India's GPA distribution goes from `0.0` to `10.0`, and doesn't exhibit grade inflation. Only 1% of students have scores at the exact boundary, and 90% of those students have a perfect GPA. We can model this as follows:

```cs
var indianGrades =
    from isBoundary in Bernoulli(0.01)
    from gpa in isBoundary ? Bernoulli(0.9, 1.0, 0.0): Beta(5, 5)
    select new Grade(gpa * 10, Country.India);

var indianSamples = indianGrades.SampleN(10000);
Histogram.Unweighted(indianSamples.Select(grade => grade.GPA));

// 0.00  0.33 
// 0.33  0.67 
// 0.67  1.00 
// 1.00  1.33 #
// 1.33  1.67 ##
// 1.67  2.00 ###
// 2.00  2.33 ######
// 2.33  2.67 ##########
// 2.67  3.00 ##############
// 3.00  3.33 ##################
// 3.33  3.67 #####################
// 3.67  4.00 ########################
// 4.00  4.33 ###########################
// 4.33  4.67 ###############################
// 4.67  5.00 ################################
// 5.00  5.33 #################################
// 5.33  5.67 ##############################
// 5.67  6.00 ###############################
// 6.00  6.33 ##########################
// 6.33  6.67 ######################
// 6.67  7.00 ##################
// 7.00  7.33 #############
// 7.33  7.67 ##########
// 7.67  8.00 ######
// 8.00  8.33 ####
// 8.33  8.67 ##
// 8.67  9.00 
// 9.00  9.33 
// 9.33  9.67 
// 9.67 10.00 ###
```

#### Composing American and Indian GPA models

These complex probability distributions can be composed like any other. Let's assume 25% of students are from America:

```cs
var combined = from isAmerican in Bernoulli(0.25)
               from grade in isAmerican ? americanGrades : indianGrades
               select grade;

var combinedSamples = combined.SampleN(10000); 

Histogram.Unweighted(combinedSamples.Select(g => g.GPA));

// 0.00  0.33 
// 0.33  0.67 
// 0.67  1.00 
// 1.00  1.33 
// 1.33  1.67 #
// 1.67  2.00 #####
// 2.00  2.33 ########
// 2.33  2.67 ##############
// 2.67  3.00 ##########################
// 3.00  3.33 ####################################
// 3.33  3.67 ###########################################
// 3.67  4.00 #######################################
// 4.00  4.33 ######################
// 4.33  4.67 ########################
// 4.67  5.00 ########################
// 5.00  5.33 ########################
// 5.33  5.67 #######################
// 5.67  6.00 ######################
// 6.00  6.33 ##################
// 6.33  6.67 ##################
// 6.67  7.00 ##############
// 7.00  7.33 ##########
// 7.33  7.67 #######
// 7.67  8.00 #####
// 8.00  8.33 ###
// 8.33  8.67 #
// 8.67  9.00 
// 9.00  9.33 
// 9.33  9.67 
// 9.67 10.00 ###
```

#### Performing inference
We can condition on this distribution to infer the probability that a student is from America or India, given that their GPA is *exactly* `4.0`. Since the probability of any particular point on a continuous distribution is zero, only the American distribution has any probability mass at `4.0`, and the "correct" answer is that a student with a `4.0` GPA must be American with probability `1.0`. 

However, since most useful likelihood functions include a noise model, we don't really condition on "exactly `4.0`" but "`4.0` plus some noise term", which will produce the "wrong" answer. Here's a likelihood function that includes a Gaussian noise term with a variance of `0.1`:

```cs
Func<Grade, Prob> likelihood4Gpa = grade => Pdf(NormalC(4.0, 0.1), grade.GPA);
```

We can use this likelihood function to construct a conditional distribution:

```cs
var countryGiven4Gpa = from grade in combined.Condition(likelihood4Gpa)
                       select grade.Country;
```

Let's try to sample from this distribution:

```cs
countryGiven4Gpa.Sample();
// System.ArgumentException: Cannot sample from conditional distribution.
```

We get an exception, because any conditionals in a distribution must be removed before sampling. It's up to a particular inference method to decide how to evaluate these conditionals. Let's use a Sequential Monte Carlo to perform inference on this distribution:

```cs
// 1000 iterations of an SMC with 100 particles.
var countryPosterior = countryGiven4Gpa.SmcMultiple(1000, 100);

// Run the inference
var whichCountry = posterior.Sample();

Histogram.Finite(whichCountry);

// India  62.6% #########################
//  USA  37.4% ##############
```

This is obviously the "wrong" answer; let's see what happens if we reduce the variance of the Gaussian noise to `0.001`:

```cs
// India    56% ######################
//   USA    44% #################
```

...to `0.00001`: 

```cs
//   USA  87.2% ##################################
// India  12.8% #####
```

We're headed in the right direction; let's get rid of the noise term completely:

```cs
var hardCond = from grade in combined.Condition(g => g.GPA == 4.0 ? Prob(1.0) : Prob(0.0))
               select grade.Country;
Histogram.Finite(hardCond.SmcMultiple(1000, 100).Sample());

// India     0% 
//   USA   100% ########################################
```

And we get the "correct" answer, but this likelihood term only really works for `0.0`, `4.0`, and `10.0`, where we have discrete probability mass.

## Parallelizing independent distributions
Ideally, we'd be able to perform inference on independent distributions simultaneously, but monadic composition oversequentialises independent distributions. This was one of the avenues for future work identified in Ścibior, Ghahramani, and Gordon (2015). 

This library implements a clean syntax for parallelizing independent distributions. Let's look at an a example that could benefit from parallelization:

```cs
var smc = model.SmcMultiple(1000, 100);
var imp = ImportanceDist(1000, model2);

var combined = from d1 in smc
               from d2 in imp
               select Math.Min(d1, d2);

combined.SmcMultiple(1000, 100).Sample();
```

In this example, we're sampling from two independent distributions. Even though they're independent, inference algorithms will still wait until sampling from `smc` is complete before beginning to sample from `imp`.

We can solve this by marking certain distributions as independent, and then running them in parallel:

```cs
var combinedPar = from d1 in Independent(smc)
                  from d2 in Independent(imp)
                  from pair in RunIndependent(d1, d2)
                  select Math.Min(pair.Item1, pair.Item2);

combined.SmcMultiple(1000, 100).SampleParallel();
```

We can also write the above like this:

```cs
var combinedPar = from pair in RunIndependent(smc, imp)
                  select Math.Min(pair.Item1, pair.Item2);
```

Inference algorithms that are run against these parallelized distributions will automatically spawn copies of themselves in a Task whenever they encounter independent distributions. This can lead to speedups of around 40% - 100%, depending on the number of cores in your system.

#### Parallel sampling
Sampling many times from the same distribution is an inherently parallelizable task, so we can sample in parallel by writing: 

```cs
distribution.SampleNParallel(1000)
```

## Writing inference methods
This library achieves composition of models and inference methods by using a free monad construction over a GADT. These concepts are well-hidden from the user, but users wishing to write their own inference methods might come into closer contact with these areas.

Writing an inference method that works on all models is achieved by implementing the `DistInterpreter<A, X>` interface, where `A` is the type the distribution operates over, and `X` is the type the interpreter will convert the distribution into.

For example, this library doesn't implement sampling as a special operator, but as just another interpreter, with the signature:

```cs
class Sampler<A> : DistInterpreter<A, A>
```

This means that it interprets a `Dist<A>` into a value of type `A`. Composable inference methods interpret distribution into other distributions- the Sequential Monte Carlo interpreter has a signature:

```
class Smc<A> : DistInterpreter<A, Dist<Samples<A>>>
```

## More examples

### Bayes Point Machines

We can define a Bayes Point Machine to predict if someone will buy a particular product, based on some demographic information about them. Some classes to hold our data:

```cs
public struct BuyData
{
    public BuyData(double income, double age, bool willBuy)
    {
        Income = income;
        Age = age;
        WillBuy = willBuy;
    }
    public double Income;
    public double Age;
    public bool WillBuy;
}

public class BuyWeight
{
    public BuyWeight(double incomeWeight, double ageWeight)
    {
        IncomeWeight = incomeWeight;
        AgeWeight = ageWeight;
    }
    public double IncomeWeight;
    public double AgeWeight;
}
```

We've got a dataset that associates a person's age and income with their buying decision for our product:

```cs
var observed = new List<BuyData> {
        new BuyData(63, 38, true),
        new BuyData(16, 23, false),
        new BuyData(28, 40, true),
        new BuyData(55, 27, true),
        new BuyData(22, 18, false),
        new BuyData(20, 40, false)
    };
```

If we think of our demographic features as a vector, we want to find a vector of weights where if we take the inner product of the weights and features, we get a positive result if they're buying and a negative result if they're not.

We don't really know what the correct weights should be, so we define a prior with heavy tails:

```cs
var prior = from ageWeight in Normal(0, 100)
            from incomeWeight in Normal(0, 100)
            select new BuyWeight(incomeWeight, ageWeight);
```

Now we define our likelihood function:

```cs
Func<BuyWeight, BuyData, Prob> likelihood = (w, d) =>
{
    var lossMagnitude = Pdf(
        NormalC(0, 0.1),
        (w.AgeWeight * d.Age) - (w.IncomeWeight * d.Income)
    ).Value;
    var loss = d.WillBuy ? lossMagnitude : -lossMagnitude;
    return Prob(loss);
};
```

We form our posterior by folding over the data, conditioning with the likelihood function each time:

```cs
var posterior = data.Aggregate(prior, (dist, datum) =>
    dist.Condition(weight => BpmLikelihood(weight, datum)));
```

Now we can run an inference algorithm on our posterior:

```cs
var samples = posterior.SmcMultiple(10000, 10).Sample();
var approxPosterior = CategoricalF(samples.Normalize());
```

We're ready to predict if someone is likely to buy our product. We can define a prediction function:

```cs
Func<FiniteDist<BuyWeight>, BuyData, Prob> predict = (dist, datum) =>
    dist.ProbOf(w => likelihood(w, datum).Value > 0.5);
```

and use it to predict for a few people:

```cs
var person1 = new BuyData(58, 36, true);
var person2 = new BuyData(18, 24, true);
var person3 = new BuyData(22, 37, true);

predict(approxPosterior, person1);
// 95.4%

predict(approxPosterior, person2);
// 35.9%

predict(approxPosterior, person3);
// 14%
```

It looks like the first person is probably going to buy our product, but the other two aren't!

### Bayesian networks
```cs
// Truth table for grass being wet
Func<bool, bool, Prob> wetProb = (rain, sprinkler) =>
{
    if (rain && sprinkler) return Prob(0.98);
    if (rain && !sprinkler) return Prob(0.8);
    if (!rain && sprinkler) return Prob(0.9);
    return Prob(0);
};

// Bayesian network for sprinkler model
var sprinklerModel =
     from rain in Bernoulli(Prob(0.2))
     from sprinkler in Bernoulli(Prob(rain ? 0.01 : 0.4))
     from wet in Bernoulli(wetProb(rain, sprinkler))
     select new SprinklerEvent(rain, sprinkler, wet);

// Probability it rained should still be 20%
var prRained = sprinklerModel.ProbOf(e => e.Raining);
// 20%

var prRainedAndWet = sprinklerModel.ProbOf(e => e.Raining && e.GrassWet);
// 16%
```

We can now condition on certain events, to answer questions like *What is the probability it rained, given that the grass is wet?*

```cs
var givenGrassWet = sprinklerModel.ConditionHard(e => e.GrassWet);

var prRainingGivenWet = givenGrassWet.ProbOf(e => e.Raining);
// 35.7%
```

Doing this results in 35.7%. This must be correct because it's the same answer that Wikipedia gives.

### Bayesian coin
**We are given a coin. There's a 70% chance the coin is fair, and a 30% chance it's biased. If the coin is biased, it will land on heads 20% of the time. If we flip the coin 8 times and get the sequence HTTHHTHT, what is the chance that we received a fair coin?**

We can represent our prior on the weight of the coin like this:

```cs
var coinWeightPrior = from isFair in BernoulliF(Prob(0.7))
                      select isFair ? 0.5 : 0.2;
coinWeightPrior.Histogram();
// 0.5    70% ###################################
// 0.2    30% ###############
```

The likelihood of a weight, given a particular coin flip, is:

```cs
Func<double, Coin, Prob> likelihood =
  (weight, coin) => coin.IsHeads ? weight : 1 - weight;
```

Now we can calculate the probability that our coin is fair, given these flips:

```cs
var flips = new List<Coin> 
{
  Heads, Tails, Tails, Heads,
  Heads, Tails, Heads, Tails
};

var posterior = coinWeightPrior.UpdateOn(likelihood, flips);
posterior.Histogram();
// 0.5 93.3% ##############################################
// 0.2 6.71% ###
```

There's only a 6.71% chance that the coin is biased.

**What if we flip the same coin 8 more times, and it comes up tails every time?**

We can take the posterior distribution we've created and update on it with another set of coin flips:

```cs
var moreFlips  = Enumerable.Repeat(Tails, 8);

var newPosterior = posterior.UpdateOn(likelihood, moreFlips);
newPosterior.Histogram();
// 0.5 24.5% ############
// 0.2 75.5% #####################################
```

Even though we used to be 93% sure that the coin was fair, observing 8 tails in a row means it's far more likely that we have a biased coin than a fair one.

### Monty Hall problem
We can naturally model the Monty Hall problem, and generalize it to arbitrary numbers of doors.

```cs
internal class MontyHallState
{
    public Door Winner { get; }
    public Door Picked { get; }

    public MontyHallState(Door winner, Door picked)
    {
        Winner = winner;
        Picked = picked;
    }
}

// Monty hall without switching
Func<List<Door>, FiniteDist<MontyHallState>> 
montyHall = doors =>
    from winner in EnumUniformD(doors)
    from picked in EnumUniformD(doors)
    select new MontyHallState(winner, picked);
```

Let's see the probability of us winning if we stay:

```cs
// You won if you picked the winner 
Func<MontyHallState, bool> won = s => s.Picked == s.Winner;

var threeDoors = new List<Door> {
    new Door("1"),
    new Door("2"),
    new Door("3")
};
var prStayWon = montyHall(threeDoors).ProbOf(won);
// 33.3%
```

We have a one in three chance of winning if we stay with our original choice. What if we switch?

```cs
// Host has equal chance of opening any non-winning door you didn't pick
Func<List<Door>, Door, Door, FiniteDist<Door>>
Open = (doors, winner, picked) => EnumUniformD(doors.Where(d => d != picked && d != winner));

// Monty hall with switching
Func<List<Door>, FiniteDist<MontyHallState>>
switched = doors =>
    from initial in montyHall(doors)
    from opened in Open(doors, initial.Winner, initial.Picked)
    // Pick one of the closed doors you didn't pick at first
    from newPick in EnumUniformD(doors.Where(d => d != initial.Picked && d != opened))
    select new MontyHallState(initial.Winner, newPick);

var prSwitchWon = switched(threeDoors).ProbOf(won);
// 66.6%
```

Switching gives a two in three chance of winning! We can also generalize to more doors:

```cs
// Four door monty hall
var fourDoors = new List<Door> {
    new Door("1"),
    new Door("2"),
    new Door("3"),
    new Door("4")
};

var prStayWon4 = montyHall(fourDoors).ProbOf(won);
// 25%

var prSwitchWon4 = switched(fourDoors).ProbOf(won);
// 37.5%
```
