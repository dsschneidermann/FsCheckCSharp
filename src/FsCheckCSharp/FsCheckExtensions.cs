// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FsCheck;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using FsCheckConfig = FsCheck.Config;

namespace FsCheckCSharp
{
    [ExcludeFromCodeCoverage]
    public static class FsCheckExtensions
    {
        /// <summary>
        ///     Attach a final FsCheckRunner to the configuration that is used.
        ///     This is done automatically when using the methods in <see cref="FsCheckExtensions" />.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static FsCheckConfig AttachToTest(this FsCheckConfig config)
        {
            (config.Runner as FsCheckRunner)?.AttachToTest();
            return config;
        }

        /// <summary>
        ///     Run FsCheck with an FsCheck.Config instance, that supports Arbitrary, unlike the
        ///     "CSharp friendly" FsCheck.Configuration type.
        ///     <seealso cref="QuickCheckThrowOnFailure" />
        ///     <seealso cref="VerboseCheckThrowOnFailure" />
        /// </summary>
        public static void Check(this Property property, FsCheckConfig config = null)
        {
            config = config ?? FsCheckConfig.Default;
            CheckOne(config.Name, config.WithFsCheckRunner().AttachToTest(), property);
        }

        /// <summary>
        ///     Run FsCheck with an FsCheck.Config instance, that supports Arbitrary, unlike the
        ///     "CSharp friendly" FsCheck.Configuration type.
        ///     <seealso cref="QuickCheckThrowOnFailure" />
        ///     <seealso cref="VerboseCheckThrowOnFailure" />
        /// </summary>
        public static void Quick(this Property property, FsCheckConfig config = null)
        {
            config = config ?? FsCheckConfig.Quick;
            CheckOne(config.Name, config.WithFsCheckRunner().AttachToTest(), property);
        }

        /// <summary>
        ///     Run FsCheck using a FsCheckRunner and the given parameters.
        /// </summary>
        public static void QuickCheckThrowOnFailure(
            this Property property, CSharpNotationConfig CSharpNotationConfig = null,
            FsCheckRunnerConfig FsCheckRunnerConfig = null, FsCheckConfig Config = null, List<Type> Arbitrary = null,
            int? EndSize = null, Func<int, object[], string> Every = null, Func<object[], string> EveryShrink = null,
            int? MaxRejected = null, int? MaxTest = null, string Name = null,
            ParallelRunConfig ParallelRunConfig = null, bool? QuietOnSuccess = null,
            Tuple<ulong, ulong, int> Replay = null, IRunner Runner = null, int? StartSize = null)
        {
            Config = Config ?? FsCheckConfig.QuickThrowOnFailure;
            var csharpRunnerConfig = (FsCheckRunnerConfig ?? FsCheckRunnerConfig.Default).With(ThrowOnFailure: true);
            property.Quick(
                Config.WithFsCheckRunner(
                    CSharpNotationConfig, csharpRunnerConfig, Arbitrary, EndSize, Every, EveryShrink, MaxRejected,
                    MaxTest, Name, ParallelRunConfig, QuietOnSuccess, Replay, Runner, StartSize
                )
            );
        }

        /// <summary>
        ///     Run FsCheck with an FsCheck.Config instance and enable TraceNumberOfRuns.
        /// </summary>
        /// <seealso cref="VerboseCheckThrowOnFailure" />
        public static void Verbose(this Property property, FsCheckConfig config = null)
        {
            config = config ?? FsCheckConfig.Verbose;
            CheckOne(
                config.Name, config.WithFsCheckRunner(FsCheckRunnerConfig: FsCheckRunnerConfig.Verbose).AttachToTest(),
                property
            );
        }

        /// <summary>
        ///     Run FsCheck with using a FsCheckRunner and the given parameters.
        /// </summary>
        public static void VerboseCheckThrowOnFailure(
            this Property property, CSharpNotationConfig CSharpNotationConfig = null,
            FsCheckRunnerConfig FsCheckRunnerConfig = null, FsCheckConfig Config = null, List<Type> Arbitrary = null,
            int? EndSize = null, Func<int, object[], string> Every = null, Func<object[], string> EveryShrink = null,
            int? MaxRejected = null, int? MaxTest = null, string Name = null,
            ParallelRunConfig ParallelRunConfig = null, bool? QuietOnSuccess = null,
            Tuple<ulong, ulong, int> Replay = null, IRunner Runner = null, int? StartSize = null)
        {
            Config = Config ?? FsCheckConfig.VerboseThrowOnFailure;
            var csharpRunnerConfig = (FsCheckRunnerConfig ?? FsCheckRunnerConfig.Default).With(ThrowOnFailure: true);
            property.Verbose(
                Config.WithFsCheckRunner(
                    CSharpNotationConfig, csharpRunnerConfig, Arbitrary, EndSize, Every, EveryShrink, MaxRejected,
                    MaxTest, Name, ParallelRunConfig, QuietOnSuccess, Replay, Runner, StartSize
                )
            );
        }

        /// <summary>
        ///     Modify parameters of an FsCheck.Config.
        /// </summary>
        public static FsCheckConfig With(
            this FsCheckConfig config, List<Type> Arbitrary = null, int? EndSize = null,
            Func<int, object[], string> Every = null, Func<object[], string> EveryShrink = null,
            int? MaxRejected = null, int? MaxTest = null, string Name = null,
            ParallelRunConfig ParallelRunConfig = null, bool? QuietOnSuccess = null,
            Tuple<ulong, ulong, int> Replay = null, IRunner Runner = null, int? StartSize = null)
        {
            // Create complex FSharp type
            var replay = config.Replay;
            if (Replay != null)
            {
                replay = FSharpOption<Replay>.Some(
                    new Replay(new Rnd(Replay.Item1, Replay.Item2), FSharpOption<int>.Some(Replay.Item3))
                );
            }

            // Create complex FSharp type
            var parallelRunConfig = config.ParallelRunConfig;
            if (ParallelRunConfig != null)
            {
                parallelRunConfig = FSharpOption<ParallelRunConfig>.Some(
                    new ParallelRunConfig(ParallelRunConfig.MaxDegreeOfParallelism)
                );
            }

            // Create complex FSharp func
            var every = config.Every;
            if (Every != null)
            {
                every = FuncConvert.FromFunc(
                    (Func<int, FSharpList<object>, string>) ((numTest, list) => Every(numTest, list.ToArray()))
                );
            }

            // Create complex FSharp func
            var everyShrink = config.EveryShrink;
            if (Every != null)
            {
                everyShrink = FuncConvert.FromFunc(
                    (Func<FSharpList<object>, string>) (list => EveryShrink(list.ToArray()))
                );
            }

            var result = new FsCheckConfig(
                MaxTest ?? config.MaxTest, MaxRejected ?? config.MaxRejected, replay, Name ?? config.Name,
                StartSize ?? config.StartSize, EndSize ?? config.EndSize, QuietOnSuccess ?? config.QuietOnSuccess,
                every, everyShrink, ToFSharpList(Arbitrary) ?? config.Arbitrary, Runner ?? config.Runner,
                parallelRunConfig
            );

            return result;

            FSharpList<T> ToFSharpList<T>(IEnumerable<T> elements)
            {
                return elements?.Reverse()
                    .Aggregate(FSharpList<T>.Empty, (list, head) => FSharpList<T>.Cons(head, list));
            }
        }

        /// <summary>
        ///     Modify parameters of an FsCheck.Config instance and attach output of
        ///     the latest shrink in CSharp notation.
        /// </summary>
        public static FsCheckConfig WithFsCheckRunner(
            this FsCheckConfig config, CSharpNotationConfig CSharpNotationConfig = null,
            FsCheckRunnerConfig FsCheckRunnerConfig = null, List<Type> Arbitrary = null, int? EndSize = null,
            Func<int, object[], string> Every = null, Func<object[], string> EveryShrink = null,
            int? MaxRejected = null, int? MaxTest = null, string Name = null,
            ParallelRunConfig ParallelRunConfig = null, bool? QuietOnSuccess = null,
            Tuple<ulong, ulong, int> Replay = null, IRunner Runner = null, int? StartSize = null)
        {
            var runner = Runner;
            var csharpRunnerConfig = FsCheckRunnerConfig;
            var csharpNotationConfig = CSharpNotationConfig;

            if (runner == null)
            {
                runner = config.Runner;
            }

            if (runner is FsCheckRunner csharpRunner)
            {
                // Unwrap allows us to call WithFsCheckRunner multiple times without a problem.
                runner = csharpRunner.RunnerImplementation;

                // Preserve wrapped runner configs unless method parameters were set.
                if (FsCheckRunnerConfig == null)
                {
                    csharpRunnerConfig = csharpRunner.FsCheckRunnerConfig;
                }

                if (CSharpNotationConfig == null)
                {
                    csharpNotationConfig = csharpRunner.CSharpNotationConfig;
                }
            }

            return config.With(
                Arbitrary: Arbitrary, EndSize: EndSize, Every: Every, EveryShrink: EveryShrink,
                MaxRejected: MaxRejected, MaxTest: MaxTest, Name: Name, ParallelRunConfig: ParallelRunConfig,
                QuietOnSuccess: QuietOnSuccess, Replay: Replay,
                Runner: new FsCheckRunner(
                    runnerImplementation: runner, csharpRunnerConfig: csharpRunnerConfig,
                    csharpNotationConfig: csharpNotationConfig, maxTest: MaxTest ?? config.MaxTest,
                    traceDiagnosticsWriter: null
                ), StartSize: StartSize
            );
        }

        private static void CheckOne(string name, FsCheckConfig config, Property property)
        {
            try
            {
                FsCheck.Check.One(name, config, property);
            }
            catch (Exception)
            {
                if (config.Runner is FsCheckRunner csharpRunner)
                {
                    throw new Exception(csharpRunner.GetExceptionResult());
                }

                throw;
            }
        }
    }
}
