// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;

namespace XUnitTests.FsCheck
{
    public partial class FsCheckTests
    {
        private readonly CSharpNotationConfig notationConfig = CSharpNotationConfig.Default.IncludeParameterNames();

        private DateTimeOffset Now { get; } = DateTimeOffset.Now;

        [Fact(Timeout = 120_000)]
        public void fscheck_many_devices()
        {
            var (generated, tested, timer) = FsCheckRunnerConfig.TraceDiagnostics.Events;

            Prop.ForAll(
                    new Generator(NumDevices: 100, NumProperties: 4, MaxRandomMs: 5000).Array(), input => {
                        var data = TelemetryGen.CreateTelemetry(input, Now);
                        generated();

                        var results = _MethodUnderTest(data).ToList();
                        timer();

                        TestHelpers.TelemetryIsSupersetAndPreservesOrder(data, results);
                        TestHelpers.TelemetryCountEqual(data, results);
                        tested();
                    }
                )
                .QuickCheckThrowOnFailure(notationConfig, StartSize: 100);
        }
    }

    public partial class FsCheckTests
    {
        private class TelemetryGen
        {
            public TelemetryGen(int NegMs, int DevId, int PropId)
            {
                this.NegMs = NegMs;
                this.DevId = DevId;
                this.PropId = PropId;
            }

            public int NegMs { get; }
            public int DevId { get; }
            public int PropId { get; }

            public static TelemetryType[] CreateTelemetry(
                TelemetryGen[] input, DateTimeOffset startTime, int millisecondIncrements = 200)
            {
                return input.Select(
                        (item, idx) => {
                            var enqueuedTime = startTime.AddMilliseconds((idx + 1) * millisecondIncrements);
                            return TelemetryType.CreateNumeric(
                                DeviceId: $"dev{item.DevId}", Property: $"prop{item.PropId}",
                                EnqueuedTime: enqueuedTime,
                                Time: enqueuedTime.AddMilliseconds(item.NegMs), 
                                NumericValue: 42
                            ); // <- Now we have a complex object with strings and datetimes
                        }
                    )
                    .ToArray();
            }
        }

        private class Generator
        {
            public Generator(int NumDevices, int NumProperties, int MaxRandomMs)
            {
                this.NumDevices = NumDevices;
                this.NumProperties = NumProperties;
                this.MaxRandomMs = MaxRandomMs;
            }

            public int MaxRandomMs { get; }
            public int NumDevices { get; }
            public int NumProperties { get; }
            
            public Arbitrary<TelemetryGen[]> Array()
            {
                var arrayGen = Telemetry().Generator.ArrayOf();
                var arrayArb = Arb.From(arrayGen, ShrinkArray);

                IEnumerable<TelemetryGen[]> ShrinkArray(TelemetryGen[] array)
                {
                    var shrunkInstances = array.Select(y => Telemetry().Shrinker(y).ToArray());
                    var shrunkArray = Arb.Default.NonEmptyArray<TelemetryGen>().Shrinker(NonEmptyArray<TelemetryGen>.NewNonEmptyArray(array)).Select(y => y.Item);
                    return shrunkInstances.Concat(shrunkArray);
                }

                return arrayArb;
            }

            private Arbitrary<TelemetryGen> Telemetry()
            {
                Gen<int> ChooseInt(int min, int max)
                {
                    return Arb.From(Gen.Choose(min, max)).Generator;
                }

                var gen =
                    from negMs in ChooseInt(0, MaxRandomMs)
                    from deviceId in ChooseInt(1, NumDevices)
                    from propertyId in ChooseInt(1, NumProperties)
                    select new TelemetryGen(negMs, deviceId, propertyId);

                var arb = Arb.From(gen, ShrinkTelemetryGen);

                IEnumerable<TelemetryGen> ShrinkTelemetryGen(TelemetryGen x)
                {
                    if (x.DevId != 1)
                    {
                        yield return x.With(DevId: 1);
                    }

                    if (x.PropId != 1)
                    {
                        yield return x.With(PropId: 1);
                    }

                    if (x.NegMs != 0)
                    {
                        yield return x.With(NegMs: x.NegMs / 2);
                        yield return x.With(NegMs: x.NegMs - 1);
                    }
                }

                return arb;
            }
        }
    }
}
