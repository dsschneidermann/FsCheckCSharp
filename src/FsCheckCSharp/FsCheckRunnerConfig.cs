// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace FsCheckCSharp
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "ArgumentsStyleLiteral")]
    public class FsCheckRunnerConfig
    {
        public delegate void TraceCall(string additional = null);

        /// <summary>Create an instance.</summary>
        /// <param name="Events">
        ///     Events that can be invoked on this config. It is a separate object so that event bindings do not
        ///     follow the <see cref="FsCheckRunnerConfig" />. It can be deconstructed into a triple of
        ///     the methods: (generated, tested, timer).
        /// </param>
        /// <param name="TraceDiagnosticsWriter">
        ///     The action invoked to write traces while the runner is executing. Default is Console.WriteLine.
        /// </param>
        public FsCheckRunnerConfig(
            bool TraceNumberOfRuns, bool ThrowOnFailure, bool TraceDiagnosticsEnabled, TraceCallEvents Events,
            Action<string> TraceDiagnosticsWriter)
        {
            this.TraceNumberOfRuns = TraceNumberOfRuns;
            this.ThrowOnFailure = ThrowOnFailure;
            this.TraceDiagnosticsEnabled = TraceDiagnosticsEnabled;
            this.Events = Events;
            this.TraceDiagnosticsWriter = TraceDiagnosticsWriter;
        }

        /// <summary>
        ///     Returns the default configuration (all settings false).
        /// </summary>
        public static FsCheckRunnerConfig Setup => Default;

        /// <summary>
        ///     Returns the default configuration with verbose output enabled.
        /// </summary>
        public static FsCheckRunnerConfig Verbose => Default.With(TraceNumberOfRuns: true);

        /// <summary>
        ///     Returns a single instance with detailed tracing enabled, enabling use of Events.
        /// </summary>
        public static FsCheckRunnerConfig TraceDiagnostics { get; } = Default.With(
            TraceNumberOfRuns: false, ThrowOnFailure: false, TraceDiagnosticsEnabled: true
        );

        /// <summary>
        ///     Returns the default configuration (all settings false).
        /// </summary>
        public static FsCheckRunnerConfig Default =>
            new FsCheckRunnerConfig(false, false, false, new TraceCallEvents(), Console.WriteLine);

        public bool TraceNumberOfRuns { get; }
        public bool ThrowOnFailure { get; }
        public bool TraceDiagnosticsEnabled { get; }

        /// <summary>
        ///     Events that can be invoked on this config. It is a separate object so that event bindings do not
        ///     follow the <see cref="FsCheckRunnerConfig" />. It can be deconstructed into a triple of
        ///     the methods: (generated, tested, timer).
        /// </summary>
        public TraceCallEvents Events { get; }

        /// <summary>
        ///     The action invoked to write traces while the runner is executing. Default is Console.WriteLine.
        /// </summary>
        public Action<string> TraceDiagnosticsWriter { get; }

        public class TraceCallEvents
        {
            /// <summary>
            ///     Event used to communicate to the executing FsCheckRunner.
            /// </summary>
            public void InvokeTraceGenerated(string additional)
            {
                TraceGenerated?.Invoke(additional);
            }

            /// <summary>
            ///     Event used to communicate to the executing FsCheckRunner.
            /// </summary>
            public void InvokeTraceTested(string additional)
            {
                TraceTested?.Invoke(additional);
            }

            /// <summary>
            ///     Event used to communicate to the executing FsCheckRunner.
            /// </summary>
            public void InvokeTraceTimer(string additional)
            {
                TraceTimer?.Invoke(additional);
            }

            /// <summary>
            ///     Event used to communicate to the executing FsCheckRunner.
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public event Action<string> TraceGenerated;

            /// <summary>
            ///     Event used to communicate to the executing FsCheckRunner.
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public event Action<string> TraceTested;

            /// <summary>
            ///     Event used to communicate to the executing FsCheckRunner.
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public event Action<string> TraceTimer;
        }

        /// <summary>Return a new instance with the properties.</summary>
        /// <param name="Events">
        ///     Events that can be invoked on this config. It is a separate object so that event bindings do not
        ///     follow the <see cref="FsCheckRunnerConfig" />. It can be deconstructed into a triple of
        ///     the methods: (generated, tested, timer).
        /// </param>
        /// <param name="TraceDiagnosticsWriter">
        ///     The action invoked to write traces while the runner is executing. Default is Console.WriteLine.
        /// </param>
        public FsCheckRunnerConfig With(
            bool? TraceNumberOfRuns = null, bool? ThrowOnFailure = null, bool? TraceDiagnosticsEnabled = null,
            TraceCallEvents Events = null, Action<string> TraceDiagnosticsWriter = null)
        {
            return new FsCheckRunnerConfig(
                TraceNumberOfRuns ?? this.TraceNumberOfRuns, ThrowOnFailure ?? this.ThrowOnFailure,
                TraceDiagnosticsEnabled ?? this.TraceDiagnosticsEnabled, Events ?? this.Events,
                TraceDiagnosticsWriter ?? this.TraceDiagnosticsWriter
            );
        }
    }
}
