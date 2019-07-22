// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************
namespace FsCheckCSharp
{
    public static class TraceCallsExtensions
    {
        // ReSharper disable once UseDeconstructionOnParameter
        public static void Deconstruct(
            this FsCheckRunnerConfig.TraceCallEvents traceCalls, out FsCheckRunnerConfig.TraceCall generated,
            out FsCheckRunnerConfig.TraceCall tested, out FsCheckRunnerConfig.TraceCall timer)
        {
            generated = traceCalls.InvokeTraceGenerated;
            tested = traceCalls.InvokeTraceTested;
            timer = traceCalls.InvokeTraceTimer;
        }
    }
}
