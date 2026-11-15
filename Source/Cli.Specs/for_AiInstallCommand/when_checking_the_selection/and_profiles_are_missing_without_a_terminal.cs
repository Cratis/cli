// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_checking_the_selection;

public class and_profiles_are_missing_without_a_terminal : Specification
{
    string? _problem;

    void Because() => _problem = AiInstallCommand.MissingSelection(new AiInstallSettings { Harnesses = "pi" }, inputRedirected: true);

    [Fact] void should_report_that_profiles_are_required() => _problem.ShouldEqual("--profiles is required when input is redirected.");
}
