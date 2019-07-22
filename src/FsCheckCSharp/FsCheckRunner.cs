// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using FsCheckConfig = FsCheck.Config;

namespace FsCheckCSharp
{
    public class FsCheckRunner : IRunner, IDisposable
    {
        private readonly List<string> debugTraceLines = new List<string>();
        private Task backgroundTask;
        private bool detailedAssertFailedIfSet;
        private CancellationTokenSource detailedBackgroundCancel;

        private bool detailedCurrentStageIsShrinking;
        private int detailedLatestNumExecutions;
        private int detailedLatestNumShrinks;

        private BoxedDateTimeOffset detailedLatestSeenActivity = new BoxedDateTimeOffset(DateTimeOffset.Now);

        private int detailedLatestTraceTimer;
        private bool isDetailedOnTraceGeneratedHit;
        private bool isDetailedOnTraceGeneratedHitSinceLast;
        private bool isDetailedOnTraceTestedHit;
        private bool isDetailedOnTraceTestedHitSinceLast;
        private bool isDetailedTraces;
        private bool isRunningShrinks;
        private bool isRunningTests;
        private int latestNumTests;
        private int numberOfShrinks;

        private string throwOnFailureMessage;

        public FsCheckRunner(
            IRunner runnerImplementation, FsCheckRunnerConfig csharpRunnerConfig,
            CSharpNotationConfig csharpNotationConfig, int? maxTest, Action<string> traceDiagnosticsWriter)
        {
            RunnerImplementation = runnerImplementation;
            FsCheckRunnerConfig = csharpRunnerConfig ?? FsCheckRunnerConfig.Default;
            CSharpNotationConfig = csharpNotationConfig ?? CSharpNotationConfig.Default;
            MaxTest = maxTest ?? FsCheckConfig.Default.MaxTest;
            TraceDiagnosticsWriter = traceDiagnosticsWriter ?? Console.WriteLine;

            TestTimer.Start();
        }

        public IRunner RunnerImplementation { get; }

        private Stopwatch DetailedTraceTimer { get; } = new Stopwatch();

        private Stopwatch DetailedTraceTotalTimer { get; } = new Stopwatch();

        private Stopwatch TestTimer { get; } = new Stopwatch();

        public FsCheckRunnerConfig FsCheckRunnerConfig { get; }
        public CSharpNotationConfig CSharpNotationConfig { get; }
        public int MaxTest { get; }

        private Action<string> TraceDiagnosticsWriter { get; }
        private Exception FailureCausedByException { get; set; }

        public void Dispose()
        {
            FsCheckRunnerConfig.Events.TraceGenerated -= OnTraceGenerated;
            FsCheckRunnerConfig.Events.TraceTested -= OnTraceTested;
            FsCheckRunnerConfig.Events.TraceTimer -= OnTraceTimer;
            detailedBackgroundCancel?.Cancel();
            backgroundTask?.Wait();
        }

        public void OnStartFixture(Type t)
        {
            RunnerImplementation.OnStartFixture(t);
            TestTimer.Restart();
        }

        public void OnArguments(
            int numTest, FSharpList<object> args, FSharpFunc<int, FSharpFunc<FSharpList<object>, string>> every)
        {
            TestTimer.Stop();
            RunnerImplementation.OnArguments(numTest, args, every);
            latestNumTests++;

            if (!isDetailedTraces)
            {
                if (FsCheckRunnerConfig.TraceNumberOfRuns)
                {
                    Trace(
                        FormattableString.Invariant(
                            $"Ran test: {latestNumTests} / {MaxTest} in {TestTimer.ElapsedMilliseconds:n0}ms"
                        )
                    );
                }

                isRunningTests = true;
            }

            TestTimer.Restart();
        }

        public void OnShrink(FSharpList<object> args, FSharpFunc<FSharpList<object>, string> everyShrink)
        {
            TestTimer.Stop();
            RunnerImplementation.OnShrink(args, everyShrink);
            numberOfShrinks++;

            if (!isDetailedTraces)
            {
                if (isRunningTests && FsCheckRunnerConfig.TraceNumberOfRuns)
                {
                    isRunningTests = false;
                    FormattableString.Invariant($"Failed test: {latestNumTests} / {MaxTest}");
                }

                if (isRunningShrinks && FsCheckRunnerConfig.TraceNumberOfRuns)
                {
                    Trace(
                        FormattableString.Invariant(
                            $"Ran shrink: {numberOfShrinks} in {TestTimer.ElapsedMilliseconds:n0}ms"
                        )
                    );
                }

                isRunningShrinks = true;
            }

            TestTimer.Restart();
        }

        public void OnFinished(string name, TestResult testResult)
        {
            if (testResult.IsFailed)
            {
                var size = ((TestResult.Failed) testResult).Item7;

                var latestShrinks = ((TestResult.Failed) testResult).Item3.Select(x => (object[]) x)
                    .Cast<IEnumerable>()
                    .Select(y => y);
                //var origArguments = ((TestResult.Failed) testResult).Item2
                //    .Select(x => (object[])x).Cast<IEnumerable>().Select(y => y);

                var message = string.Join(
                    Environment.NewLine,
                    $"Falsifiable, after {latestNumTests} test{(latestNumTests != 1 ? "s" : string.Empty)} ({numberOfShrinks} shrink{(numberOfShrinks != 1 ? "s" : string.Empty)})",
                    $"Last step was invoked with size of {size}", "Shrunk:",
                    $"{CSharpNotationSerializer.SerializeEach(latestShrinks.ToList(), CSharpNotationConfig)}"
                );

                if (((TestResult.Failed) testResult).Item4.IsFailed)
                {
                    var failedOutcome = (Outcome.Failed) ((TestResult.Failed) testResult).Item4;
                    FailureCausedByException = failedOutcome.Item;
                }

                if (!FsCheckRunnerConfig.ThrowOnFailure)
                {
                    Trace(message);
                }
                else
                {
                    throwOnFailureMessage = message;
                }
            }

            RunnerImplementation.OnFinished(name, testResult);

            // No need for event handlers to keep this runner alive, if we are executing multiple
            // property checks in the same test method.
            Dispose();
        }

        public void AttachToTest()
        {
            // Check if we want detailed traces enabled
            if (FsCheckRunnerConfig.TraceDiagnosticsEnabled)
            {
                isDetailedTraces = true;

                FsCheckRunnerConfig.Events.TraceGenerated += OnTraceGenerated;
                FsCheckRunnerConfig.Events.TraceTested += OnTraceTested;
                FsCheckRunnerConfig.Events.TraceTimer += OnTraceTimer;

                detailedBackgroundCancel = new CancellationTokenSource();
                var ct = detailedBackgroundCancel.Token;

                if (isDetailedTraces || FsCheckRunnerConfig.TraceNumberOfRuns)
                {
                    backgroundTask = Task.Run(() => TimeReporterTask(ct), CancellationToken.None);
                }

                DetailedTraceTimer.Start();
                DetailedTraceTotalTimer.Start();
            }

            async Task TimeReporterTask(CancellationToken cancellationToken)
            {
                var firstRun = true;
                var hasWarnedAboutMissingTraces = false;
                var i = 0;
                var startTime = DateTimeOffset.Now;
                while (!cancellationToken.IsCancellationRequested)
                {
                    i++;
                    var now = DateTimeOffset.Now;
                    var desiredEnd = startTime.AddSeconds(i);
                    var wait = Math.Max(1, (int) (desiredEnd - now).TotalMilliseconds);
                    try
                    {
                        await Task.Delay(wait, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Trace(
                            string.Concat(
                                "time passed in total is now ",
                                FormattableString.Invariant($"{DetailedTraceTotalTimer.ElapsedMilliseconds:n0}ms")
                            )
                        );
                    }

                    // Give the test a few seconds to start, then report missing trace
                    if (!firstRun && isDetailedTraces && !cancellationToken.IsCancellationRequested)
                    {
                        WarnAboutMissingTraces();

                        if (!isDetailedOnTraceTestedHitSinceLast || !isDetailedOnTraceGeneratedHitSinceLast)
                        {
                            var secondsPassed = DateTimeOffset.Now - detailedLatestSeenActivity.InnerDateTimeOffset;
                            Trace(
                                FormattableString.Invariant(
                                    $"--- {secondsPassed.TotalSeconds:n0} seconds have passed since any activity"
                                )
                            );
                        }

                        isDetailedOnTraceTestedHitSinceLast = false;
                        isDetailedOnTraceGeneratedHitSinceLast = false;
                    }

                    firstRun = false;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        WarnAboutMissingTraces();
                    }
                }

                void WarnAboutMissingTraces()
                {
                    if (hasWarnedAboutMissingTraces || isDetailedOnTraceTestedHit || isDetailedOnTraceGeneratedHit)
                    {
                        return;
                    }

                    hasWarnedAboutMissingTraces = true;
                    Trace(
                        "--- No calls to trace call functions, make sure your code is instrumented with the functions from FsCheckRunnerConfig.TraceDiagnostics.Events"
                    );
                }
            }
        }

        public class BoxedDateTimeOffset : IEquatable<BoxedDateTimeOffset>
        {
            public BoxedDateTimeOffset(DateTimeOffset InnerDateTimeOffset)
            {
                this.InnerDateTimeOffset = InnerDateTimeOffset;
            }

            public DateTimeOffset InnerDateTimeOffset { get; }

            public bool Equals(BoxedDateTimeOffset other)
            {
                return other != null && InnerDateTimeOffset.Equals(other.InnerDateTimeOffset);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BoxedDateTimeOffset);
            }

            public override int GetHashCode()
            {
                return 449512821 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(InnerDateTimeOffset);
            }

            public static bool operator ==(BoxedDateTimeOffset offset1, BoxedDateTimeOffset offset2)
            {
                return EqualityComparer<BoxedDateTimeOffset>.Default.Equals(offset1, offset2);
            }

            public static bool operator !=(BoxedDateTimeOffset offset1, BoxedDateTimeOffset offset2)
            {
                return !(offset1 == offset2);
            }
        }

        public string GetExceptionResult()
        {
            var sb = new StringBuilder();
            sb.AppendLine(throwOnFailureMessage);
            sb.AppendLine("with exception:");
            sb.AppendLine($"{FailureCausedByException}");

            if (isDetailedTraces || FsCheckRunnerConfig.TraceNumberOfRuns)
            {
                sb.AppendLine("with trace messages:");
                debugTraceLines.ForEach(x => sb.AppendLine(x));
            }

            return sb.ToString();
        }

        public void OnTraceGenerated(string additional)
        {
            if (!isDetailedTraces)
            {
                return;
            }

            isDetailedOnTraceGeneratedHit = true;
            isDetailedOnTraceGeneratedHitSinceLast = true;

            // Atomic swap of detailedLatestSeenActivity
            BoxedDateTimeOffset initialValue;
            BoxedDateTimeOffset computedValue;
            do
            {
                initialValue = detailedLatestSeenActivity;
                computedValue = new BoxedDateTimeOffset(DateTimeOffset.Now);
            } while (initialValue != Interlocked.CompareExchange(
                ref detailedLatestSeenActivity, computedValue, initialValue
            ));

            if (detailedAssertFailedIfSet)
            {
                Trace(
                    string.Concat(OnTraceGetMessage("failed test", additional), $" ---> shrinking{Environment.NewLine}")
                );

                detailedCurrentStageIsShrinking = true;
                detailedLatestNumExecutions = 0;
                detailedLatestNumShrinks++;
            }

            detailedAssertFailedIfSet = true;
            detailedLatestTraceTimer = 0;
            detailedLatestNumExecutions++;
            Trace(OnTraceGetMessage("generated", additional));
            DetailedTraceTimer.Restart();
        }

        public void OnTraceTested(string additional)
        {
            if (!isDetailedTraces)
            {
                return;
            }

            isDetailedOnTraceTestedHit = true;
            isDetailedOnTraceTestedHitSinceLast = true;
            Trace(OnTraceGetMessage("succeeded test", additional));
            detailedAssertFailedIfSet = false;
            DetailedTraceTimer.Restart();
        }

        public void OnTraceTimer(string additional)
        {
            if (!isDetailedTraces)
            {
                return;
            }

            detailedLatestTraceTimer++;
            Trace(OnTraceGetMessage($"timer{detailedLatestTraceTimer} hit", additional));
            DetailedTraceTimer.Restart();
        }

        private string OnTraceGetMessage(string message, string additional)
        {
            var stageMessage = detailedCurrentStageIsShrinking
                ? $"shrink {detailedLatestNumShrinks} (attempt {detailedLatestNumExecutions})"
                : $"test {detailedLatestNumExecutions} / {MaxTest}";
            return string.Concat(
                $"{additional}{(string.IsNullOrWhiteSpace(additional) ? string.Empty : ": ")}",
                $"{stageMessage} -> {message} in ",
                FormattableString.Invariant($"{DetailedTraceTimer.ElapsedMilliseconds:n0}ms")
            );
        }

        private void Trace(string message)
        {
            debugTraceLines.Add(message);
            TraceDiagnosticsWriter(message);
        }
    }
}
