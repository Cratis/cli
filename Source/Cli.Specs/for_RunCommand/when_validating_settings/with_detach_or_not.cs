// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_validating_settings;

public class with_detach_or_not : Specification
{
    TimeSpan? _foreground;
    TimeSpan? _detached;
    TimeSpan? _explicit;
    TimeSpan? _explicitDetached;

    void Because()
    {
        _foreground = new RunSettings().ReadinessTimeout;
        _detached = new RunSettings { Detach = true }.ReadinessTimeout;
        _explicit = new RunSettings { Timeout = 20 }.ReadinessTimeout;
        _explicitDetached = new RunSettings { Detach = true, Timeout = 20 }.ReadinessTimeout;
    }

    [Fact] void should_not_time_out_a_foreground_run() => _foreground.ShouldBeNull();
    [Fact] void should_default_a_detached_run_to_300_seconds() => _detached.ShouldEqual(TimeSpan.FromSeconds(300));
    [Fact] void should_honor_an_explicit_timeout() => _explicit.ShouldEqual(TimeSpan.FromSeconds(20));
    [Fact] void should_honor_an_explicit_detached_timeout() => _explicitDetached.ShouldEqual(TimeSpan.FromSeconds(20));
}
