# FsCheckCSharp [![Build Status](https://img.shields.io/appveyor/ci/dsschneidermann/fscheckcsharp/master?logo=appveyor)](https://ci.appveyor.com/project/dsschneidermann/fscheckcsharp/branch/master) ![NuGet Version](https://img.shields.io/nuget/v/FsCheckCSharp.svg?logo=nuget) [![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=dsschneidermann/FsCheckCSharp)](https://dependabot.com)

Better C# support for FsCheck.

### Features

* Configuration that supports all FsCheck options
* Output of generated data as C# notation: object or constructor calls
* Instrument test executions to output timing information

### Getting started

Install the [FsCheckCSharp](https://nuget.org/packages/fscheckcsharp) package from NuGet:

```shell
dotnet add package FsCheckCSharp
```

In your test file, add a using statement to access the new extension methods:

```csharp
using FsCheckCSharp;
```

Here is an example test case:

```csharp
[Fact(Timeout = 120_000)] // xUnit is used in this example
public void fscheck_many_devices()
{
    Prop.ForAll(
            // See full sample for how to make a Generator
            new Generator(NumDevices: 100, NumProperties: 4, MaxRandomMs: 5000).Array(),
                input => {
                    // Input was generated, convert it to our actual data type
                    var data = TelemetryGen.CreateTelemetry(input, DateTimeOffset.Now);

                    // Call method under test
                    var results = _MethodUnderTest(data).ToList();

                    // Asserts
                    TestHelpers.TelemetryIsSuperset(data, results);
                    TestHelpers.TelemetryCountEqual(data, results);
                }
        )
        .QuickCheckThrowOnFailure(CSharpNotationConfig.Default, StartSize: 100);
        // ^ use overload from FsCheckCSharp
}
```

### Configuration options

Use the extension methods from FsCheckCSharp:
```csharp
Prop.ForAll(/*...*/).QuickCheckThrowOnFailure(CSharpNotationConfig.Default)
Prop.ForAll(/*...*/).VerboseCheckThrowOnFailure(CSharpNotationConfig.Default)
```

* **CSharpNotationConfig** is the configuration of how output from tests should be formatted. Options are:

```csharp
CSharpNotationConfig.Default
    .IncludeFullTypeNames() // use full type names in output
    .PreferObjectInitialization() // always use "new" even if constructor can be used
    .IncludeParameterNames() // use parameter names in constructor
    .SkipCreateAssignment(); // skip "var data = "
```

It is **recommended to pass the** `CSharpNotationConfig.Default` **always**, otherwise you might be calling the FsCheck extension method which takes no parameters.

Sample output with just `IncludeParameterNames`:
```
System.Exception : Falsifiable, after 4 tests (2 shrinks)
Last step was invoked with size of 100
Shrunk:
var data = new[] {
  new TelemetryGen(NegMs: 2800, DevId: 1, PropId: 1),
  new TelemetryGen(NegMs: 1400, DevId: 1, PropId: 3),
  new TelemetryGen(NegMs: 2799, DevId: 1, PropId: 3)
};
with exception:
XUnitTests.Infrastructure.Error: TelemetryIsSupersetAndPreservesOrder failed
```

* **FsCheck.Config** options can be specified by assigning parameters, such as:
```csharp
.QuickCheckThrowOnFailure(CSharpNotationConfig.Default,
    StartSize: 100, EndSize: 1000, MaxTest: 100, MaxRejected: 10, Arbitrary: /* etc. */);
```

It's also possible to pass an `FsCheck.Config` instance and to use additional extension methods that don't have the throw on failure behavior (`Check`/`Quick` and `Verbose`), but using the methods like in the example above is recommended.

* **FsCheckRunnerConfig** is not intended to be given as it is created based on the method call, eg. `Verbose` will include information on runs, etc. Examining the source code will be necessary to figure the options out.

### Full sample for FsCheck

FsCheck can be hard to get started with, so sample code is provided as an example [here](https://github.com/dsschneidermann/FsCheckCSharp/tree/master/samples/FsCheckTests.cs). It is not the only way of writing a generator for FsCheck but it is my preferred approach.

The goal of the sample is to have a class that we can pass some configuration and generate some data with:

```csharp
new Generator(NumDevices: 100, NumProperties: 4, MaxRandomMs: 5000).Array()
```

By not specifying a size of the generated array, we defer to the FsCheck configurations `StartSize` and `EndSize` that we should set when we run the test.

We create a custom type that represents the *minimum amount of fields we need to generate* (ie. don't use your target implementation type here). In the sample, this is `TelemetryGen`. It has three properties that are generated like so:

```csharp
var gen =
    from negMs in ChooseInt(0, MaxRandomMs)
    from deviceId in ChooseInt(1, NumDevices)
    from propertyId in ChooseInt(1, NumProperties)
    select new TelemetryGen(negMs, deviceId, propertyId);
```

In the `Generator` class we define methods that return `FsCheck.Arbitrary<T>` instances. In the sample code, `Array()` and `Telemetry(...)` is defined as data that can be created. 

Finally, we create a method to convert the generated data into our target type. In the sample the target type is named `datatypes.TelemetryType` and the convertion is done by using an increasing timestamp and varying it by the `NegMs` value.

```csharp
public static TelemetryType[] CreateTelemetry(
    TelemetryGen[] input, DateTimeOffset startTime, int millisecondIncrements = 200)
{
    return input.Select(
            (item, idx) => {
                // item has DevId, PropId and NegMs
                var enqueuedTime = startTime.AddMilliseconds((idx + 1) * millisecondIncrements);
                return TelemetryType.CreateNumeric(
                    // Create strings from DevId and PropId
                    DeviceId: $"dev{item.DevId}", Property: $"prop{item.PropId}",
                    // Use NegMs to influence the Time but not the EnqueuedTime
                    EnqueuedTime: enqueuedTime,
                    Time: enqueuedTime.AddMilliseconds(item.NegMs), 
                    NumericValue: 42
                ); // <- Now we have an actual TelemetryType data
            }
        )
        .ToArray();
}
```

See the full sample file [here](https://github.com/dsschneidermann/FsCheckCSharp/tree/master/samples/FsCheckTests.cs).

### Diagnostic traces on runs

Tests can be instrumented to produce output by adding the following:

```csharp
[Fact(Timeout = 120_000)]
public void fscheck_single_device_mixed_sessions()
{
    // Get instrumentation methods to call:
    var (generated, tested, timer) = FsCheckRunnerConfig.TraceDiagnostics.Events;

    Prop.ForAll(
            new Generator(1, 4, 1000).Array(), input => {
                var data = TelemetryGen.CreateTelemetry(input, Now, 1000);
                generated(); // <- call after data generation

                var results = _MethodUnderTest(data).ToList();
                timer(); // <- call after your test (or as many times as you want)

                TestHelpers.TelemetryIsSupersetAndPreservesOrder(data, results);
                TestHelpers.TelemetryCountEqual(data, results);
                tested(); // <- call after asserts
            }
        )
        .QuickCheckThrowOnFailure(CSharpNotationConfig.Default, 
            FsCheckRunnerConfig.TraceDiagnostics, StartSize: 100);
        //  ^ Pass the trace diagnostics runner config
}
```

The methods will trace the test executions and with `VerboseCheckThrowOnFailure`, the output will include timing information on how long it takes to complete each step. With this we can determine if we have performance issues in the generator, shrinker or the method under test.

It will include output both in the test result (on failure) and will also write during the run to `Console.WriteLine`. To change the writer, specify `TraceDiagnosticsWriter`:
```csharp
FsCheckCSharpRunner.TraceDiagnostics.With(TraceDiagnosticsWriter: (s) => Log.Information(s))`
```

The output will look like this, in my case, I have a dependency induced delay on the first execution of the test method:
```
with trace messages:
test 1 / 100 -> generated in 518ms
time passed in total is now 1,019ms
time passed in total is now 2,026ms
--- 2 seconds have passed since any activity
time passed in total is now 3,030ms
--- 3 seconds have passed since any activity
time passed in total is now 4,030ms
--- 4 seconds have passed since any activity
time passed in total is now 5,024ms
--- 5 seconds have passed since any activity
time passed in total is now 6,028ms
--- 6 seconds have passed since any activity
time passed in total is now 7,025ms
--- 7 seconds have passed since any activity
time passed in total is now 8,016ms
--- 8 seconds have passed since any activity
time passed in total is now 9,058ms
--- 9 seconds have passed since any activity
test 1 / 100 -> timer1 hit in 8,604ms
test 1 / 100 -> succeeded test in 95ms
test 2 / 100 -> generated in 56ms
test 2 / 100 -> timer1 hit in 32ms
test 2 / 100 -> succeeded test in 0ms
test 3 / 100 -> generated in 1ms
test 3 / 100 -> timer1 hit in 34ms
test 3 / 100 -> succeeded test in 1ms
test 4 / 100 -> generated in 0ms
test 4 / 100 -> timer1 hit in 69ms
test 4 / 100 -> succeeded test in 0ms
... etc
```
